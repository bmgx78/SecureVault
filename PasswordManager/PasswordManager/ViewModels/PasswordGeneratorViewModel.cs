using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Services;

namespace PasswordManager.ViewModels
{
    public partial class PasswordGeneratorViewModel : ObservableObject
    {
        [ObservableProperty] private string generatedPassword = string.Empty;
        [ObservableProperty] private int length = 20;
        [ObservableProperty] private bool includeUppercase = true;
        [ObservableProperty] private bool includeDigits = true;
        [ObservableProperty] private bool includeSymbols = true;
        [ObservableProperty] private bool excludeAmbiguous = false;
        [ObservableProperty] private bool usePassphrase = false;
        [ObservableProperty] private int passphraseWords = 4;
        [ObservableProperty] private string passphraseSeparator = "-";
        [ObservableProperty] private int passwordStrength = 0;
        [ObservableProperty] private string passwordStrengthLabel = "—";
        [ObservableProperty] private string copiedMessage = string.Empty;
        [ObservableProperty] private List<string> passwordHistory = new();

        public PasswordGeneratorViewModel() => Generate();

        [RelayCommand]
        private void Generate()
        {
            GeneratedPassword = UsePassphrase
                ? EncryptionService.GeneratePassphrase(PassphraseWords, PassphraseSeparator)
                : EncryptionService.GeneratePassword(Length, IncludeUppercase, IncludeDigits, IncludeSymbols, ExcludeAmbiguous);

            PasswordStrength = EncryptionService.ScorePassword(GeneratedPassword);
            PasswordStrengthLabel = PasswordStrength switch
            {
                >= 80 => "Very Strong",
                >= 60 => "Strong",
                >= 40 => "Fair",
                >= 20 => "Weak",
                _ => "Very Weak"
            };

            if (!PasswordHistory.Contains(GeneratedPassword))
            {
                PasswordHistory = PasswordHistory.Prepend(GeneratedPassword).Take(20).ToList();
            }
        }

        [RelayCommand]
        private async Task CopyAsync()
        {
            await ClipboardService.CopyAndAutoClearAsync(GeneratedPassword, 30);
            CopiedMessage = "Copied! Clears in 30s.";
            await Task.Delay(3000);
            CopiedMessage = string.Empty;
        }

        [RelayCommand]
        private async Task CopyHistoryItemAsync(string pw)
        {
            await ClipboardService.CopyAndAutoClearAsync(pw, 30);
        }
    }
}
