using System;
using System.Globalization;
using Integration.Application.DTOs;
using Integration.Domain.Entities;

namespace Integration.Application.Helpers;

public class InvoiceMappingHelper
{
    public async Task<ERPInvoicedOrderDetail> MapToERPInvoicedOrderDetail(
        SapInvoiceResponseDto sapInvoice,
        SalesOrderHeader order
    )
    {
        return new ERPInvoicedOrderDetail
        {
            BusinessUnit = order.BusinessUnit,
            TerritoryCode = order.TerritoryCode,
            ExecutiveCode = order.ExecutiveCode,
            CustomerCode = order.RetailerCode,
            OrderNo = sapInvoice.OrderNumber,
            OrderDate = order.OrderDate,
            InvoiceDate = ParseInvoiceDateOrOrderDate(
                sapInvoice.InvoiceDate,
                order.OrderDate,
                sapInvoice.InvoiceStatus
            ),

            TotalGoodsValue = order.TotalGoodsValue,
            TotalInvoiceValue = sapInvoice.TotalInvoiceValue,
            Status = sapInvoice.InvoiceStatus,
            CreatedBy = "SAPSYNC",
            CreatedOn = DateTime.Now,
            UpdatedBy = "SAPSYNC",
            UpdatedOn = DateTime.Now,
        };
    }

    private static DateTime ParseInvoiceDateOrOrderDate(
        string? sapInvoiceDate,
        DateTime orderDate,
        string invoiceStatus
    )
    {
        if (invoiceStatus == "O" && string.IsNullOrWhiteSpace(sapInvoiceDate))
            return TrimToSeconds(orderDate);
        else if (string.IsNullOrWhiteSpace(sapInvoiceDate))
            throw new ValidationExceptionDto(
                $"InvoiceDate is mandatory but was not provided by SAP"
            );

        if (DateTime.TryParse(sapInvoiceDate, out var parsed))
            return TrimToSeconds(parsed);

        if (
            DateTime.TryParseExact(
                sapInvoiceDate,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed
            )
        )
            return parsed;

        throw new ValidationExceptionDto(
            $"Invalid InvoiceDate format received from SAP: '{sapInvoiceDate}'"
        );
    }

    private static DateTime TrimToSeconds(DateTime dt) =>
        new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
}
