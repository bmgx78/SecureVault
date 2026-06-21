using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services
{
    /// <summary>
    /// AES-256-GCM encryption with PBKDF2 key derivation.
    /// The master password never leaves the device — only the encrypted blob is synced.
    /// </summary>
    public class EncryptionService
    {
        private const int SaltSize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int KeySize = 32;
        private const int Pbkdf2Iterations = 600_000;

        public static byte[] DeriveKey(string masterPassword, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(masterPassword),
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256);
            return kdf.GetBytes(KeySize);
        }

        public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSize);
        public static byte[] GenerateNonce() => RandomNumberGenerator.GetBytes(NonceSize);

        /// <summary>Encrypts plaintext with AES-256-GCM. Returns salt+nonce+tag+ciphertext as Base64.</summary>
        public static string Encrypt(string plaintext, byte[] key)
        {
            byte[] nonce = GenerateNonce();
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            byte[] combined = new byte[NonceSize + TagSize + ciphertext.Length];
            nonce.CopyTo(combined, 0);
            tag.CopyTo(combined, NonceSize);
            ciphertext.CopyTo(combined, NonceSize + TagSize);

            return Convert.ToBase64String(combined);
        }

        /// <summary>Decrypts a Base64 blob produced by Encrypt.</summary>
        public static string Decrypt(string encryptedBase64, byte[] key)
        {
            byte[] combined = Convert.FromBase64String(encryptedBase64);

            byte[] nonce = combined[..NonceSize];
            byte[] tag = combined[NonceSize..(NonceSize + TagSize)];
            byte[] ciphertext = combined[(NonceSize + TagSize)..];
            byte[] plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }

        /// <summary>Encrypts the entire vault JSON payload. Returns a self-contained blob with salt.</summary>
        public static string EncryptVault(string vaultJson, string masterPassword)
        {
            byte[] salt = GenerateSalt();
            byte[] key = DeriveKey(masterPassword, salt);
            string encrypted = Encrypt(vaultJson, key);

            string saltB64 = Convert.ToBase64String(salt);
            return $"{saltB64}:{encrypted}";
        }

        /// <summary>Decrypts a vault blob produced by EncryptVault.</summary>
        public static string DecryptVault(string vaultBlob, string masterPassword)
        {
            int sep = vaultBlob.IndexOf(':');
            if (sep < 0) throw new InvalidDataException("Invalid vault format.");

            byte[] salt = Convert.FromBase64String(vaultBlob[..sep]);
            string encrypted = vaultBlob[(sep + 1)..];
            byte[] key = DeriveKey(masterPassword, salt);
            return Decrypt(encrypted, key);
        }

        /// <summary>Scores a password 0-100.</summary>
        public static int ScorePassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;
            int score = 0;
            if (password.Length >= 8) score += 10;
            if (password.Length >= 12) score += 10;
            if (password.Length >= 16) score += 10;
            if (password.Length >= 20) score += 10;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")) score += 10;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")) score += 10;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]")) score += 10;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]")) score += 20;
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"(.)\1{2,}")) score += 10;
            return Math.Min(score, 100);
        }

        /// <summary>Generates a secure random password.</summary>
        public static string GeneratePassword(
            int length = 20,
            bool upper = true,
            bool digits = true,
            bool symbols = true,
            bool excludeAmbiguous = false)
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upperStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digitsStr = "0123456789";
            const string symbolsStr = "!@#$%^&*()-_=+[]{}|;:,.<>?";
            const string ambiguous = "0Oo1lI";

            string charset = lower;
            if (upper) charset += upperStr;
            if (digits) charset += digitsStr;
            if (symbols) charset += symbolsStr;
            if (excludeAmbiguous) charset = string.Concat(charset.Where(c => !ambiguous.Contains(c)));

            var result = new char[length];
            var bytes = RandomNumberGenerator.GetBytes(length * 4);
            for (int i = 0; i < length; i++)
            {
                uint rand = BitConverter.ToUInt32(bytes, i * 4);
                result[i] = charset[(int)(rand % charset.Length)];
            }
            return new string(result);
        }

        /// <summary>Generates a memorable passphrase from random words.</summary>
        public static string GeneratePassphrase(int wordCount = 4, string separator = "-")
        {
            string[] words = {
                "apple","brave","cloud","dance","eagle","flame","grace","honey",
                "ivory","jewel","knave","lunar","maple","noble","ocean","pearl",
                "quake","river","stone","tiger","umbra","vapor","wrath","xenon",
                "yield","zonal","amber","blaze","coral","delta","ember","frost",
                "glass","hazel","inlet","jelly","karma","lemon","magic","night",
                "orbit","piano","quest","radar","solar","tulip","ultra","vivid",
                "water","xylop","yacht","zebra"
            };
            var selected = new string[wordCount];
            var bytes = RandomNumberGenerator.GetBytes(wordCount * 4);
            for (int i = 0; i < wordCount; i++)
            {
                uint rand = BitConverter.ToUInt32(bytes, i * 4);
                selected[i] = words[rand % words.Length];
            }
            return string.Join(separator, selected);
        }
    }
}
