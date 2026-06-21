using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Models;
using PasswordManager.Services;

namespace PasswordManager.ViewModels
{
    [QueryProperty(nameof(Entry), "Entry")]
    public partial class EntryEditViewModel : ObservableObject
    {
        private readonly VaultService _vault;

        [ObservableProperty] private VaultEntry entry = new();
        [ObservableProperty] private string plainPassword = string.Empty;
        [ObservableProperty] private bool showPassword = false;
        [ObservableProperty] private int passwordStrength = 0;
        [ObservableProperty] private string passwordStrengthLabel = "—";
        [ObservableProperty] private bool isBusy = false;
        [ObservableProperty] private bool isEdit = false;
        [ObservableProperty] private string totpCode = string.Empty;
        [ObservableProperty] private int totpSecondsLeft = 30;

        public EntryEditViewModel(VaultService vault) => _vault = vault;

        partial void OnEntryChanged(VaultEntry value)
        {
            if (!string.IsNullOrEmpty(value.EncryptedPassword))
            {
                IsEdit = true;
                PlainPassword = _vault.GetDecryptedPassword(value);
                UpdateStrength(PlainPassword);
            }
        }

        partial void OnPlainPasswordChanged(string value) => UpdateStrength(value);

        private void UpdateStrength(string pw)
        {
            PasswordStrength = EncryptionService.ScorePassword(pw);
            PasswordStrengthLabel = PasswordStrength switch
            {
                >= 80 => "Very Strong",
                >= 60 => "Strong",
                >= 40 => "Fair",
                >= 20 => "Weak",
                _ => "Very Weak"
            };
        }

        [RelayCommand]
        private void GeneratePassword()
        {
            PlainPassword = EncryptionService.GeneratePassword(20, true, true, true, false);
        }

        [RelayCommand]
        private void GeneratePassphrase()
        {
            PlainPassword = EncryptionService.GeneratePassphrase(4, "-");
        }

        [RelayCommand]
        private void ToggleShowPassword() => ShowPassword = !ShowPassword;

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Entry.Title))
            {
                await Shell.Current.DisplayAlert("Error", "Title is required.", "OK");
                return;
            }
            IsBusy = true;
            Entry.EncryptedPassword = _vault.EncryptPassword(PlainPassword);
            Entry.UpdatedAt = DateTime.UtcNow;
            if (IsEdit)
                await _vault.UpdateEntryAsync(Entry);
            else
                await _vault.AddEntryAsync(Entry);
            IsBusy = false;
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

        public void StartTotpTimer()
        {
            if (string.IsNullOrEmpty(Entry.Totp)) return;
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    TotpCode = TotpService.GenerateCode(Entry.Totp!);
                    TotpSecondsLeft = TotpService.SecondsRemaining();
                    await Task.Delay(1000);
                }
            });
        }
    }
}
