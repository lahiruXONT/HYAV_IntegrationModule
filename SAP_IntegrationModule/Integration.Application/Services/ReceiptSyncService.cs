using System.Diagnostics;
using Integration.Application.DTOs;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Services;

public class ReceiptSyncService : IReceiptSyncService
{
    private readonly ITransactionsRepository _transactionsRepository;
    private readonly ISapClient _sapClient;
    private readonly ReceiptMappingHelper _mappingHelper;
    private readonly ILogger<ReceiptSyncService> _logger;

    public ReceiptSyncService(
        ITransactionsRepository transactionsRepository,
        ISapClient sapClient,
        ReceiptMappingHelper mappingHelper,
        ILogger<ReceiptSyncService> logger
    )
    {
        _transactionsRepository =
            transactionsRepository
            ?? throw new ArgumentNullException(nameof(transactionsRepository));
        _sapClient = sapClient ?? throw new ArgumentNullException(nameof(sapClient));
        _mappingHelper = mappingHelper ?? throw new ArgumentNullException(nameof(mappingHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReceiptSyncResultDto> SyncReceiptToSapAsync(XontReceiptSyncRequestDto request)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationContext.CorrelationId;
        var result = new ReceiptSyncResultDto { SyncDate = DateTime.Now };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Receipt",
                    ["RequestDate"] = result.SyncDate,
                    ["BusinessUnit"] = request.BusinessUnit,
                    ["ReciptIDs"] = string.Join(",", request.IDs),
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
                        $"Receipt sync request validation failed: {string.Join("; ", validationErrors)}";
                    _logger.LogWarning(
                        "Receipt sync validation failed: {ValidationErrors}",
                        string.Join("; ", validationErrors)
                    );
                    return result;
                }
                _logger.LogInformation("Starting Receipt sync : {Date}", result.SyncDate);

                var pendingReceipts = await _transactionsRepository.GetUnsyncedReceiptsAsync(
                    request.BusinessUnit,
                    request.IDs
                );

                result.TotalRecords = pendingReceipts?.Count ?? 0;

                if (pendingReceipts == null || !pendingReceipts.Any())
                {
                    result.Success = true;
                    result.Message = "No pending receipts found for synchronization";
                    _logger.LogInformation("No pending receipts found for synchronization");
                    return result;
                }

                _logger.LogInformation(
                    "Found {Count} pending receipts to sync",
                    result.TotalRecords
                );

                try
                {
                    foreach (var receipt in pendingReceipts)
                    {
                        await ProcessReceiptAsync(receipt, result);
                    }

                    if (result.FailedRecords > 0)
                        result.Success = false;
                    else
                        result.Success = true;
                    result.Message = BuildSuccessMessage(result);

                    _logger.LogInformation(
                        "Receipt sync completed successfully: {@Result}",
                        result
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error during receipt processing. Processed {Processed}/{Total}.",
                        result.SyncedRecords,
                        result.TotalRecords
                    );

                    result.Success = false;
                    result.Message =
                        $"Unexpected error during receipt sync; failed after processing {result.SyncedRecords}/{result.TotalRecords}"
                        + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                        + (
                            !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                                ? $"; {ex.InnerException.Message}"
                                : ""
                        );
                    throw new IntegrationException(result.Message, ex, ErrorCodes.ReceiptSync);
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = sapEx.Message;
                _logger.LogError(
                    sapEx.InnerException,
                    "SAP API error during receipt sync: {Message}",
                    sapEx.Message
                );
                throw;
            }
            catch (Exception ex) when (ex is not IntegrationException)
            {
                result.Success = false;
                result.Message =
                    $"Unexpected error during receipt sync"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );
                _logger.LogError(ex, "Unexpected error during receipt sync");
                throw new IntegrationException(result.Message, ex, ErrorCodes.ReceiptSync);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
    }

    private List<string> ValidateRequest(XontReceiptSyncRequestDto request)
    {
        var errors = new List<string>();

        if (request == null)
        {
            errors.Add("Request cannot be null");
            return errors;
        }
        if (string.IsNullOrWhiteSpace(request.BusinessUnit))
            errors.Add("Business Unit is required");

        return errors;
    }

    private async Task ProcessReceiptAsync(Transaction receipt, ReceiptSyncResultDto result)
    {
        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["BusinessUnit"] = receipt.BusinessUnit,
                    ["DocumentNumberSystem"] = receipt.DocumentNumberSystem,
                }
            )
        )
        {
            try
            {
                _logger.LogDebug(
                    "Processing receipt: {BusinessUnit} ,{DocumentNumberSystem}",
                    receipt.BusinessUnit,
                    receipt.DocumentNumberSystem
                );

                var sapReceipt = await _mappingHelper.MapXontTransactionToSapReceiptAsync(receipt);

                var sapResult = await _sapClient.SendReceiptAsync(sapReceipt);

                if (sapResult.E_RESULT == "1")
                {
                    receipt.IntegratedStatus = "1";
                    receipt.IntegratedOn = DateTime.Now;
                    receipt.SAPDocumentNumber = sapResult.DOCUMENT_NUMBER;

                    await _transactionsRepository.UpdateTransactionAsync(receipt);

                    result.SyncedRecords++;
                    _logger.LogInformation(
                        "Successfully processed receipt {BusinessUnit}, {DocumentNumberSystem} with SAP doc number {SAPDocumentNumber}",
                        receipt.BusinessUnit,
                        receipt.DocumentNumberSystem,
                        sapResult.DOCUMENT_NUMBER
                    );
                }
                else
                {
                    var errorMessage =
                        $"SAP rejected receipt {receipt.BusinessUnit}, {receipt.DocumentNumberSystem}: {sapResult.E_REASON}";
                    _logger.LogError(errorMessage);
                    result.Errors ??= new List<string>();
                    result.Errors.Add(errorMessage);
                    result.FailedRecords++;
                }
            }
            catch (SapApiExceptionDto sapEx)
            {
                result.Success = false;
                result.Message = sapEx.Message;
                _logger.LogError(
                    sapEx.InnerException,
                    "SAP API error processing receipt {BusinessUnit} {DocumentNumberSystem}: {Message}",
                    receipt.BusinessUnit,
                    receipt.DocumentNumberSystem,
                    sapEx.Message
                );
                throw;
            }
            catch (Exception ex) when (ex is not IntegrationException)
            {
                _logger.LogError(
                    ex,
                    "Error processing receipt {BusinessUnit} , {DocumentNumberSystem}",
                    receipt.BusinessUnit,
                    receipt.DocumentNumberSystem
                );
                result.FailedRecords += 1;
                result.Message =
                    $"Receipt {receipt.BusinessUnit}, {receipt.DocumentNumberSystem}"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    );

                throw new IntegrationException(
                    result.Message,
                    receipt.DocumentNumberSystem.ToString(),
                    ex,
                    ErrorCodes.ReceiptSync
                );
            }
        }
    }

    private string BuildSuccessMessage(ReceiptSyncResultDto result)
    {
        var message = "Receipt sync completed successfully. ";

        if (result.SyncedRecords > 0)
            message += $"Success: {result.SyncedRecords}. ";

        if (result.SkippedRecords > 0)
            message += $"Skipped: {result.SkippedRecords}. ";

        if (result.FailedRecords > 0)
            message += $"Failed: {result.FailedRecords}. ";

        message += $"Total processed: {result.TotalRecords} in {result.ElapsedMilliseconds}ms";

        return message;
    }
}
