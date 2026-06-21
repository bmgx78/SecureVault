using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Services
{
    /// <summary>
    /// Checks passwords against the HaveIBeenPwned database using the k-anonymity model.
    /// Only the first 5 characters of the SHA-1 hash are sent — password never leaves device.
    /// </summary>
    public class BreachCheckService
    {
        private readonly HttpClient _http = new();

        public async Task<int> CheckPasswordBreachAsync(string password)
        {
            byte[] hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(password));
            string hash = Convert.ToHexString(hashBytes).ToUpperInvariant();
            string prefix = hash[..5];
            string suffix = hash[5..];

            _http.DefaultRequestHeaders.Add("Add-Padding", "true");
            var resp = await _http.GetAsync($"https://api.pwnedpasswords.com/range/{prefix}");
            if (!resp.IsSuccessStatusCode) return 0;

            string body = await resp.Content.ReadAsStringAsync();
            foreach (var line in body.Split('\n'))
            {
                var parts = line.Trim().Split(':');
                if (parts.Length == 2 && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    return int.TryParse(parts[1], out int count) ? count : 1;
            }
            return 0;
        }

        public async Task<bool> CheckEmailBreachAsync(string email)
        {
            // Uses HIBP v3 API — requires an API key for email checks
            // Returns true if email has been in a breach
            try
            {
                var resp = await _http.GetAsync(
                    $"https://haveibeenpwned.com/api/v3/breachedaccount/{Uri.EscapeDataString(email)}");
                return resp.StatusCode != System.Net.HttpStatusCode.NotFound;
            }
            catch { return false; }
        }
    }
}
