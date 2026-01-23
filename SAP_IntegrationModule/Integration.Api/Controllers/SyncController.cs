using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Integration.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Authorize]
public sealed class SyncController : ControllerBase
{
    private readonly ICustomerSyncService _customerSyncService;
    private readonly IMaterialSyncService _materialSyncService;
    private readonly IStockSyncService _stockSyncService;
    private readonly IReceiptSyncService _receiptSyncService;
    private readonly IMaterialStockSyncService _materialStockSyncService;
    private readonly IInvoiceSyncService _invoiceSyncService;

    public SyncController(
        ICustomerSyncService customerSync,
        IMaterialSyncService materialSync,
        IStockSyncService stockSyncService,
        IReceiptSyncService receiptSyncService,
        IMaterialStockSyncService materialStockSyncService,
        IInvoiceSyncService invoiceSyncService
    )
    {
        _customerSyncService = customerSync;
        _materialSyncService = materialSync;
        _stockSyncService = stockSyncService;
        _receiptSyncService = receiptSyncService;
        _materialStockSyncService = materialStockSyncService;
        _invoiceSyncService = invoiceSyncService;
    }

    [HttpPost("customer")]
    public async Task<ActionResult<CustomerSyncResultDto>> SyncCustomers(
        [FromBody] XontCustomerSyncRequestDto request
    )
    {
        var result = await _customerSyncService.SyncCustomersFromSapAsync(request);
        return Ok(result);
    }

    [HttpPost("material")]
    public async Task<ActionResult<MaterialSyncResultDto>> SyncMaterials(
        [FromBody] XontMaterialSyncRequestDto request
    )
    {
        var result = await _materialSyncService.SyncMaterialsFromSapAsync(request);
        return Ok(result);
    }

    [HttpPost("stockout")]
    public async Task<ActionResult<StockOutSapResponseDto>> GetStockOutFromSap(
        [FromBody] StockOutSapRequestDto request
    )
    {
        var result = await _stockSyncService.SyncStockOutFromSapAsync(request);
        return Ok(result);
    }

    [HttpPost("receipt")]
    public async Task<ActionResult<ReceiptSyncResultDto>> SyncReceiptToSAP(
        [FromBody] XontReceiptSyncRequestDto request
    )
    {
        var result = await _receiptSyncService.SyncReceiptToSapAsync(request);
        return Ok(result);
    }

    [HttpPost("materialstock")]
    public async Task<ActionResult<MaterialStockSyncResultDto>> SyncMaterialStockFromSAP(
        [FromBody] XontMaterialStockSyncRequestDto request
    )
    {
        var result = await _materialStockSyncService.SyncMaterialStockFromSapAsync(request);
        return Ok(result);
    }

    [HttpPost("invoice")]
    public async Task<ActionResult<InvoiceSyncResultDto>> SyncInvoiceFromSAP(
        [FromBody] XontInvoiceSyncRequestDto request
    )
    {
        var result = await _invoiceSyncService.SyncInvoiceFromSapAsync(request);
        return Ok(result);
    }

    [HttpGet("status")]
    public IActionResult GetSyncStatus()
    {
        var status = new { IsHealthy = true };

        return Ok(status);
    }
}
