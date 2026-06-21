using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PasswordManager.Services
{
    /// <summary>
    /// Stores the encrypted vault blob as a file in the user's Google Drive app data folder.
    /// Requires OAuth2 access token obtained via Google Sign-In / web OAuth flow.
    /// </summary>
    public class GoogleDriveStorageProvider : IStorageProvider
    {
        private const string FileName = "securevault.enc";
        private const string AppDataFolder = "appDataFolder";
        private readonly string _accessToken;
        private readonly HttpClient _http = new();

        public GoogleDriveStorageProvider(string accessToken)
        {
            _accessToken = accessToken;
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        private async Task<string?> FindFileIdAsync()
        {
            var resp = await _http.GetAsync(
                $"https://www.googleapis.com/drive/v3/files?spaces={AppDataFolder}&fields=files(id,name)&q=name='{FileName}'");
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var files = json.GetProperty("files");
            if (files.GetArrayLength() == 0) return null;
            return files[0].GetProperty("id").GetString();
        }

        public async Task<string?> LoadVaultAsync()
        {
            string? fileId = await FindFileIdAsync();
            if (fileId == null) return null;
            var resp = await _http.GetAsync(
                $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        public async Task SaveVaultAsync(string encryptedBlob)
        {
            string? fileId = await FindFileIdAsync();
            var metadata = JsonSerializer.Serialize(new { name = FileName, parents = new[] { AppDataFolder } });
            var content = new MultipartContent("related");
            content.Add(new StringContent(metadata, Encoding.UTF8, "application/json"));
            content.Add(new StringContent(encryptedBlob, Encoding.UTF8, "text/plain"));

            HttpResponseMessage resp;
            if (fileId == null)
            {
                resp = await _http.PostAsync(
                    "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart",
                    content);
            }
            else
            {
                resp = await _http.PatchAsync(
                    $"https://www.googleapis.com/upload/drive/v3/files/{fileId}?uploadType=multipart",
                    content);
            }
            resp.EnsureSuccessStatusCode();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var resp = await _http.GetAsync("https://www.googleapis.com/drive/v3/about?fields=user");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
