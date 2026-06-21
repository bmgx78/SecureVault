namespace PasswordManager.Models
{
    public class VaultSettings
    {
        public StorageProvider StorageProvider { get; set; } = StorageProvider.Local;
        public string? FirebaseProjectId { get; set; }
        public string? FirebaseApiKey { get; set; }
        public string? FirebaseEmail { get; set; }
        public string? GoogleDriveClientId { get; set; }
        public string? GoogleDriveClientSecret { get; set; }
        public string? GitHubToken { get; set; }
        public string? GitHubRepo { get; set; }
        public string? GitHubOwner { get; set; }
        public List<CustomStorageProvider> CustomProviders { get; set; } = new();
        public bool AutoLockEnabled { get; set; } = true;
        public int AutoLockMinutes { get; set; } = 5;
        public bool BiometricEnabled { get; set; } = false;
        public bool ClipboardClearEnabled { get; set; } = true;
        public int ClipboardClearSeconds { get; set; } = 30;
        public bool BreachCheckEnabled { get; set; } = true;
        public bool AutoFillEnabled { get; set; } = true;
        public bool ShowPasswordStrength { get; set; } = true;
        public string Theme { get; set; } = "Dark";
        public bool SyncOnOpen { get; set; } = true;
    }

    public enum StorageProvider
    {
        Local,
        Firebase,
        GoogleDrive,
        GitHub,
        Custom
    }

    public class CustomStorageProvider
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedToken { get; set; } = string.Empty;
        public string ProviderType { get; set; } = "REST";
    }
}
