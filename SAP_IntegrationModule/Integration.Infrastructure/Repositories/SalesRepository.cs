using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Integration.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Integration.Infrastructure.Repositories;

public sealed class SalesRepository : ISalesRepository, IAsyncDisposable
{
    private readonly UserDbContext _context;
    private IDbContextTransaction? _transaction;

    public SalesRepository(UserDbContext context)
    {
        _context = context;
    }

    //public async Task BeginTransactionAsync()
    //{
    //    _transaction = await _context.Database.BeginTransactionAsync();
    //}

    //public async Task CommitTransactionAsync()
    //{
    //    await _context.SaveChangesAsync();
    //    await _transaction!.CommitAsync();
    //    await _transaction.DisposeAsync();
    //    _transaction = null;
    //}

    //public async Task RollbackTransactionAsync()
    //{
    //    if (_transaction != null)
    //    {
    //        await _transaction.RollbackAsync();
    //        await _transaction.DisposeAsync();
    //        _transaction = null;
    //    }

    //    _context.ChangeTracker.Clear();
    //}

    public async Task<List<SalesOrderHeader>> GetXontSalesOrderAsync(DateTime currentDate)
    {
        return await _context
            .SalesOrderHeaders.Include(h => h.Lines)
            .Include(h => h.Discounts)
            .Where(h =>
                h.OrderDate <= currentDate && h.OrderComplete == "1" && h.IntegratedStatus == "0"
            )
            .OrderBy(h => h.OrderDate)
            .ToListAsync();
    }

    public async Task<SalesOrderHeader?> UpdateOrderStatusAsync(SalesOrderHeader order)
    {
        order.IntegratedStatus = "1";
        order.IntegratedOn = DateTime.UtcNow;

        _context.SalesOrderHeaders.Update(order);
        await _context.SaveChangesAsync();

        return order;
    }

    //public async Task<SalesInvoiceResponseDto?> GetSapSalesInvoiceAsync(
    //    SalesInvoiceRequestDto request
    //)
    //{
    //    var invoice = await _context
    //        .SalesInvoiceHeaders.AsNoTracking()
    //        .Where(i => i.OrderNo == request.OrderNo)
    //        .Select(i => new SalesInvoiceResponseDto
    //        {
    //            OrderNo = i.OrderNo,
    //            InvoiceDate = i.InvoiceDate,
    //            Status = i.DeliveryStatus,
    //            TotalInvoiceValue = i.TotalInvoiceValue,
    //        })
    //        .FirstOrDefaultAsync();

    //    return invoice;
    //}

    //public async Task<bool> UpdateSalesInvoiceInquiryAsync(SalesInvoiceResponseDto request)
    //{
    //    var invoice = await _context
    //        .SalesInvoiceHeaders.AsNoTracking()
    //        .Where(i => i.OrderNo == request.OrderNo)
    //        .Select(i => new SalesInvoiceResponseDto
    //        {
    //            OrderNo = i.OrderNo,
    //            InvoiceDate = i.InvoiceDate,
    //            Status = i.DeliveryStatus,
    //            TotalInvoiceValue = i.TotalInvoiceValue,
    //        })
    //        .FirstOrDefaultAsync();

    //    return true;
    //}

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
            await _transaction.DisposeAsync();
    }
}
