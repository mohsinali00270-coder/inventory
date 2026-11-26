using ShopLedger.Services;

namespace ShopLedger.Views;

public static class PageExtensions
{
    /// <summary>Adds the English / اردو toggle button to the page's app bar.</summary>
    public static void AddLanguageToggle(this ContentPage page)
    {
        var item = new ToolbarItem { Text = Loc.ToggleLabel, Priority = 100 };
        item.Clicked += (_, _) => LanguageSwitcher.Toggle();
        page.ToolbarItems.Add(item);
    }
}
