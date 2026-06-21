using PasswordManager.ViewModels;

namespace PasswordManager.Views;

[QueryProperty(nameof(Entry), "Entry")]
public partial class EntryEditPage : ContentPage
{
    private readonly EntryEditViewModel _vm;

    public object? Entry
    {
        set
        {
            if (value is Models.VaultEntry entry)
            {
                _vm.Entry = entry;
                _vm.StartTotpTimer();
            }
        }
    }

    public EntryEditPage(EntryEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}
