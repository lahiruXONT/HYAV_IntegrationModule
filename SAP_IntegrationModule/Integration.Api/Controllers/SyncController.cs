using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Integration.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Integration.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ICustomerSyncService _customerSyncService;
    private readonly IMaterialSyncService _materialSyncService;

    public SyncController(ICustomerSyncService customerSync, IMaterialSyncService materialSync)
    {
        _customerSyncService = customerSync;
        _materialSyncService = materialSync;
    }

    [HttpPost("customer")]
    public async Task<ActionResult<CustomerSyncResultDto>> SyncCustomers([FromBody] XontCustomerSyncRequestDto request)
    {
        var result = await _customerSyncService.SyncCustomersFromSapAsync(request);
        return Ok(result);

    }

    [HttpPost("material")]
    public async Task<ActionResult<MaterialSyncResultDto>> SyncMaterials([FromBody] XontMaterialSyncRequestDto request)
    {

        var result = await _materialSyncService.SyncMaterialsFromSapAsync(request);
        return Ok(result);

    }

    [HttpGet("status")]
    public IActionResult GetSyncStatus()
    {
        var status = new
        {
           
            IsHealthy = true
        };

        return Ok(status);
    }
}