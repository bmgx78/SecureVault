using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Services;

namespace PasswordManager.ViewModels
{
    public partial class UnlockViewModel : ObservableObject
    {
        private readonly VaultService _vault;

        [ObservableProperty] private string masterPassword = string.Empty;
        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isBusy = false;
        [ObservableProperty] private bool isNewVault = false;
        [ObservableProperty] private string confirmPassword = string.Empty;

        public UnlockViewModel(VaultService vault) => _vault = vault;

        [RelayCommand]
        private async Task UnlockAsync()
        {
            if (string.IsNullOrWhiteSpace(MasterPassword))
            {
                ErrorMessage = "Please enter your master password.";
                return;
            }
            if (IsNewVault && MasterPassword != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }
            IsBusy = true;
            ErrorMessage = string.Empty;
            bool ok = await _vault.UnlockAsync(MasterPassword);
            IsBusy = false;
            if (ok)
                await Shell.Current.GoToAsync("//vault");
            else
                ErrorMessage = "Incorrect master password. Please try again.";
        }

        [RelayCommand]
        private async Task UseBiometricAsync()
        {
            var result = await Plugin.Fingerprint.CrossFingerprint.Current
                .AuthenticateAsync(new Plugin.Fingerprint.Abstractions.AuthenticationRequestConfiguration(
                    "SecureVault", "Authenticate to unlock your vault"));
            if (result.Authenticated)
            {
                string? saved = await SecureStorage.Default.GetAsync("master_password_hash");
                if (saved != null)
                    await UnlockWithStoredAsync(saved);
            }
        }

        private async Task UnlockWithStoredAsync(string storedPassword)
        {
            IsBusy = true;
            bool ok = await _vault.UnlockAsync(storedPassword);
            IsBusy = false;
            if (ok)
                await Shell.Current.GoToAsync("//vault");
            else
                ErrorMessage = "Biometric unlock failed. Enter master password.";
        }
    }
}
