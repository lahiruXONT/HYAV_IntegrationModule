using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Domain.Entities;

namespace Integration.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<List<SalesOrderHeader>> GetByOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<List<ERPInvoicedOrderDetail>> GetERPInvoiceDataByOrderAsync(
        long orderNo,
        string businessUnit,
        string customerCode,
        string executiveCode,
        string territoryCode
    );
    Task CreateERPInvoicedOrderDetailAsync(ERPInvoicedOrderDetail rec);
    Task UpdateERPInvoicedOrderDetailAsync(ERPInvoicedOrderDetail rec);
}
