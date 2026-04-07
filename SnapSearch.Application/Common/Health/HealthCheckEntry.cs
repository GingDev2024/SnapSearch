namespace SnapSearch.Application.Common.Health
{
    public enum HealthStatus
    { Healthy, Degraded, Unhealthy }

    public sealed class HealthCheckEntry
    {
        #region Properties

        public string Name { get; init; } = string.Empty;
        public HealthStatus Status { get; init; }
        public string Description { get; init; } = string.Empty;
        public string? Error { get; init; }
        public string Duration { get; init; } = string.Empty;

        public string StatusIcon => Status switch
        {
            HealthStatus.Healthy => "✓",
            HealthStatus.Degraded => "⚠",
            HealthStatus.Unhealthy => "✗",
            _ => "?"
        };

        #endregion Properties
    }

    public sealed class HealthReport
    {
        #region Properties

        public HealthStatus Status { get; init; }
        public string Duration { get; init; } = string.Empty;
        public string Timestamp { get; init; } = string.Empty;
        public List<HealthCheckEntry> Checks { get; init; } = new();

        #endregion Properties
    }
}