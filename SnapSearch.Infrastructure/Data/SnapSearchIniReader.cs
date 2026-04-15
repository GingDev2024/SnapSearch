using Microsoft.Extensions.Configuration;
using SnapSearch.Application.Common.Helpers;

namespace SnapSearch.Infrastructure.Data
{
    public static class SnapSearchIniReader
    {
        #region Fields

        private static readonly string BasePath =
            Path.GetDirectoryName(
                System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!;

        #endregion Fields

        #region Public Methods

        public static string BuildConnectionString(IConfiguration appSettings)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(BasePath)
                .AddIniFile(Path.Combine(BasePath, "snapsearch.ini"),
                            optional: true, reloadOnChange: false)
                .Build();

            var machine = Environment.MachineName;
            var section = $"Machine_{machine}";

            // ── Resolve which section to use ─────────────────────────────────
            // Priority: [Machine_PCNAME]  →  [Default]  →  appsettings fallback
            string ResolveValue(string key)
            {
                return config[$"{section}:{key}"]       // per-machine override
                    ?? config[$"Default:{key}"];        // shared default
            }

            var server = ResolveValue("Server");

            if (server is null)
            {
                var fallback = appSettings.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrWhiteSpace(fallback))
                    return fallback;

                throw new InvalidOperationException(
                    $"snapsearch.ini has no [Machine_{machine}] section, no [Default] section, " +
                    $"and appsettings.json has no DefaultConnection.\n" +
                    $"Expected ini at: {BasePath}\\snapsearch.ini");
            }

            var database = ResolveValue("Database")
                ?? throw new InvalidOperationException(
                    "snapsearch.ini is missing Database in both " +
                    $"[Machine_{machine}] and [Default]");

            var encKey = appSettings["EncryptionKey"]
                ?? throw new InvalidOperationException(
                    "EncryptionKey is missing from appsettings.json");

            var rawUser = ResolveValue("User")
                ?? throw new InvalidOperationException(
                    "snapsearch.ini is missing User in both " +
                    $"[Machine_{machine}] and [Default]");

            var rawPass = ResolveValue("Password")
                ?? throw new InvalidOperationException(
                    "snapsearch.ini is missing Password in both " +
                    $"[Machine_{machine}] and [Default]");

            string user, password;
            try
            {
                user = IniEncryptionHelper.Decrypt(rawUser, encKey);
                password = IniEncryptionHelper.Decrypt(rawPass, encKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to decrypt credentials. " +
                    $"The EncryptionKey in appsettings.json does not match the key used to " +
                    $"encrypt the values in snapsearch.ini. Re-generate using the INI Encryptor.", ex);
            }

            var encrypt = ResolveValue("Encrypt") ?? "true";
            var trust = ResolveValue("TrustServerCertificate") ?? "true";

            return $"Data Source={server};Database={database};" +
                   $"User Id={user};Password={password};" +
                   $"Encrypt={encrypt};TrustServerCertificate={trust};";
        }

        #endregion Public Methods
    }
}