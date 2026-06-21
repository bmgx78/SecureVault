using PasswordManager.Views;

namespace PasswordManager;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("unlock", typeof(UnlockPage));
        Routing.RegisterRoute("entryEdit", typeof(EntryEditPage));
        Routing.RegisterRoute("entryDetail", typeof(EntryEditPage));
    }
}
