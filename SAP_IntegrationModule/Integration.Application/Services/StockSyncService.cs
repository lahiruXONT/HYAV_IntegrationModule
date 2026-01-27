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
        GetMaterialStockFromSapRequestDto request
    )
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
                        $"Stock Record sync completed for Material Code" + result.Material;
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

                throw new IntegrationException(
                    $"Stock sync failed: {ex.Message}",
                    ex,
                    ErrorCodes.StockOutSync
                );
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

                throw new IntegrationException(
                    $"Stock sync failed: {ex.Message}",
                    ex,
                    ErrorCodes.StockOutSync
                );
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
                    ["SyncType"] = "Stock In",
                    ["RequestDate"] = result.SyncDate,
                    ["BusinessUnit"] = request.BusinessUnit,
                    ["MaterialDocumentNumber"] = request.MaterialDocumentNumber,
                }
            )
        )
        {
            var validationErrors = ValidateRequest(request);
            if (validationErrors.Any())
            {
                result.Success = false;
                result.Message =
                    $"Stock In sync request validation failed: {string.Join("; ", validationErrors)}";
                _logger.LogWarning(
                    "Stock In sync validation failed: {ValidationErrors}",
                    string.Join("; ", validationErrors)
                );
                return result;
            }

            _logger.LogInformation(
                "Starting stock in sync for MaterialDocumentNumber : {MaterialDocumentNumber} : {Date}",
                request.MaterialDocumentNumber,
                result.SyncDate
            );

            try
            {
                var xontStockDetails = await _stockRepository.GetStockInTransactionDetails(
                    request.BusinessUnit,
                    request.MaterialDocumentNumber
                );

                if (xontStockDetails == null || !xontStockDetails.Any())
                {
                    result.Success = false;
                    result.Message =
                        $"No Stock Record found for MaterialDocumentNumber :{request.MaterialDocumentNumber} ";

                    _logger.LogWarning(result.Message);
                    return result;
                }

                var stock = await _mappingHelper.MapXontToSapStockTransactionAsync(
                    xontStockDetails
                );

                var sapResult = await _sapClient.SendStockInAsync(stock);

                if (sapResult.E_RESULT == "1")
                {
                    result.Success = true;
                    result.Message =
                        $"Stock Record sync completed for Document Number "
                        + result.MaterialDocumentNumber;
                }
                else
                {
                    result.Success = false;
                    var errorMessage =
                        $"Stock Record sync Failed for Document Number {request.BusinessUnit}, {request.MaterialDocumentNumber}: {sapResult.E_REASON}";
                    _logger.LogError(errorMessage);
                    result.Message = errorMessage;
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = sapEx.Message;
                _logger.LogError(
                    sapEx.InnerException,
                    "SAP API error processing stock in {BusinessUnit} {MaterialDocumentNumber}: {Message}",
                    request.BusinessUnit,
                    request.MaterialDocumentNumber,
                    sapEx.Message
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during sync stock in {BusinessUnit} , {MaterialDocumentNumber}",
                    request.BusinessUnit,
                    request.MaterialDocumentNumber
                );
                result.Message =
                    $"Unexpected error during sync stock in {request.BusinessUnit}, {request.MaterialDocumentNumber}"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );

                throw new IntegrationException(
                    result.Message,
                    request.MaterialDocumentNumber.ToString(),
                    ex,
                    ErrorCodes.StockInSync
                );
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    private List<string> ValidateRequest(StockInXontRequestDto request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request cannot be null");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.MaterialDocumentNumber))
            errors.Add("MaterialDocumentNumber is required");
        if (string.IsNullOrWhiteSpace(request.BusinessUnit))
            errors.Add("BusinessUnit is required");

        return errors;
    }
}
