namespace ShopLedger.Services;

/// <summary>Switches language at runtime: applies culture, then rebuilds the Shell (no app restart).</summary>
public static class LanguageSwitcher
{
    public static void Toggle() => Set(Loc.IsUrdu ? "en" : "ur");

    public static void Set(string language)
    {
        if (Loc.Language == language) return;
        Loc.Apply(language);
        (Application.Current as App)?.RebuildForLanguage();
    }

    /// <summary>Flips the native Android window (toolbar, tab bar) to right-to-left when Urdu is active.</summary>
    public static void ApplyNativeDirection()
    {
#if ANDROID
        try
        {
            var view = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView;
            if (view is not null)
                view.LayoutDirection = Loc.IsUrdu ? Android.Views.LayoutDirection.Rtl : Android.Views.LayoutDirection.Ltr;
        }
        catch { /* cosmetic only */ }
#endif
    }
}
