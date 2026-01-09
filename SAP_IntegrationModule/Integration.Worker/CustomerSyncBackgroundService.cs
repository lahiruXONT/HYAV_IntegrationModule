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

        var request = new XontCustomerSyncRequestDto
        {
            Date = DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd"),
        };

        var result = await syncService.SyncCustomersFromSapAsync(request);

        if (result.Success)
        {
            _logger.LogInformation("Customer sync completed {@Result}", result);
        }
        else
        {
            _logger.LogWarning("Customer sync completed with issues {@Result}", result);

            if (result.TotalRecords > 0 && result.NewCustomers + result.UpdatedCustomers == 0)
            {
                throw new InvalidOperationException(
                    $"Customer sync processed zero records. {result.Message}"
                );
            }
        }
    }

}
