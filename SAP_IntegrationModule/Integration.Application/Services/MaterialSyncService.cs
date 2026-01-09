using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (request.Date == default)
                    throw new ArgumentException("Date is required", nameof(request.Date));

                var sapMaterials = await _sapClient.GetMaterialChangesAsync(request);

                result.TotalRecords = sapMaterials?.Count ?? 0;

                if (sapMaterials == null || !sapMaterials.Any())
                {
                    result.Success = true;
                    result.Message = "No material changes found";

                    return result;
                }

                var materialGroups = sapMaterials.GroupBy(m => new { m.Material }).ToList();

                await _productRepository.BeginTransactionAsync();

                try
                {
                    foreach (var group in materialGroups)
                    {
                        await ProcessMaterialGroupAsync(group.Key.Material, group.ToList(), result);
                    }

                    await _productRepository.CommitTransactionAsync();

                    result.Success = true;
                    result.Message =
                        $"Material sync completed. "
                        + $"Total: {result.TotalRecords}, "
                        + $"New: {result.NewMaterials}, "
                        + $"Updated: {result.UpdatedMaterials}, "
                        + $"Skipped: {result.SkippedMaterials}, "
                        + $"Failed: {result.FailedRecords}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error during material processing in sync , rolling back transaction"
                    );

                    await _productRepository.RollbackTransactionAsync();

                    result.Success = false;
                    result.Message = $"Sync  failed and rolled back: {ex.Message}";
                    throw;
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = $"SAP API error: {sapEx.Message}";
                _logger.LogError(sapEx, "SAP API error during customer sync");
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync  failed: {ex.Message}";

                _logger.LogError(ex, "Material sync  failed");

                throw new MaterialSyncException($"Material sync failed: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
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
                var globalMaterialObj = await _mappingHelper.MapSapToXontGlobalMaterialAsync(
                    sapMaterials[0]
                );

                var globalMaterialExisting = await _productRepository.GetGlobalProductAsync(
                    globalMaterialObj.ProductCode
                );
                if (globalMaterialExisting == null)
                {
                    await _productRepository.CreateGlobalProductAsync(globalMaterialObj);
                }
                else if (
                    _mappingHelper.HasGlobalMaterialChanges(
                        globalMaterialExisting,
                        globalMaterialObj
                    )
                )
                {
                    _mappingHelper.UpdateGlobalMaterial(globalMaterialExisting, globalMaterialObj);
                }

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
                            "Error processing material {materialCode} in business unit",
                            sapMaterial.Material
                        );
                        result.FailedRecords++;
                        throw new CustomerSyncException(
                            $"Failed to process material {sapMaterial.Material}: {ex.Message}",
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
                    "Error processing material group {materialCode}",
                    materialCode
                );
                result.FailedRecords += sapMaterials.Count;
                throw;
            }
        }
    }
}
