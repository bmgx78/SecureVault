using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PasswordManager.Models;

namespace PasswordManager.Services
{
    /// <summary>
    /// Stores the encrypted vault blob in Firebase Realtime Database.
    /// Only the encrypted blob travels to Firebase — plaintext never leaves the device.
    /// </summary>
    public class FirebaseStorageProvider : IStorageProvider
    {
        private readonly string _projectId;
        private readonly string _apiKey;
        private readonly string _userEmail;
        private string? _idToken;
        private string? _uid;
        private readonly HttpClient _http = new();

        public FirebaseStorageProvider(string projectId, string apiKey, string userEmail)
        {
            _projectId = projectId;
            _apiKey = apiKey;
            _userEmail = userEmail;
        }

        private async Task EnsureAuthenticatedAsync(string password)
        {
            if (_idToken != null) return;
            var body = new { email = _userEmail, password, returnSecureToken = true };
            var resp = await _http.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
                body);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            _idToken = json.GetProperty("idToken").GetString();
            _uid = json.GetProperty("localId").GetString();
        }

        private string VaultUrl =>
            $"https://{_projectId}-default-rtdb.firebaseio.com/vaults/{_uid}/vault.json?auth={_idToken}";

        public async Task<string?> LoadVaultAsync()
        {
            if (_idToken == null) throw new InvalidOperationException("Not authenticated. Call EnsureAuthenticatedAsync first.");
            var resp = await _http.GetAsync(VaultUrl);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (json.ValueKind == JsonValueKind.Null) return null;
            return json.GetString();
        }

        public async Task SaveVaultAsync(string encryptedBlob)
        {
            if (_idToken == null) throw new InvalidOperationException("Not authenticated.");
            var content = new StringContent(
                JsonSerializer.Serialize(encryptedBlob),
                Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync(VaultUrl, content);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var resp = await _http.GetAsync(
                    $"https://{_projectId}-default-rtdb.firebaseio.com/.json?auth={_idToken}&shallow=true");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
