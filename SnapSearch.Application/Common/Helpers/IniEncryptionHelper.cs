using System.Security.Cryptography;
using System.Text;

namespace SnapSearch.Application.Common.Helpers
{
    public static class IniEncryptionHelper
    {
        #region Fields

        private const string Prefix = "AES:";

        #endregion Fields

        #region Public Methods

        public static string Encrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = GetKeyBytes(key);
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var plain = Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(plain, 0, plain.Length);
            var result = new byte[aes.IV.Length + encrypted.Length];
            aes.IV.CopyTo(result, 0);
            encrypted.CopyTo(result, aes.IV.Length);
            return Prefix + Convert.ToBase64String(result);
        }

        public static string Decrypt(string value, string key)
        {
            if (!value.StartsWith(Prefix, StringComparison.Ordinal))
                return value; // plain-text passthrough (backwards compat)

            var data = Convert.FromBase64String(value[Prefix.Length..]);
            using var aes = Aes.Create();
            aes.Key = GetKeyBytes(key);
            aes.IV = data[..16];
            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(data, 16, data.Length - 16);
            return Encoding.UTF8.GetString(decrypted);
        }

        /// <summary>Returns true when the value carries the AES: prefix.</summary>
        public static bool IsEncrypted(string value) =>
            value.StartsWith(Prefix, StringComparison.Ordinal);

        #endregion Public Methods

        #region Private Methods

        private static byte[] GetKeyBytes(string key)
        {
            var keyBytes = new byte[32];
            var raw = Encoding.UTF8.GetBytes(key);
            Array.Copy(raw, keyBytes, Math.Min(raw.Length, keyBytes.Length));
            return keyBytes;
        }

        #endregion Private Methods
    }
}