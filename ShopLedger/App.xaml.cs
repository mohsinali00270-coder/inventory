using ShopLedger.Data;
using ShopLedger.Services;

namespace ShopLedger;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private static bool? _urduFontFound;

    public App(IServiceProvider services, AppDatabase db)
    {
        InitializeComponent();
        _services = services;
        _ = db.InitAsync();   // create tables on startup
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        ThemeService.Apply();
        var window = new Window(BuildShell()) { Title = Loc.T("X_shopledger") };
        ApplyTitleBar(window);
        window.Activated += (_, _) => LanguageSwitcher.ApplyNativeDirection();
        // Desktop: switch between sidebar / collapsible menu as the window is resized
        window.SizeChanged += (_, _) => ApplyNavigationMode(window);
        return window;
    }

    /// <summary>Desktop: navy title bar that continues the app header, so the white menu (hamburger) button is visible.</summary>
    private static void ApplyTitleBar(Window window)
    {
#if WINDOWS
        window.TitleBar = new TitleBar
        {
            Title = Loc.T("X_shopledger"),
            BackgroundColor = Color.FromArgb("#14213D"),
            ForegroundColor = Colors.White,
            HeightRequest = 40,
            FlowDirection = Loc.FlowDirection,
        };
        if (window.Handler is null)
            window.HandlerChanged += (_, _) => StyleCaptionButtons(window);
        else
            StyleCaptionButtons(window);
#endif
    }

#if WINDOWS
    /// <summary>Minimise / maximise / close glyphs in white so they stay visible on the navy title bar.</summary>
    private static void StyleCaptionButtons(Window window)
    {
        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window w) return;
        var tb = w.AppWindow.TitleBar;
        tb.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        tb.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        tb.ButtonForegroundColor = Microsoft.UI.Colors.White;
        tb.ButtonInactiveForegroundColor = global::Windows.UI.Color.FromArgb(255, 0xB6, 0xC2, 0xD9);
        tb.ButtonHoverBackgroundColor = global::Windows.UI.Color.FromArgb(255, 0x22, 0x34, 0x5C);
        tb.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
        tb.ButtonPressedBackgroundColor = global::Windows.UI.Color.FromArgb(255, 0x2C, 0x44, 0x75);
        tb.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
    }
#endif

    private static void ApplyNavigationMode(Window window)
    {
        if (window.Page is AppShell shell && window.Width > 0 && !double.IsNaN(window.Width))
            shell.ApplyNavigationMode(window.Width);
    }

    private AppShell BuildShell()
    {
        ApplyFontStyles();
        return _services.GetRequiredService<AppShell>();   // transient: every call builds a fresh, correctly translated Shell
    }

    /// <summary>Called after the language changed: rebuild the whole UI in the new language and return to the same tab.</summary>
    public void RebuildForLanguage()
    {
        var route = Shell.Current?.CurrentItem?.CurrentItem?.CurrentItem?.Route;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var window = Windows.FirstOrDefault();
            if (window is null) return;
            window.Page = BuildShell();
            window.Title = Loc.T("X_shopledger");
            ApplyTitleBar(window);
            ApplyNavigationMode(window);
            LanguageSwitcher.ApplyNativeDirection();
            if (!string.IsNullOrEmpty(route))
            {
                try { await Task.Delay(120); if (Shell.Current is { } shell) await shell.GoToAsync($"//{route}"); }
                catch { /* stay on the first tab */ }
            }
        });
    }

    /// <summary>Urdu: use the Nastaliq font for all text controls (only when the font file is in the app package).
    /// Every text style in Resources/Styles/Styles.xaml reads its font from the dynamic resource "AppFont".</summary>
    private void ApplyFontStyles()
    {
        if (Loc.IsUrdu && UrduFontAvailable()) Resources[AppFontKey] = "UrduFont";
        else Resources.Remove(AppFontKey);   // unset = platform default font
    }

    private const string AppFontKey = "AppFont";

    private static bool UrduFontAvailable() => _urduFontFound ??= CheckFont();

    private static bool CheckFont()
    {
        try { return Task.Run(() => FileSystem.AppPackageFileExistsAsync(Loc.UrduFontFile)).GetAwaiter().GetResult(); }
        catch { return false; }
    }
}
