using System.Globalization;
using System.Resources;

namespace ShopLedger.Services;

/// <summary>Single source of truth for the app language, culture and translated text (.resx).</summary>
public static class Loc
{
    public const string UrduFontFile = "NotoNastaliqUrdu-Regular.ttf";
    public const string UrduFontFamily = "Noto Nastaliq Urdu";   // internal family name, used by the PDF engine
    private const string PrefKey = "Language";

    private static readonly ResourceManager Rm =
        new("ShopLedger.Resources.Strings.AppResources", typeof(Loc).Assembly);

    public static string Language { get; private set; } = "en";
    public static bool IsUrdu => Language == "ur";
    public static CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;
    public static FlowDirection FlowDirection => IsUrdu ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    /// <summary>Label of the app-bar toggle: shows the language you will switch TO.</summary>
    public static string ToggleLabel => IsUrdu ? "English" : "اردو";

    /// <summary>Call once, very early (before any XAML loads).</summary>
    public static void Init() => Apply(Preferences.Get(PrefKey, "en"), persist: false);

    public static void Apply(string language, bool persist = true)
    {
        Language = language == "ur" ? "ur" : "en";
        Culture = MakeCulture(Language);
        CultureInfo.DefaultThreadCurrentCulture = Culture;      // background threads too
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        if (persist) Preferences.Set(PrefKey, Language);
    }

    /// <summary>Translated text for a key (falls back to English, then to the key itself).</summary>
    public static string T(string key)
    {
        try { return Rm.GetString(key, Culture) ?? key; }
        catch (MissingManifestResourceException) { return key; }
    }

    /// <summary>Translated template filled with arguments, formatted in the current culture.</summary>
    public static string F(string key, params object?[] args) => string.Format(Culture, T(key), args);

    /// <summary>"Rs 2,500" or "2,500 روپے".</summary>
    public static string Money(decimal value)
    {
        var n = value.ToString("N0", Culture);
        return IsUrdu ? $"{n} روپے" : $"Rs {n}";
    }

    private static CultureInfo MakeCulture(string lang)
    {
        foreach (var name in lang == "ur" ? new[] { "ur-PK", "ur" } : new[] { "en-US", "en" })
        {
            try { return new CultureInfo(name); }
            catch (CultureNotFoundException) { }
        }
        return CultureInfo.InvariantCulture;
    }
}
