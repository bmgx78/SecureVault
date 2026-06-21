using PasswordManager.Views;

namespace PasswordManager;

public partial class App : Application
{
    public App(UnlockPage unlockPage)
    {
        InitializeComponent();
        MainPage = new AppShell();
        // Start at the unlock screen
        Shell.Current.GoToAsync("//unlock");
    }
}
