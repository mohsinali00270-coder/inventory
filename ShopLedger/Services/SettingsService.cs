namespace ShopLedger.Services;

public interface ISettingsService
{
    string Language { get; set; }          // "en" or "ur"
    bool IsUrdu { get; }
    string CurrencySymbol { get; }
    bool DemoDataSeeded { get; set; }
}

public class SettingsService : ISettingsService
{
    public string Language
    {
        get => Loc.Language;
        set => Loc.Apply(value);
    }
    public bool IsUrdu => Loc.IsUrdu;
    public string CurrencySymbol => Loc.IsUrdu ? "روپے" : "Rs";
    public bool DemoDataSeeded
    {
        get => Preferences.Get(nameof(DemoDataSeeded), false);
        set => Preferences.Set(nameof(DemoDataSeeded), value);
    }
}
