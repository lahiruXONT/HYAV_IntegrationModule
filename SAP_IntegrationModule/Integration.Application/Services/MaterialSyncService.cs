using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        ILogger<MaterialSyncService> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MaterialSyncResultDto> SyncMaterialsFromSapAsync(XontMaterialSyncRequestDto request)
    {
        var result = new MaterialSyncResultDto
        {
            SyncDate = DateTime.Now,
        };


        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Date == default)
                throw new ArgumentException("Date is required", nameof(request.Date));

            var sapMaterials = await _sapClient.GetMaterialChangesAsync(request);


            if (sapMaterials == null || !sapMaterials.Any())
            {
                result.Success = true;
                result.Message = "No material changes found";

                return result;
            }

            result.TotalRecords = sapMaterials.Count;

            var validMaterials = sapMaterials
                .Where(m => _mappingHelper.HasValidMaterialCode(m.Material))
                .ToList();

            var invalidCount = sapMaterials.Count - validMaterials.Count;
            

            var materialGroups = validMaterials
                .GroupBy(m => new { m.Material })
                .ToList();


            await _productRepository.BeginTransactionAsync();

            try
            {
                foreach (var group in materialGroups)
                {
                    await ProcessMaterialGroupAsync(
                        group.Key.Material,
                        group.ToList(),
                        result);
                }

                await _productRepository.CommitTransactionAsync();

                result.Success = true;
                result.Message = $"Material sync completed. " +
                               $"Total: {result.TotalRecords}, " +
                               $"New: {result.NewMaterials}, " +
                               $"Updated: {result.UpdatedMaterials}, " +
                               $"Skipped: {result.SkippedMaterials}, " +
                               $"Failed: {result.FailedRecords}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during material processing in sync , rolling back transaction");

                await _productRepository.RollbackTransactionAsync();

                result.Success = false;
                result.Message = $"Sync  failed and rolled back: {ex.Message}";
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Sync  failed: {ex.Message}";

            _logger.LogError(ex, "Material sync  failed");

            throw new MaterialSyncException($"Material sync failed: {ex.Message}", ex);
        }
       

        return result;
    }

    private async Task ProcessMaterialGroupAsync( string materialCode, List<SapMaterialResponseDto> sapMaterials,MaterialSyncResultDto result)
    {

        foreach (var sapMaterial in sapMaterials)
        {
            try
            {
                var xontProduct = await _mappingHelper.MapSapToXontMaterialAsync(sapMaterial);

                if (!await _mappingHelper.HasValidBusinessUnitAsync(xontProduct.BusinessUnit))
                {
                    result.SkippedMaterials++;
                    continue;
                }

                var existing = await _productRepository.GetByProductCodeAsync(
                    xontProduct.ProductCode,
                    xontProduct.BusinessUnit);

                if (existing == null)
                {
                    xontProduct.CreatedOn = DateTime.Now;
                    xontProduct.CreatedBy = "SAP_SYNC";
                    await _productRepository.CreateAsync(xontProduct);
                    result.NewMaterials++;

                }
                else
                {
                    if (_mappingHelper.HasChanges(existing, xontProduct))
                    {
                        _mappingHelper.UpdateMaterial(existing, xontProduct);
                        existing.UpdatedOn = DateTime.Now;
                        existing.UpdatedBy = "SAP_SYNC";
                        await _productRepository.UpdateAsync(existing);
                        result.UpdatedMaterials++;

                    }
                    else
                    {
                        result.SkippedMaterials++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,  "Error processing material {Code} in sync ",  materialCode);

                result.FailedRecords++;

                throw new MaterialSyncException(
                    $"Failed to process material {materialCode}: {ex.Message}",
                    materialCode, ex);
            }
        }

    }
}