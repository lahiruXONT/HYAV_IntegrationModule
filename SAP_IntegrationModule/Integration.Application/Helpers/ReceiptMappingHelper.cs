using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.DTOs;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Helpers;

public class ReceiptMappingHelper
{
    public readonly ILogger<ReceiptMappingHelper> _logger;
    public readonly BusinessUnitResolveHelper _businessUnitResolver;

    public ReceiptMappingHelper(
        ILogger<ReceiptMappingHelper> logger,
        BusinessUnitResolveHelper businessUnitResolver
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _businessUnitResolver =
            businessUnitResolver ?? throw new ArgumentNullException(nameof(businessUnitResolver));
        _businessUnitResolver = businessUnitResolver;
    }

    public async Task<ReceiptRequestDto> MapXontTransactionToSapReceiptAsync(
        Transaction xontReceipt
    )
    {
        try
        {
            var businessUnit = await _businessUnitResolver.GetBusinessUnitDataAsync(
                xontReceipt.BusinessUnit
            );

            var receiptHeader = new ReceiptHeader
            {
                COMP_CODE = businessUnit?.SalesOrganization?.Trim() ?? string.Empty,
                PSTNG_DATE = xontReceipt.PostedDate,
                CURRENCY_ISO = xontReceipt.CurrencyCode,
                REF_DOC_NO = xontReceipt?.DocumentNumber?.Trim() ?? string.Empty,
            };
            var receiptItem = new ReceiptItem
            {
                CUSTOMER = xontReceipt?.CurrencyCode?.Trim() ?? string.Empty,
                GL_ACCOUNT = xontReceipt?.BankAccountNumber?.Trim() ?? string.Empty,
                PROFIT_CTR = string.Empty, //businessUnit?.ProfitCenter?.Trim() ?? string.Empty,
                AMOUNT = xontReceipt?.Amount ?? 0m,
            };
            var receiptObj = new ReceiptRequestDto
            {
                I_HEADER = receiptHeader,
                I_ITEM = new List<ReceiptItem> { receiptItem },
            };

            return receiptObj;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to map Receipt to SAP receipt. BusinessUnit: {BusinessUnit}, DocumentNumber: {DocumentNumberSystem}",
                xontReceipt.BusinessUnit,
                xontReceipt.DocumentNumberSystem
            );
            throw;
        }
    }
}
