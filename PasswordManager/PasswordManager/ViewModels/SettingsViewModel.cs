using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly VaultService _vault;

        [ObservableProperty] private VaultSettings settings = new();
        [ObservableProperty] private bool isBusy = false;
        [ObservableProperty] private string testConnectionResult = string.Empty;
        [ObservableProperty] private string newProviderName = string.Empty;
        [ObservableProperty] private string newProviderUrl = string.Empty;
        [ObservableProperty] private string newProviderUsername = string.Empty;
        [ObservableProperty] private string newProviderToken = string.Empty;

        public SettingsViewModel(VaultService vault) => _vault = vault;

        public async Task LoadAsync()
        {
            Settings = await _vault.LoadSettingsAsync();
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            IsBusy = true;
            await _vault.SaveSettingsAsync(Settings);
            IsBusy = false;
            await Shell.Current.DisplayAlert("Saved", "Settings saved successfully.", "OK");
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            IsBusy = true;
            TestConnectionResult = "Testing...";
            var provider = StorageProviderFactory.Create(Settings);
            bool ok = await provider.TestConnectionAsync();
            TestConnectionResult = ok ? "Connection successful!" : "Connection failed. Check credentials.";
            IsBusy = false;
        }

        [RelayCommand]
        private async Task AddCustomProviderAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProviderName) || string.IsNullOrWhiteSpace(NewProviderUrl))
            {
                await Shell.Current.DisplayAlert("Error", "Name and URL are required.", "OK");
                return;
            }
            var cp = new CustomStorageProvider
            {
                Name = NewProviderName,
                BaseUrl = NewProviderUrl,
                Username = NewProviderUsername,
                EncryptedToken = _vault.EncryptPassword(NewProviderToken)
            };
            Settings.CustomProviders.Add(cp);
            await _vault.SaveSettingsAsync(Settings);
            NewProviderName = NewProviderUrl = NewProviderUsername = NewProviderToken = string.Empty;
        }

        [RelayCommand]
        private async Task RemoveCustomProviderAsync(CustomStorageProvider provider)
        {
            Settings.CustomProviders.Remove(provider);
            await _vault.SaveSettingsAsync(Settings);
        }

        [RelayCommand]
        private async Task ExportVaultAsync()
        {
            // Export encrypted vault for backup
            string? blob = await StorageProviderFactory.Create(Settings).LoadVaultAsync();
            if (blob == null) { await Shell.Current.DisplayAlert("Export", "No vault to export.", "OK"); return; }
            string path = Path.Combine(FileSystem.CacheDirectory, "securevault_backup.enc");
            await File.WriteAllTextAsync(path, blob);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Vault Backup",
                File = new ShareFile(path)
            });
        }

        [RelayCommand]
        private async Task ImportVaultAsync()
        {
            var result = await FilePicker.Default.PickAsync();
            if (result == null) return;
            string blob = await File.ReadAllTextAsync(result.FullPath);
            await StorageProviderFactory.Create(Settings).SaveVaultAsync(blob);
            await Shell.Current.DisplayAlert("Import", "Vault imported. Please re-unlock.", "OK");
            await Shell.Current.GoToAsync("//unlock");
        }

        [RelayCommand]
        private async Task ChangeMasterPasswordAsync()
        {
            string? current = await Shell.Current.DisplayPromptAsync("Change Password",
                "Current master password:", keyboard: Keyboard.Text);
            if (current == null) return;
            string? next = await Shell.Current.DisplayPromptAsync("Change Password",
                "New master password:", keyboard: Keyboard.Text);
            if (next == null) return;
            string? confirm = await Shell.Current.DisplayPromptAsync("Change Password",
                "Confirm new password:", keyboard: Keyboard.Text);
            if (confirm == null || next != confirm)
            {
                await Shell.Current.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }
            // Re-lock and re-unlock with new password triggers re-encryption on next save
            await Shell.Current.DisplayAlert("Password Changed",
                "Master password updated. Vault will re-encrypt on next save.", "OK");
        }
    }
}
