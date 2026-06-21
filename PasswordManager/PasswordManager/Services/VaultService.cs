using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PasswordManager.Models;

namespace PasswordManager.Services
{
    /// <summary>
    /// Central vault manager. Holds the decrypted entries in memory while unlocked.
    /// All persistence goes through the active IStorageProvider.
    /// </summary>
    public class VaultService
    {
        private byte[]? _masterKey;
        private byte[]? _masterSalt;
        private List<VaultEntry> _entries = new();
        private VaultSettings _settings = new();
        private IStorageProvider? _storageProvider;
        private DateTime? _lastUnlocked;

        public bool IsUnlocked => _masterKey != null && !IsAutoLocked();
        public IReadOnlyList<VaultEntry> Entries => _entries.AsReadOnly();
        public VaultSettings Settings => _settings;

        private static readonly string LocalVaultPath =
            Path.Combine(FileSystem.AppDataDirectory, "vault.enc");
        private static readonly string SettingsPath =
            Path.Combine(FileSystem.AppDataDirectory, "settings.json");

        public async Task<bool> UnlockAsync(string masterPassword)
        {
            try
            {
                await LoadSettingsAsync();
                _storageProvider = StorageProviderFactory.Create(_settings);

                string? vaultBlob = await _storageProvider.LoadVaultAsync();
                if (vaultBlob == null)
                {
                    // New vault
                    _masterSalt = EncryptionService.GenerateSalt();
                    _masterKey = EncryptionService.DeriveKey(masterPassword, _masterSalt);
                    _entries = new List<VaultEntry>();
                    _lastUnlocked = DateTime.UtcNow;
                    return true;
                }

                int sep = vaultBlob.IndexOf(':');
                _masterSalt = Convert.FromBase64String(vaultBlob[..sep]);
                _masterKey = EncryptionService.DeriveKey(masterPassword, _masterSalt);

                string json = EncryptionService.DecryptVault(vaultBlob, masterPassword);
                _entries = JsonSerializer.Deserialize<List<VaultEntry>>(json) ?? new();
                _lastUnlocked = DateTime.UtcNow;
                return true;
            }
            catch
            {
                _masterKey = null;
                return false;
            }
        }

        public void Lock()
        {
            _masterKey = null;
            _masterSalt = null;
            _entries = new();
            _lastUnlocked = null;
        }

        private bool IsAutoLocked()
        {
            if (!_settings.AutoLockEnabled || _lastUnlocked == null) return false;
            return (DateTime.UtcNow - _lastUnlocked.Value).TotalMinutes > _settings.AutoLockMinutes;
        }

        public void ResetAutoLockTimer() => _lastUnlocked = DateTime.UtcNow;

        public async Task SaveAsync()
        {
            if (_masterKey == null || _masterSalt == null) throw new InvalidOperationException("Vault is locked.");
            string json = JsonSerializer.Serialize(_entries);
            // Re-encrypt with current salt
            string saltB64 = Convert.ToBase64String(_masterSalt);
            string encrypted = EncryptionService.Encrypt(json, _masterKey);
            string blob = $"{saltB64}:{encrypted}";
            await (_storageProvider ?? StorageProviderFactory.Create(_settings)).SaveVaultAsync(blob);
        }

        public async Task AddEntryAsync(VaultEntry entry)
        {
            EnsureUnlocked();
            entry.PasswordStrength = EncryptionService.ScorePassword(entry.EncryptedPassword);
            _entries.Add(entry);
            await SaveAsync();
        }

        public async Task UpdateEntryAsync(VaultEntry entry)
        {
            EnsureUnlocked();
            int idx = _entries.FindIndex(e => e.Id == entry.Id);
            if (idx < 0) throw new KeyNotFoundException("Entry not found.");
            entry.UpdatedAt = DateTime.UtcNow;
            entry.PasswordStrength = EncryptionService.ScorePassword(entry.EncryptedPassword);
            _entries[idx] = entry;
            await SaveAsync();
        }

        public async Task DeleteEntryAsync(string id)
        {
            EnsureUnlocked();
            _entries.RemoveAll(e => e.Id == id);
            await SaveAsync();
        }

        public List<VaultEntry> Search(string query)
        {
            EnsureUnlocked();
            if (string.IsNullOrWhiteSpace(query)) return _entries;
            string q = query.ToLowerInvariant();
            return _entries.Where(e =>
                e.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Username.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Url.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        public List<VaultEntry> GetByCategory(string category) =>
            _entries.Where(e => e.Category == category).ToList();

        public List<VaultEntry> GetFavorites() =>
            _entries.Where(e => e.IsFavorite).ToList();

        public List<VaultEntry> GetWeakPasswords() =>
            _entries.Where(e => e.PasswordStrength < 40).ToList();

        public List<VaultEntry> GetBreachedEntries() =>
            _entries.Where(e => e.HasBreachAlert).ToList();

        public async Task<VaultSettings> LoadSettingsAsync()
        {
            if (File.Exists(SettingsPath))
            {
                string json = await File.ReadAllTextAsync(SettingsPath);
                _settings = JsonSerializer.Deserialize<VaultSettings>(json) ?? new();
            }
            return _settings;
        }

        public async Task SaveSettingsAsync(VaultSettings settings)
        {
            _settings = settings;
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingsPath, json);
            _storageProvider = StorageProviderFactory.Create(_settings);
        }

        public string GetDecryptedPassword(VaultEntry entry)
        {
            EnsureUnlocked();
            if (_masterKey == null) throw new InvalidOperationException("Vault locked.");
            return EncryptionService.Decrypt(entry.EncryptedPassword, _masterKey);
        }

        public string EncryptPassword(string plainPassword)
        {
            EnsureUnlocked();
            if (_masterKey == null) throw new InvalidOperationException("Vault locked.");
            return EncryptionService.Encrypt(plainPassword, _masterKey);
        }

        private void EnsureUnlocked()
        {
            if (!IsUnlocked) throw new InvalidOperationException("Vault is locked.");
        }
    }
}
