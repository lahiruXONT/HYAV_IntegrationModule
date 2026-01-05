using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Serilog.Context;
using System.Text.Json;

namespace Integration.Worker;

public abstract class ResilientBackgroundService : BackgroundService
{
    protected readonly ILogger _logger;
    private readonly string _serviceName;
    private readonly IOptionsMonitor<BackgroundServiceOptions> _optionsMonitor;
    private readonly AsyncRetryPolicy _retryPolicy;

    private int _consecutiveFailures;
    private DateTime? _lastSuccessfulRun;
    private BackgroundServiceState _state = BackgroundServiceState.Stopped;

    protected ResilientBackgroundService(
        ILogger logger,
        IOptionsMonitor<BackgroundServiceOptions> optionsMonitor,
        string serviceName)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _serviceName = serviceName;

        _retryPolicy = Policy.Handle<Exception>(IsTransientException).WaitAndRetryAsync(
                retryCount: 3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, delay, retry, _) =>
                {
                    _logger.LogWarning( ex, "{ServiceName} transient retry {Retry} after {Delay}s",_serviceName, retry, delay.TotalSeconds);
                });

        _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (LogContext.PushProperty("ServiceName", _serviceName))
        {
            _state = BackgroundServiceState.Starting;
            _logger.LogInformation("{ServiceName} starting", _serviceName);

            var options = _optionsMonitor.Get(_serviceName);

            if (options.InitialDelay > TimeSpan.Zero)
            {
                _logger.LogInformation("{ServiceName} initial delay {Delay}", _serviceName, options.InitialDelay);

                await Task.Delay(options.InitialDelay, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                options = _optionsMonitor.Get(_serviceName);

                if (!options.IsEnabled)
                {
                    PauseService();
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                _state = BackgroundServiceState.Running;

                try
                {
                    await ExecuteOnceAsync(stoppingToken);

                    _consecutiveFailures = 0;
                    _lastSuccessfulRun = DateTime.UtcNow;

                    var delay = CalculateNextRunDelay(options);

                    _logger.LogInformation("{ServiceName} completed successfully. Next run in {Delay}", _serviceName, delay);

                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("{ServiceName} cancellation requested", _serviceName);
                    break;
                }
                catch (Exception ex)
                {
                    _consecutiveFailures++;
                    await HandleFailureAsync(ex, options, stoppingToken);
                }
            }

            _state = BackgroundServiceState.Stopped;
            _logger.LogInformation("{ServiceName} stopped", _serviceName);
        }
    }

    private async Task ExecuteOnceAsync(CancellationToken token)
    {
        var executionId = Guid.NewGuid().ToString("N");

        using (LogContext.PushProperty("ExecutionId", executionId))
        using (LogContext.PushProperty("CorrelationId", executionId))
        {
            _logger.LogInformation("{ServiceName} execution started", _serviceName);

            await _retryPolicy.ExecuteAsync(ct => ExecuteCycleAsync(ct),token);

            _logger.LogInformation("{ServiceName} execution finished", _serviceName);
        }
    }

    protected abstract Task ExecuteCycleAsync(CancellationToken stoppingToken);

    private async Task HandleFailureAsync(
        Exception ex,
        BackgroundServiceOptions options,
        CancellationToken token)
    {
        _logger.LogError( ex,"{ServiceName} failed ({Failures}/{MaxFailures})", _serviceName, _consecutiveFailures, options.MaxConsecutiveFailures);

        LogDetailedError(ex);

        if (_consecutiveFailures >= options.MaxConsecutiveFailures)
        {
            _state = BackgroundServiceState.Failed;

            _logger.LogCritical("{ServiceName} marked FAILED after {Failures} consecutive errors", _serviceName, _consecutiveFailures);

            return;
        }

        var delay = CalculateBackoffDelay(_consecutiveFailures);

        _logger.LogWarning("{ServiceName} retrying after {Delay}", _serviceName, delay);

        await Task.Delay(delay, token);
    }

    private void PauseService()
    {
        if (_state != BackgroundServiceState.Paused)
        {
            _state = BackgroundServiceState.Paused;
            _logger.LogInformation("{ServiceName} paused via configuration", _serviceName);
        }
    }

    private static TimeSpan CalculateNextRunDelay(BackgroundServiceOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.DailyScheduleTime))
        {
            var today = DateTime.UtcNow.Date;
            var scheduled = today.Add(TimeSpan.Parse(options.DailyScheduleTime));

            if (scheduled <= DateTime.UtcNow)
                scheduled = scheduled.AddDays(1);

            return scheduled - DateTime.UtcNow;
        }

        return options.Interval;
    }

    private static TimeSpan CalculateBackoffDelay(int failures)
    {
        var baseMinutes = Math.Pow(2, Math.Min(failures - 1, 6));
        var jitter = Random.Shared.NextDouble() * 0.3 + 0.85;
        return TimeSpan.FromMinutes(baseMinutes * jitter);
    }

    private void LogDetailedError(Exception ex)
    {
        var details = new
        {
            Service = _serviceName,
            State = _state.ToString(),
            Failures = _consecutiveFailures,
            LastSuccess = _lastSuccessfulRun,
            Error = ex.Message,
            StackTrace = ex.StackTrace
        };

        _logger.LogError("{ServiceName} error details {@Details}", _serviceName, details);
    }

    private void OnConfigurationChanged(BackgroundServiceOptions options, string name)
    {
        if (name == _serviceName)
        {
            _logger.LogInformation(
                "{ServiceName} config changed: Enabled={Enabled}, Interval={Interval}, Schedule={Schedule}",
                _serviceName,
                options.IsEnabled,
                options.Interval,
                options.DailyScheduleTime);
        }
    }

    private static bool IsTransientException(Exception ex)
        => ex is HttpRequestException
        || ex is TimeoutException
        || ex is TaskCanceledException;
}

public enum BackgroundServiceState
{
    Starting,
    Running,
    Paused,
    Stopped,
    Failed
}

public class BackgroundServiceOptions
{
    public bool IsEnabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
    public string? DailyScheduleTime { get; set; }
    public int MaxConsecutiveFailures { get; set; } = 10;
}
