using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PasswordManager.Services
{
    /// <summary>
    /// Stores the encrypted vault blob as a file in a private GitHub repository.
    /// Requires a Personal Access Token with 'repo' scope.
    /// </summary>
    public class GitHubStorageProvider : IStorageProvider
    {
        private const string FileName = "securevault.enc";
        private readonly string _owner;
        private readonly string _repo;
        private readonly HttpClient _http = new();

        public GitHubStorageProvider(string token, string owner, string repo)
        {
            _owner = owner;
            _repo = repo;
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            _http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("SecureVault", "1.0"));
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        private string FileUrl =>
            $"https://api.github.com/repos/{_owner}/{_repo}/contents/{FileName}";

        public async Task<string?> LoadVaultAsync()
        {
            var resp = await _http.GetAsync(FileUrl);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            string base64 = json.GetProperty("content").GetString()!.Replace("\n", "");
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }

        public async Task SaveVaultAsync(string encryptedBlob)
        {
            string base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptedBlob));
            string? sha = await GetFileShaAsync();

            object body = sha == null
                ? new { message = "Update vault", content = base64Content }
                : new { message = "Update vault", content = base64Content, sha };

            var content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync(FileUrl, content);
            resp.EnsureSuccessStatusCode();
        }

        private async Task<string?> GetFileShaAsync()
        {
            var resp = await _http.GetAsync(FileUrl);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("sha").GetString();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"https://api.github.com/repos/{_owner}/{_repo}");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
