using System;
using System.Security.Cryptography;
using System.Text;

namespace GuvenlikDuvarim.Core.Utils
{
    /// <summary>
    /// Windows DPAPI (Data Protection API) kullanarak hassas verileri (GitHub Token vb.)
    /// kullanıcı hesabına özel 256-bit DPAPI şifrelemesi ile korur.
    /// Başka bir bilgisayara veya kullanıcıya kopyalandığında şifre çözülemez.
    /// </summary>
    public static class SecretProtection
    {
        private const string EncPrefix = "ENC:";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("HaYTooL_Firewall_DPAPI_Protection_2026");

        /// <summary>
        /// Düz metin tokenı Windows DPAPI ile şifreler.
        /// </summary>
        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            if (plainText.StartsWith(EncPrefix)) return plainText;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                return EncPrefix + Convert.ToBase64String(cipherBytes);
            }
            catch
            {
                return plainText;
            }
        }

        /// <summary>
        /// DPAPI ile şifrelenmiş tokenın şifresini çözer. Düz metinse doğrudan döner.
        /// Başka kullanıcı veya makinede başarısız olursa boş string döner.
        /// </summary>
        public static string Unprotect(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            if (!cipherText.StartsWith(EncPrefix)) return cipherText;

            try
            {
                string base64 = cipherText.Substring(EncPrefix.Length);
                byte[] cipherBytes = Convert.FromBase64String(base64);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return "";
            }
        }
    }
}
