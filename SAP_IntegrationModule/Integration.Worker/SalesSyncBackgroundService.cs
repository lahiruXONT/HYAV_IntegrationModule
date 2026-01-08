using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Integration.Worker;

public sealed class SalesSyncBackgroundService : ResilientBackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public SalesSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SalesSyncBackgroundService> logger,
        IOptionsMonitor<BackgroundServiceOptions> optionsMonitor
    )
        : base(logger, optionsMonitor, nameof(SalesSyncBackgroundService))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ISalesSyncService>();

        var request = new SalesOrderDto
        {
            //Date = DateTime.UtcNow.AddDays(-1).ToString("yyyyMMdd")
        };

        var result = await syncService.SyncSalesAsync(request);

        if (result.Success)
        {
            _logger.LogInformation("Sales Order sync completed {@Result}", result);
        }
        else
        {
            _logger.LogWarning("Sales Order completed with issues {@Result}", result);

            if (result.TotalRecords > 0 && result.NewCustomers + result.UpdatedCustomers == 0)
            {
                throw new InvalidOperationException(
                    $"Sales Order processed zero records. {result.Message}"
                );
            }
        }
    }
}
