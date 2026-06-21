using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using PasswordManager.Services;
using PasswordManager.ViewModels;
using PasswordManager.Views;
using ZXing.Net.Maui.Controls;

namespace PasswordManager;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services (singleton — one vault instance for the app lifetime)
        builder.Services.AddSingleton<VaultService>();
        builder.Services.AddSingleton<BreachCheckService>();

        // ViewModels (transient so each navigation gets a fresh instance)
        builder.Services.AddTransient<UnlockViewModel>();
        builder.Services.AddTransient<VaultViewModel>();
        builder.Services.AddTransient<EntryEditViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<PasswordGeneratorViewModel>();

        // Pages
        builder.Services.AddTransient<UnlockPage>();
        builder.Services.AddTransient<VaultPage>();
        builder.Services.AddTransient<EntryEditPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<PasswordGeneratorPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
