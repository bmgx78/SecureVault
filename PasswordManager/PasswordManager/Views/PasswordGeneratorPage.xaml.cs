using PasswordManager.ViewModels;

namespace PasswordManager.Views;

public partial class PasswordGeneratorPage : ContentPage
{
    public PasswordGeneratorPage(PasswordGeneratorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
