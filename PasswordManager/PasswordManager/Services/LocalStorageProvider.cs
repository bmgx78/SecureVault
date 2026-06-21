using System.IO;
using System.Threading.Tasks;

namespace PasswordManager.Services
{
    public class LocalStorageProvider : IStorageProvider
    {
        private static readonly string VaultPath =
            Path.Combine(FileSystem.AppDataDirectory, "vault.enc");

        public async Task<string?> LoadVaultAsync()
        {
            if (!File.Exists(VaultPath)) return null;
            return await File.ReadAllTextAsync(VaultPath);
        }

        public async Task SaveVaultAsync(string encryptedBlob)
        {
            await File.WriteAllTextAsync(VaultPath, encryptedBlob);
        }

        public Task<bool> TestConnectionAsync() => Task.FromResult(true);
    }
}
