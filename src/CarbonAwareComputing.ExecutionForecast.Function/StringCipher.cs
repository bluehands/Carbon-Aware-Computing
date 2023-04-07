using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CarbonAwareComputing.ExecutionForecast.Function
{
    public static class StringCipher
    {
        private static byte[] IV =
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
        };

        public static async Task<string> EncryptAsync(string plainText, string passPhrase)
        {
            using Aes aes = Aes.Create();
            aes.Key = DeriveKeyFromPassword(passPhrase);
            aes.IV = IV;
            using MemoryStream output = new();
            await using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await cryptoStream.WriteAsync(Encoding.Unicode.GetBytes(plainText));
            await cryptoStream.FlushFinalBlockAsync();
            return Convert.ToBase64String(output.ToArray());
        }

        public static async Task<string> DecryptAsync(string cipherText, string passPhrase)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = DeriveKeyFromPassword(passPhrase);
                aes.IV = IV;
                using MemoryStream input = new(Convert.FromBase64String(cipherText));
                await using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using MemoryStream output = new();
                await cryptoStream.CopyToAsync(output);
                return Encoding.Unicode.GetString(output.ToArray());
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static byte[] DeriveKeyFromPassword(string password)
        {
            var emptySalt = Array.Empty<byte>();
            var iterations = 1000;
            var desiredKeyLength = 16; // 16 bytes equal 128 bits.
            var hashMethod = HashAlgorithmName.SHA384;
            return Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(password),
                emptySalt,
                iterations,
                hashMethod,
                desiredKeyLength);
        }

    }
}