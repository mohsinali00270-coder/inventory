namespace ShopLedger.Services;

/// <summary>Appearance: 0 = follow the phone, 1 = light, 2 = dark. Saved in Preferences.</summary>
public static class ThemeService
{
    private const string Key = "Theme";

    public static int Index
    {
        get => Preferences.Get(Key, 0);
        set { Preferences.Set(Key, value); Apply(); }
    }

    public static void Apply()
    {
        if (Application.Current is not { } app) return;
        app.UserAppTheme = Index switch { 1 => AppTheme.Light, 2 => AppTheme.Dark, _ => AppTheme.Unspecified };
    }
}
