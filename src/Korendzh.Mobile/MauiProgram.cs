using Korendzh.Mobile.Services;
using Korendzh.Mobile.Pages;
using Microsoft.Extensions.Logging;

namespace Korendzh.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // DI: ApiClient — single instance, hands-out HttpClient per request.
        builder.Services.AddSingleton<AuthState>();
        builder.Services.AddSingleton<KorendzhApiClient>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<EntriesPage>();
        builder.Services.AddTransient<CreateEntryPage>();

        return builder.Build();
    }
}
