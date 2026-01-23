using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public class MaterialStockSyncService : IMaterialStockSyncService
{
    private readonly ISapClient _sapClient;
    private readonly ILogger<MaterialStockSyncService> _logger;

    public MaterialStockSyncService(ISapClient sapClient, ILogger<MaterialStockSyncService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<MaterialStockSyncResultDto> SyncMaterialStockFromSapAsync(
        XontMaterialStockSyncRequestDto request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new MaterialStockSyncResultDto { SyncDate = DateTime.Now };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "MaterialStock",
                    ["RequestDate"] = result.SyncDate,
                    ["Material"] = request.Material,
                }
            )
        )
        {
            try
            {
                _logger.LogInformation(
                    "Starting Material stock sync for material: {Material}",
                    request.Material
                );

                var sapMaterialStockResult = await _sapClient.GetMaterialStockAsync(request);

                if (
                    sapMaterialStockResult == null
                    || (
                        !string.IsNullOrEmpty(sapMaterialStockResult.E_RESULT)
                        && sapMaterialStockResult.E_RESULT != "1"
                    )
                )
                {
                    result.Success = false;
                    result.Message =
                        sapMaterialStockResult?.E_REASON ?? "No response message received from SAP";
                    _logger.LogWarning(
                        "SAP returned no data or error result: {Result}, Reason: {Reason}",
                        sapMaterialStockResult?.E_RESULT,
                        sapMaterialStockResult?.E_REASON
                    );
                }
                else
                {
                    result.ITEM =
                        sapMaterialStockResult.ITEM ?? new List<SapMaterialStockSyncResponseItem>();
                    result.Success = true;
                    result.Message = sapMaterialStockResult.E_REASON;

                    _logger.LogInformation(
                        "Successfully synced {ItemCount} material stock items from SAP",
                        result.ITEM.Count
                    );
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
                result.Message =
                    "Unexpected error during material stock sync"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );
                _logger.LogError(ex, result.Message);
                throw new MaterialStockSyncException(result.Message, ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }
}
