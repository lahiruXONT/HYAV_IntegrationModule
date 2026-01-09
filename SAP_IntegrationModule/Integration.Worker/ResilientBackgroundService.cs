using Integration.Application.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Serilog.Context;

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
    private readonly HealthMetrics _healthMetrics = new();

    public ResilientBackgroundService(
        ILogger logger,
        IOptionsMonitor<BackgroundServiceOptions> optionsMonitor,
        string serviceName
    )
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _serviceName = serviceName;

        _retryPolicy = Policy
            .Handle<Exception>(IsTransientException)
            .WaitAndRetryAsync(
                retryCount: 3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, retry, _) =>
                {
                    _logger.LogWarning(
                        ex,
                        "{ServiceName} transient retry {Retry} after {Delay}s",
                        _serviceName,
                        retry,
                        delay.TotalSeconds
                    );
                }
            );

        _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var executionId = Guid.NewGuid().ToString("N");

        using (LogContext.PushProperty("ServiceName", _serviceName))
        using (LogContext.PushProperty("ExecutionId", executionId))
        {
            _state = BackgroundServiceState.Starting;
            _healthMetrics.ServiceStartTime = DateTime.UtcNow;

            _logger.LogInformation(
                "{ServiceName} starting with execution ID: {ExecutionId}",
                _serviceName,
                executionId
            );

            var options = _optionsMonitor.Get(_serviceName);

            if (options.InitialDelay > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "{ServiceName} initial delay {Delay}",
                    _serviceName,
                    options.InitialDelay
                );
                await Task.Delay(options.InitialDelay, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var cycleStartTime = DateTime.UtcNow;
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
                    _logger.LogInformation("{ServiceName} starting execution cycle", _serviceName);

                    await ExecuteOnceAsync(stoppingToken);

                    _consecutiveFailures = 0;
                    _lastSuccessfulRun = DateTime.UtcNow;
                    _healthMetrics.SuccessfulCycles++;
                    _healthMetrics.LastSuccessfulRun = DateTime.UtcNow;

                    var delay = CalculateNextRunDelay(options);

                    _logger.LogInformation(
                        "{ServiceName} completed successfully in {Duration}. Next run in {Delay}",
                        _serviceName,
                        DateTime.UtcNow - cycleStartTime,
                        delay
                    );

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
                    _healthMetrics.FailedCycles++;
                    await HandleFailureAsync(ex, options, stoppingToken, cycleStartTime);
                }
                finally
                {
                    _healthMetrics.TotalCycles++;
                    _healthMetrics.LastRunDuration = DateTime.UtcNow - cycleStartTime;
                }
            }

            _state = BackgroundServiceState.Stopped;
            _healthMetrics.ServiceStopTime = DateTime.UtcNow;

            LogHealthMetrics();
            _logger.LogInformation("{ServiceName} stopped", _serviceName);
        }
    }

    private async Task ExecuteOnceAsync(CancellationToken token)
    {
        CorrelationContext.CorrelationId = $"WORKER-{Guid.NewGuid()}";

        using (LogContext.PushProperty("CorrelationId", CorrelationContext.CorrelationId))
        {
            _logger.LogInformation(
                "{ServiceName} execution started with correlation ID: {CorrelationId}",
                _serviceName,
                CorrelationContext.CorrelationId
            );

            await _retryPolicy.ExecuteAsync(ct => ExecuteCycleAsync(ct), token);

            _logger.LogInformation("{ServiceName} execution finished", _serviceName);
        }
    }

    protected abstract Task ExecuteCycleAsync(CancellationToken stoppingToken);

    private async Task HandleFailureAsync(
        Exception ex,
        BackgroundServiceOptions options,
        CancellationToken token,
        DateTime cycleStartTime
    )
    {
        using (LogContext.PushProperty("FailureCount", _consecutiveFailures))
        {
            _logger.LogError(
                ex,
                "{ServiceName} failed ({Failures}/{MaxFailures}) after {Duration}",
                _serviceName,
                _consecutiveFailures,
                options.MaxConsecutiveFailures,
                DateTime.UtcNow - cycleStartTime
            );

            LogDetailedError(ex);

            if (_consecutiveFailures >= options.MaxConsecutiveFailures)
            {
                _state = BackgroundServiceState.Failed;

                _logger.LogCritical(
                    "{ServiceName} marked FAILED after {Failures} consecutive errors",
                    _serviceName,
                    _consecutiveFailures
                );

                return;
            }

            var delay = CalculateBackoffDelay(_consecutiveFailures);

            _logger.LogWarning("{ServiceName} retrying after {Delay}", _serviceName, delay);

            await Task.Delay(delay, token);
        }
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
            try
            {
                var today = DateTime.UtcNow.Date;
                var scheduled = today.Add(TimeSpan.Parse(options.DailyScheduleTime));

                if (scheduled <= DateTime.UtcNow)
                    scheduled = scheduled.AddDays(1);

                return scheduled - DateTime.UtcNow;
            }
            catch (FormatException)
            {
                return options.Interval;
            }
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
        var errorDetails = new
        {
            Service = _serviceName,
            State = _state.ToString(),
            Failures = _consecutiveFailures,
            LastSuccess = _lastSuccessfulRun,
            ErrorType = ex.GetType().Name,
            ErrorMessage = ex.Message,
            StackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length)),
            InnerException = ex.InnerException?.Message,
        };

        _logger.LogError("{ServiceName} error details {@ErrorDetails}", _serviceName, errorDetails);
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
                options.DailyScheduleTime
            );
        }
    }

    private void LogHealthMetrics()
    {
        _logger.LogInformation(
            "{ServiceName} health metrics: {@Metrics}",
            _serviceName,
            _healthMetrics
        );
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is HttpRequestException
            || ex is TimeoutException
            || ex is TaskCanceledException
            || (ex is SqlException sqlEx && IsTransientSqlError(sqlEx.Number));
    }

    private static bool IsTransientSqlError(int errorNumber)
    {
        int[] transientErrors = { 1205, 4060, 40197, 40501, 40613, 49918, 49919, 49920, 4221 };
        return transientErrors.Contains(errorNumber);
    }
}
