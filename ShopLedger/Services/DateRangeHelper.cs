namespace ShopLedger.Services;

public static class DateRangeHelper
{
    private static readonly string[] Keys = { "Today", "Last7", "ThisMonth", "ThisYear", "All", "Custom" };

    /// <summary>Localized names, in the same order as Keys (so the logic never depends on the display language).</summary>
    public static List<string> Names => Keys.Select(Label).ToList();

    public static string Label(string key) => Loc.T("Range_" + key);
    public static bool IsCustom(string? name) => name == Label("Custom");

    public static (DateTime? From, DateTime? To) Resolve(string name, DateTime customFrom, DateTime customTo)
    {
        var idx = Names.IndexOf(name);
        var key = idx >= 0 ? Keys[idx] : "All";
        var today = DateTime.Today;
        return key switch
        {
            "Today" => (today, today),
            "Last7" => (today.AddDays(-6), today),
            "ThisMonth" => (new DateTime(today.Year, today.Month, 1), today),
            "ThisYear" => (new DateTime(today.Year, 1, 1), today),
            "Custom" => (customFrom, customTo),
            _ => (null, null)
        };
    }
}
