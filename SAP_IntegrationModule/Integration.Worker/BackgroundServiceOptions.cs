using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Worker
{
    public enum BackgroundServiceState
    {
        Starting,
        Running,
        Paused,
        Stopped,
        Failed,
    }

    public class BackgroundServiceOptions
    {
        public bool IsEnabled { get; set; } = true;
        public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
        public string? DailyScheduleTime { get; set; }
        public int MaxConsecutiveFailures { get; set; } = 10;
    }

    public class HealthMetrics
    {
        public DateTime ServiceStartTime { get; set; }
        public DateTime? ServiceStopTime { get; set; }
        public int TotalCycles { get; set; }
        public int SuccessfulCycles { get; set; }
        public int FailedCycles { get; set; }
        public DateTime? LastSuccessfulRun { get; set; }
        public TimeSpan? LastRunDuration { get; set; }
        public double SuccessRate => TotalCycles > 0 ? (SuccessfulCycles * 100.0) / TotalCycles : 0;
    }
}
