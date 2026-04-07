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

            if (config[$"{section}:Server"] is null)
            {
                var fallback = appSettings.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrWhiteSpace(fallback))
                    return fallback;

                throw new InvalidOperationException(
                    $"snapsearch.ini has no [Machine_{machine}] section and " +
                    $"appsettings.json has no DefaultConnection. " +
                    $"Expected ini at: {BasePath}\\snapsearch.ini");
            }

            var server = config[$"{section}:Server"]!;

            var database = config[$"{section}:Database"]
                ?? throw new InvalidOperationException(
                    $"snapsearch.ini is missing [Machine_{machine}]:Database");

            var encKey = appSettings["EncryptionKey"]
                ?? throw new InvalidOperationException(
                    "EncryptionKey is missing from appsettings.json");

            var rawUser = config[$"{section}:User"]
                ?? throw new InvalidOperationException(
                    $"snapsearch.ini is missing [Machine_{machine}]:User");

            var rawPass = config[$"{section}:Password"]
                ?? throw new InvalidOperationException(
                    $"snapsearch.ini is missing [Machine_{machine}]:Password");

            // Decrypt — wrap so a key mismatch gives an actionable message
            string user, password;
            try
            {
                user = IniEncryptionHelper.Decrypt(rawUser, encKey);
                password = IniEncryptionHelper.Decrypt(rawPass, encKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to decrypt credentials for [Machine_{machine}]. " +
                    $"The EncryptionKey in appsettings.json does not match the key " +
                    $"that was used to encrypt the values in snapsearch.ini. " +
                    $"Re-generate the encrypted values using the INI Encryptor with " +
                    $"the current key and update snapsearch.ini.", ex);
            }

            var encrypt = config[$"{section}:Encrypt"] ?? "true";
            var trust = config[$"{section}:TrustServerCertificate"] ?? "true";

            return $"Data Source={server};Database={database};" +
                   $"User Id={user};Password={password};" +
                   $"Encrypt={encrypt};TrustServerCertificate={trust};";
        }

        #endregion Public Methods
    }
}