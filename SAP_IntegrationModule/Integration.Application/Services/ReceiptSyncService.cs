using System.Diagnostics;
using Azure.Core;
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
        var result = new ReceiptSyncResultDto { SyncDate = DateTime.UtcNow };

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["SyncType"] = "Receipt",
                    ["RequestDate"] = result.SyncDate,
                }
            )
        )
        {
            try
            {
                _logger.LogInformation("Starting Receipt sync : {Date} ", result.SyncDate);

                var pendingReceipts = await _transactionsRepository.GetUnsyncedReceiptsAsync(
                    request.IDs
                );

                result.TotalRecords = pendingReceipts?.Count ?? 0;

                if (pendingReceipts == null || !pendingReceipts.Any())
                {
                    result.Success = true;
                    result.Message = "No receipts found";
                    _logger.LogInformation("No receipts found : {Date}", result.SyncDate);
                    return result;
                }

                _logger.LogInformation(
                    "Pending {Count} recipts available to sync",
                    result.TotalRecords
                );

                //await _transactionsRepository.BeginTransactionAsync();

                try
                {
                    foreach (var reciept in pendingReceipts)
                    {
                        await ProcessReceiptAsync(reciept, result);
                    }

                    //await _transactionsRepository.CommitTransactionAsync();

                    result.Success = true;
                    result.Message = BuildSuccessMessage(result);

                    _logger.LogInformation(result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error during receipt processing. Processed {Processed}/{Total}.",
                        result.SyncedRecords,
                        result.TotalRecords
                    );

                    //await _transactionsRepository.RollbackTransactionAsync();

                    result.Success = false;
                    result.Message =
                        $"Unexpected error during; failed after processing {result.SyncedRecords}/{result.TotalRecords}";
                    throw new ReceiptSyncException(result.Message, ex);
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
                result.Message = $"Unexpected error during receipt sync";
                _logger.LogError(ex, result.Message);
                throw new ReceiptSyncException(result.Message, ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        return result;
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
                var sapReceipt = await _mappingHelper.MapXontTransactionToSapReceiptAsync(receipt);

                var sapResult = await _sapClient.SendReceiptAsync(sapReceipt);

                if (sapResult.E_RESULT == "1")
                {
                    receipt.IntegratedStatus = "1";
                    receipt.IntegratedOn = DateTime.Now;
                    receipt.SAPDocumentNumber = sapResult.DOCUMENT_NUMBER;
                    await _transactionsRepository.UpdateTransactionAsync(receipt);
                }
                else
                {
                    _logger.LogError(
                        "Error processing receipt {number} : {sapMessage}",
                        receipt.DocumentNumberSystem,
                        sapResult.E_REASON
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
                _logger.LogError(
                    ex,
                    "Error processing receipt {number} ",
                    receipt.DocumentNumberSystem
                );
                result.FailedRecords += 1;
                throw;
            }
        }
    }

    private string BuildSuccessMessage(ReceiptSyncResultDto result)
    {
        var message = $"Customer sync completed. ";

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
