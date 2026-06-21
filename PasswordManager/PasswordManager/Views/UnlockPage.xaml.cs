using PasswordManager.ViewModels;

namespace PasswordManager.Views;

public partial class UnlockPage : ContentPage
{
    public UnlockPage(UnlockViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
