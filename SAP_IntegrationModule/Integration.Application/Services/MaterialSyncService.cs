using System.Diagnostics;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public sealed class MaterialSyncService : IMaterialSyncService
{
    private readonly IProductRepository _productRepository;
    private readonly ISapClient _sapClient;
    private readonly MaterialMappingHelper _mappingHelper;
    private readonly ILogger<MaterialSyncService> _logger;

    public MaterialSyncService(
        IProductRepository productRepository,
        ISapClient sapClient,
        MaterialMappingHelper mappingHelper,
        ILogger<MaterialSyncService> logger
    )
    {
        _productRepository =
            productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MaterialSyncResultDto> SyncMaterialsFromSapAsync(
        XontMaterialSyncRequestDto request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new MaterialSyncResultDto { SyncDate = DateTime.Now };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Material",
                    ["RequestDate"] = request.Date,
                }
            )
        )
        {
            try
            {
                var validationErrors = ValidateRequest(request);
                if (validationErrors.Any())
                {
                    result.Success = false;
                    result.Message =
                        $"Material sync request Validation failed: {string.Join("; ", validationErrors)}";
                    _logger.LogWarning(result.Message);
                    return result;
                }

                var sapMaterials = await _sapClient.GetMaterialChangesAsync(request);

                result.TotalRecords = sapMaterials?.Count ?? 0;

                if (sapMaterials == null || !sapMaterials.Any())
                {
                    result.Success = true;
                    result.Message = "No material changes found for  date: {request.Date}";
                    _logger.LogInformation(result.Message);
                    return result;
                }

                _logger.LogInformation(
                    "Retrieved {Count} material records from SAP",
                    result.TotalRecords
                );

                var materialGroups = sapMaterials.GroupBy(m => new { m.Material }).ToList();
                var processedGroups = 0;
                //await _productRepository.BeginTransactionAsync();

                try
                {
                    foreach (var group in materialGroups)
                    {
                        await ProcessMaterialGroupAsync(group.Key.Material, group.ToList(), result);
                        processedGroups++;
                    }

                    //await _productRepository.CommitTransactionAsync();

                    result.Success = true;
                    result.Message = BuildSuccessMessage(result);

                    _logger.LogInformation(result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during material sync");
                    //await _productRepository.RollbackTransactionAsync();
                    result.Success = false;
                    result.Message = $"Unexpected error during material sync";
                    throw new MaterialSyncException($"Unexpected error during material sync", ex);
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = sapEx.Message;
                _logger.LogError(sapEx.InnerException, result.Message);
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Unexpected error during material sync";
                _logger.LogError(ex, "Unexpected error during material sync");
                throw new MaterialSyncException($"Unexpected error during material sync", ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    private List<string> ValidateRequest(XontMaterialSyncRequestDto request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request cannot be null");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Date))
            errors.Add("Date is required");

        if (!string.IsNullOrWhiteSpace(request.Date))
        {
            if (request.Date.Length != 8)
                errors.Add("Date must be in YYYYMMDD format (8 characters)");

            if (
                !DateTime.TryParseExact(
                    request.Date,
                    "yyyyMMdd",
                    null,
                    System.Globalization.DateTimeStyles.None,
                    out _
                )
            )
                errors.Add("Date must be a valid date in YYYYMMDD format");
        }

        return errors;
    }

    private async Task ProcessMaterialGroupAsync(
        string materialCode,
        List<SapMaterialResponseDto> sapMaterials,
        MaterialSyncResultDto result
    )
    {
        if (!sapMaterials.Any())
            return;

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["MaterialCode"] = materialCode,
                    ["RecordCount"] = sapMaterials.Count,
                }
            )
        )
        {
            try
            {
                #region global material processing (commented out)
                //var globalMaterialObj = await _mappingHelper.MapSapToXontGlobalMaterialAsync(
                //    sapMaterials[0]
                //);

                //var globalMaterialExisting = await _productRepository.GetGlobalProductAsync(
                //    globalMaterialObj.ProductCode
                //);
                //if (globalMaterialExisting == null)
                //{
                //    await _productRepository.CreateGlobalProductAsync(globalMaterialObj);
                //}
                //else if (
                //    _mappingHelper.HasGlobalMaterialChanges(
                //        globalMaterialExisting,
                //        globalMaterialObj
                //    )
                //)
                //{
                //    _mappingHelper.UpdateGlobalMaterial(globalMaterialExisting, globalMaterialObj);
                //}
                #endregion

                foreach (var sapMaterial in sapMaterials)
                {
                    try
                    {
                        var xontProduct = await _mappingHelper.MapSapToXontMaterialAsync(
                            sapMaterial
                        );

                        var existing = await _productRepository.GetByProductCodeAsync(
                            xontProduct.ProductCode,
                            xontProduct.BusinessUnit
                        );

                        if (existing == null)
                        {
                            await _productRepository.CreateProductAsync(xontProduct);
                            result.NewMaterials++;
                        }
                        else
                        {
                            if (_mappingHelper.HasMaterialChanges(existing, xontProduct))
                            {
                                _mappingHelper.UpdateMaterial(existing, xontProduct);
                                result.UpdatedMaterials++;
                            }
                            else
                            {
                                result.SkippedMaterials++;
                            }
                        }
                    }
                    catch (ValidationExceptionDto valEx)
                    {
                        _logger.LogWarning(
                            "Validation failed for material {MaterialCode}: {Message}",
                            sapMaterial.Material,
                            valEx.Message
                        );
                        result.FailedRecords++;
                        result.ValidationErrors ??= new List<string>();
                        result.ValidationErrors.Add(
                            $"Material {sapMaterial.Material}: {valEx.Message}"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Unexpected error during material sync : {CustomerCode} ",
                            sapMaterial.Material
                        );
                        result.FailedRecords++;

                        throw new MaterialSyncException(
                            $"Unexpected error during material sync : {sapMaterial.Material}",
                            sapMaterial.Material,
                            ex
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during customer sync : {CustomerCode}",
                    materialCode
                );
                result.FailedRecords += sapMaterials.Count;

                throw new CustomerSyncException(
                    $"Unexpected error during customer sync : {materialCode}",
                    materialCode,
                    ex
                );
            }
        }
    }

    private string BuildSuccessMessage(MaterialSyncResultDto result)
    {
        var message = $"Material sync completed. ";

        if (result.NewMaterials > 0)
            message += $"New: {result.NewMaterials}. ";

        if (result.UpdatedMaterials > 0)
            message += $"Updated: {result.UpdatedMaterials}. ";

        if (result.SkippedMaterials > 0)
            message += $"Skipped: {result.SkippedMaterials}. ";

        if (result.FailedRecords > 0)
            message += $"Failed: {result.FailedRecords}. ";

        message += $"Total processed: {result.TotalRecords} in {result.ElapsedMilliseconds}ms";

        return message;
    }
}
