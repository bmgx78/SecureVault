namespace PasswordManager.Services
{
    public interface IStorageProvider
    {
        Task<string?> LoadVaultAsync();
        Task SaveVaultAsync(string encryptedBlob);
        Task<bool> TestConnectionAsync();
    }
}
