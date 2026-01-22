using System.Diagnostics;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public sealed class StockSyncService : IStockSyncService
{
    private readonly IStockRepository _stockRepository;
    private readonly ISapClient _sapClient;
    private readonly StockMappingHelper _mappingHelper;
    private readonly ILogger<StockSyncService> _logger;

    public StockSyncService(
        IStockRepository stockRepository,
        ISapClient sapClient,
        StockMappingHelper mappingHelper,
        ILogger<StockSyncService> logger
    )
    {
        _stockRepository =
            stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetMaterialStockFromSapResponseDto> GetLocationStockDetailsAsync(
        GetMaterialStockFromSapRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new GetMaterialStockFromSapResponseDto { SyncDate = DateTime.Now };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Stock Out",
                    ["RequestDate"] = request.SyncDate,
                }
            )
        )
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (request.SyncDate == default)
                    throw new ArgumentException("Date is required", nameof(request.SyncDate));

                var sapStockDetails = await _sapClient.GetLocationStockDetails(request);

                if (sapStockDetails == null)
                {
                    result.Success = true;
                    result.Message = "No Stock Record found";

                    return result;
                }

                try
                {
                    //var stock = _mappingHelper.MapSapToXontStockTransactionAsync(sapStockDetails);

                    //await _stockRepository.UpdateStockTransactionAsync(stock);

                    result.Success = true;
                    result.Message =
                        $"Stock Record sync completed for Material Code"
                        + result.Material;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error during stock processing in sync , rolling back transaction"
                    );

                    result.Success = false;
                    result.Message = $"Sync failed and rolled back: {ex.Message}";
                    throw;
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = $"SAP API error: {sapEx.Message}";
                _logger.LogError(sapEx, "SAP API error during stock sync");
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync  failed: {ex.Message}";

                _logger.LogError(ex, "Stock sync  failed");

                throw new MaterialSyncException($"Stock sync failed: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    public async Task<StockOutXontResponseDto> SyncStockOutFromSapAsync(
        StockOutSapRequestDto request
    )
    {
        //Get Sap Details by Sending DocNo
        //Map Sap Details to StockTransaction
        //Update Xont Stock
        //Send Response to SAP

        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new StockOutXontResponseDto { SyncDate = DateTime.Now };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Stock Out",
                    ["RequestDate"] = request.SyncDate,
                }
            )
        )
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (request.SyncDate == default)
                    throw new ArgumentException("Date is required", nameof(request.SyncDate));

                var sapStockDetails = await _sapClient.GetStockOutTransactionDetails(request);

                if (sapStockDetails == null)
                {
                    result.Success = true;
                    result.Message = "No Stock Record found";

                    return result;
                }

                try
                {
                    var stock = _mappingHelper.MapSapToXontStockTransactionAsync(sapStockDetails);

                    await _stockRepository.UpdateStockTransactionAsync(stock);

                    result.Success = true;
                    result.Message =
                        $"Stock Record sync completed for Document Number "
                        + result.MaterialDocumentNumber;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error during stock processing in sync , rolling back transaction"
                    );

                    result.Success = false;
                    result.Message = $"Sync failed and rolled back: {ex.Message}";
                    throw;
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = $"SAP API error: {sapEx.Message}";
                _logger.LogError(sapEx, "SAP API error during stock sync");
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync  failed: {ex.Message}";

                _logger.LogError(ex, "Stock sync  failed");

                throw new MaterialSyncException($"Stock sync failed: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    public async Task<StockInXontResponseDto> SyncStockInFromXontAsync(
        StockInXontRequestDto request
    )
    {
        //Get Xont Stock Transaction Details by Sending DocNo
        //Map StockTransaction Details to Sap DTO
        //Send Response to SAP

        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new StockInXontResponseDto
        {
            SyncDate = DateTime.Now,
            StockDetails = new StockTransaction(),
        };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Stock Out",
                    ["RequestDate"] = request.SyncDate,
                }
            )
        )
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (request.SyncDate == default)
                    throw new ArgumentException("Date is required", nameof(request.SyncDate));

                var xontStockDetails = await _stockRepository.GetStockInTransactionDetails(request);

                if (xontStockDetails == null)
                {
                    result.Success = true;
                    result.Message = "No Stock Record found";

                    return result;
                }

                try
                {
                    var stock = _mappingHelper.MapXontToSapStockTransactionAsync(xontStockDetails);

                    //await _stockRepository.UpdateStockTransactionAsync(stock);

                    result.Success = true;
                    result.Message =
                        $"Stock Record sync completed for Document Number "
                        + result.MaterialDocumentNumber;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error during stock processing in sync , rolling back transaction"
                    );

                    result.Success = false;
                    result.Message = $"Sync failed and rolled back: {ex.Message}";
                    throw;
                }
            }
            //catch (SapApiExceptionDto sapEx)
            //{
            //    result.Success = false;
            //    result.Message = $"SAP API error: {sapEx.Message}";
            //    _logger.LogError(sapEx, "SAP API error during stock sync");
            //    throw;
            //}
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Sync  failed: {ex.Message}";

                _logger.LogError(ex, "Stock sync  failed");

                throw new MaterialSyncException($"Stock sync failed: {ex.Message}", ex);
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
