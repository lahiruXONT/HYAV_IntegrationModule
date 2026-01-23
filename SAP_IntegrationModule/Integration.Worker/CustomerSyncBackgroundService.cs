using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Integration.Worker;

public class CustomerSyncBackgroundService : ResilientBackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public CustomerSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CustomerSyncBackgroundService> logger,
        IOptionsMonitor<BackgroundServiceOptions> optionsMonitor
    )
        : base(logger, optionsMonitor, nameof(CustomerSyncBackgroundService))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteCycleAsync(CancellationToken stoppingToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var syncService = scope.ServiceProvider.GetRequiredService<ICustomerSyncService>();

        try
        {
            var request = new XontCustomerSyncRequestDto
            {
                Date = DateTime.Now.AddDays(-1).ToString("yyyyMMdd"),
            };

            var result = await syncService.SyncCustomersFromSapAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("Customer sync completed {@Result}", result);
            }
            else
            {
                _logger.LogWarning("Customer sync completed with issues {@Result}", result);
            }
        }
        catch (SapApiExceptionDto ex)
        {
            _logger.LogError("Customer sync failed with SAP Issue");
            throw;
        }
        catch (CustomerSyncException ex)
        {
            _logger.LogError("Customer sync failed with Issues");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer sync failed with issues");

            throw;
        }
    }
}
