using Microsoft.Extensions.Configuration;
using SnapSearch.Application.Common.Health;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Infrastructure.Data;
using System.Diagnostics;

namespace SnapSearch.Infrastructure.Services
{
    public sealed class HealthCheckService : IHealthCheckService
    {
        #region Fields

        private static readonly string IniPath = Path.Combine(
            Path.GetDirectoryName(
                Process.GetCurrentProcess().MainModule!.FileName)!,
            "snapsearch.ini");

        private readonly IConfiguration _configuration;

        #endregion Fields

        #region Public Constructors

        public HealthCheckService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<HealthReport> CheckAsync()
        {
            var overall = Stopwatch.StartNew();
            var checks = new List<HealthCheckEntry>();

            checks.Add(await CheckIniFileAsync());
            checks.Add(CheckEncryptionKey());
            checks.Add(await CheckDatabaseAsync());

            overall.Stop();

            var worstStatus = checks.Any(c => c.Status == HealthStatus.Unhealthy)
                ? HealthStatus.Unhealthy
                : checks.Any(c => c.Status == HealthStatus.Degraded)
                    ? HealthStatus.Degraded
                    : HealthStatus.Healthy;

            return new HealthReport
            {
                Status = worstStatus,
                Duration = $"{overall.ElapsedMilliseconds}ms",
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Checks = checks
            };
        }

        #endregion Public Methods

        #region Private Methods

        private static HealthCheckEntry Entry(
            string name, HealthStatus status, string description,
            string? error, Stopwatch sw)
        {
            sw.Stop();
            return new HealthCheckEntry
            {
                Name = name,
                Status = status,
                Description = description,
                Error = error,
                Duration = $"{sw.ElapsedMilliseconds}ms"
            };
        }

        private Task<HealthCheckEntry> CheckIniFileAsync()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (!File.Exists(IniPath))
                    return Task.FromResult(Entry("INI File", HealthStatus.Unhealthy,
                        "snapsearch.ini not found next to the executable.",
                        $"Expected: {IniPath}", sw));

                var machine = Environment.MachineName;
                var section = $"Machine_{machine}";

                var config = new ConfigurationBuilder()
                    .AddIniFile(IniPath, optional: true)
                    .Build();

                var hasMachineSection = config[$"{section}:Server"] is not null;
                var hasDefaultSection = config["Default:Server"] is not null;

                if (!hasMachineSection && !hasDefaultSection)
                    return Task.FromResult(Entry("INI File", HealthStatus.Unhealthy,
                        "File found but no usable section.",
                        $"Add a [Default] or [{section}] block to {IniPath}", sw));

                var activeSection = hasMachineSection ? $"[{section}]" : "[Default]";

                return Task.FromResult(Entry("INI File", HealthStatus.Healthy,
                    $"Using {activeSection} → Server: {config[$"{(hasMachineSection ? section : "Default")}:Server"]}",
                    null, sw));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Entry("INI File", HealthStatus.Unhealthy,
                    "Failed to read snapsearch.ini.", ex.Message, sw));
            }
        }

        private HealthCheckEntry CheckEncryptionKey()
        {
            var sw = Stopwatch.StartNew();
            var key = _configuration["EncryptionKey"];

            if (string.IsNullOrWhiteSpace(key))
                return Entry("Encryption Key", HealthStatus.Unhealthy,
                    "EncryptionKey is missing from appsettings.json.", null, sw);

            if (key.Length < 16)
                return Entry("Encryption Key", HealthStatus.Degraded,
                    "EncryptionKey is set but shorter than 16 characters.", null, sw);

            return Entry("Encryption Key", HealthStatus.Healthy,
                "EncryptionKey is present in appsettings.json.", null, sw);
        }

        private async Task<HealthCheckEntry> CheckDatabaseAsync()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var cs = SnapSearchIniReader.BuildConnectionString(_configuration);
                await using var ctx = new AppDbContext(_configuration);
                await ctx.OpenAsync();

                // lightweight round-trip
                var cmd = ctx.Connection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.CommandTimeout = 5;
                await cmd.ExecuteScalarAsync();

                return Entry("Database", HealthStatus.Healthy,
                    "Connection established and query succeeded.", null, sw);
            }
            catch (Exception ex)
            {
                return Entry("Database", HealthStatus.Unhealthy,
                    "Could not connect to SQL Server.", ex.Message, sw);
            }
        }

        #endregion Private Methods
    }
}