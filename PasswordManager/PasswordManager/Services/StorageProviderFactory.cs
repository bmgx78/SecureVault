using PasswordManager.Models;

namespace PasswordManager.Services
{
    public static class StorageProviderFactory
    {
        public static IStorageProvider Create(VaultSettings settings)
        {
            return settings.StorageProvider switch
            {
                StorageProvider.Firebase when
                    !string.IsNullOrEmpty(settings.FirebaseProjectId) &&
                    !string.IsNullOrEmpty(settings.FirebaseApiKey) =>
                    new FirebaseStorageProvider(
                        settings.FirebaseProjectId!,
                        settings.FirebaseApiKey!,
                        settings.FirebaseEmail ?? string.Empty),

                StorageProvider.GoogleDrive when
                    !string.IsNullOrEmpty(settings.GoogleDriveClientId) =>
                    new GoogleDriveStorageProvider(settings.GoogleDriveClientId!),

                StorageProvider.GitHub when
                    !string.IsNullOrEmpty(settings.GitHubToken) &&
                    !string.IsNullOrEmpty(settings.GitHubOwner) &&
                    !string.IsNullOrEmpty(settings.GitHubRepo) =>
                    new GitHubStorageProvider(
                        settings.GitHubToken!,
                        settings.GitHubOwner!,
                        settings.GitHubRepo!),

                _ => new LocalStorageProvider()
            };
        }
    }
}
