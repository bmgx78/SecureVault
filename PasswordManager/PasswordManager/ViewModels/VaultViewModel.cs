using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordManager.Models;
using PasswordManager.Services;
using System.Collections.ObjectModel;

namespace PasswordManager.ViewModels
{
    public partial class VaultViewModel : ObservableObject
    {
        private readonly VaultService _vault;
        private readonly BreachCheckService _breach;

        [ObservableProperty] private ObservableCollection<VaultEntry> entries = new();
        [ObservableProperty] private string searchQuery = string.Empty;
        [ObservableProperty] private string selectedCategory = "All";
        [ObservableProperty] private bool isBusy = false;
        [ObservableProperty] private bool showFavoritesOnly = false;
        [ObservableProperty] private int totalEntries = 0;
        [ObservableProperty] private int weakPasswordCount = 0;
        [ObservableProperty] private int breachCount = 0;

        public VaultViewModel(VaultService vault, BreachCheckService breach)
        {
            _vault = vault;
            _breach = breach;
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            Refresh();
            UpdateStats();
            IsBusy = false;
        }

        private void Refresh()
        {
            var results = string.IsNullOrWhiteSpace(SearchQuery)
                ? _vault.Entries.ToList()
                : _vault.Search(SearchQuery);

            if (SelectedCategory != "All")
                results = results.Where(e => e.Category == SelectedCategory).ToList();

            if (ShowFavoritesOnly)
                results = results.Where(e => e.IsFavorite).ToList();

            Entries = new ObservableCollection<VaultEntry>(
                results.OrderByDescending(e => e.IsFavorite)
                       .ThenByDescending(e => e.UpdatedAt));
        }

        partial void OnSearchQueryChanged(string value) => Refresh();
        partial void OnSelectedCategoryChanged(string value) => Refresh();
        partial void OnShowFavoritesOnlyChanged(bool value) => Refresh();

        private void UpdateStats()
        {
            TotalEntries = _vault.Entries.Count;
            WeakPasswordCount = _vault.GetWeakPasswords().Count;
            BreachCount = _vault.GetBreachedEntries().Count;
        }

        [RelayCommand]
        private async Task AddEntryAsync()
        {
            await Shell.Current.GoToAsync("entryEdit");
        }

        [RelayCommand]
        private async Task OpenEntryAsync(VaultEntry entry)
        {
            _vault.ResetAutoLockTimer();
            await Shell.Current.GoToAsync("entryDetail",
                new Dictionary<string, object> { ["Entry"] = entry });
        }

        [RelayCommand]
        private async Task CopyPasswordAsync(VaultEntry entry)
        {
            _vault.ResetAutoLockTimer();
            string pw = _vault.GetDecryptedPassword(entry);
            await ClipboardService.CopyAndAutoClearAsync(pw, 30);
            entry.LastUsed = DateTime.UtcNow;
            await _vault.UpdateEntryAsync(entry);
            await Shell.Current.DisplayAlert("Copied", "Password copied. Clipboard clears in 30 s.", "OK");
        }

        [RelayCommand]
        private async Task CopyUsernameAsync(VaultEntry entry)
        {
            await Clipboard.Default.SetTextAsync(entry.Username);
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync(VaultEntry entry)
        {
            entry.IsFavorite = !entry.IsFavorite;
            await _vault.UpdateEntryAsync(entry);
            Refresh();
        }

        [RelayCommand]
        private async Task DeleteEntryAsync(VaultEntry entry)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete", $"Delete '{entry.Title}'?", "Delete", "Cancel");
            if (!confirm) return;
            await _vault.DeleteEntryAsync(entry.Id);
            Refresh();
            UpdateStats();
        }

        [RelayCommand]
        private async Task RunBreachCheckAsync()
        {
            IsBusy = true;
            foreach (var entry in _vault.Entries)
            {
                string pw = _vault.GetDecryptedPassword(entry);
                int count = await _breach.CheckPasswordBreachAsync(pw);
                entry.HasBreachAlert = count > 0;
                await _vault.UpdateEntryAsync(entry);
            }
            Refresh();
            UpdateStats();
            IsBusy = false;
            await Shell.Current.DisplayAlert("Breach Check",
                $"Found {BreachCount} compromised password(s).", "OK");
        }

        [RelayCommand]
        private void LockVault()
        {
            _vault.Lock();
            Shell.Current.GoToAsync("//unlock");
        }
    }
}
