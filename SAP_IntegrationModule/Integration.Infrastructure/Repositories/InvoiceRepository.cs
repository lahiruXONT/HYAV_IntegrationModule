using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Integration.Infrastructure.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly UserDbContext _context;

    public InvoiceRepository(UserDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<List<SalesOrderHeader>> GetOrdersByDateRangeAsync(
        string businessunit,
        DateTime fromDate,
        DateTime toDate
    ) =>
        _context
            .SalesOrderHeaders.Where(h =>
                h.BusinessUnit == businessunit
                && h.OrderDate.Date >= fromDate.Date
                && h.OrderDate.Date <= toDate.Date
                && h.IntegratedStatus == "1"
            )
            .OrderBy(h => h.OrderDate)
            .ToListAsync();

    public Task<List<ERPInvoicedOrderDetail>> GetERPInvoiceDataByOrderAsync(
        string orderNo,
        string businessUnit,
        string customerCode,
        string executiveCode,
        string territoryCode
    ) =>
        _context
            .ERPInvoicedOrderDetails.Where(p =>
                p.BusinessUnit == businessUnit
                && p.TerritoryCode == territoryCode
                && p.ExecutiveCode == executiveCode
                && p.CustomerCode == customerCode
                && p.OrderNo == orderNo
            )
            .ToListAsync();

    public async Task CreateERPInvoicedOrderDetailAsync(ERPInvoicedOrderDetail rec)
    {
        await _context.ERPInvoicedOrderDetails.AddAsync(rec);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateERPInvoicedOrderDetailAsync(ERPInvoicedOrderDetail rec)
    {
        _context.ERPInvoicedOrderDetails.Update(rec);
        await _context.SaveChangesAsync();
    }
}
