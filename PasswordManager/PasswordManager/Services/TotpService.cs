using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services
{
    /// <summary>
    /// Generates TOTP codes (RFC 6238) for two-factor authentication entries.
    /// </summary>
    public static class TotpService
    {
        public static string GenerateCode(string base32Secret)
        {
            byte[] key = Base32Decode(base32Secret.ToUpperInvariant().Replace(" ", ""));
            long timeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
            byte[] timeBytes = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian) Array.Reverse(timeBytes);

            using var hmac = new HMACSHA1(key);
            byte[] hash = hmac.ComputeHash(timeBytes);
            int offset = hash[^1] & 0x0F;
            int code = ((hash[offset] & 0x7F) << 24)
                     | ((hash[offset + 1] & 0xFF) << 16)
                     | ((hash[offset + 2] & 0xFF) << 8)
                     | (hash[offset + 3] & 0xFF);
            return (code % 1_000_000).ToString("D6");
        }

        public static int SecondsRemaining() => 30 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 30);

        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            input = input.TrimEnd('=');
            int bits = input.Length * 5;
            byte[] result = new byte[bits / 8];
            int buffer = 0, bitsLeft = 0, idx = 0;
            foreach (char c in input)
            {
                int val = alphabet.IndexOf(c);
                if (val < 0) continue;
                buffer = (buffer << 5) | val;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    result[idx++] = (byte)(buffer >> (bitsLeft - 8));
                    bitsLeft -= 8;
                }
            }
            return result;
        }
    }
}
