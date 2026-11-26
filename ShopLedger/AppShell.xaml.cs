using ShopLedger.Services;
using ShopLedger.Views;

namespace ShopLedger;

public partial class AppShell : Shell
{
    private static bool _routesRegistered;

    public AppShell()
    {
        InitializeComponent();
        FlowDirection = Loc.FlowDirection;     // RightToLeft for Urdu; pushed pages inherit it
        ApplyNavigationMode(DeviceDisplay.Current.MainDisplayInfo.Width / Math.Max(1, DeviceDisplay.Current.MainDisplayInfo.Density));
        SizeChanged += (_, _) => { if (Width > 0) ApplyNavigationMode(Width); };

        if (_routesRegistered) return;         // the Shell is rebuilt on every language switch
        _routesRegistered = true;
        Routing.RegisterRoute(nameof(ProductEditPage), typeof(ProductEditPage));
        Routing.RegisterRoute(nameof(StockAdjustPage), typeof(StockAdjustPage));
        Routing.RegisterRoute(nameof(CustomerEditPage), typeof(CustomerEditPage));
        Routing.RegisterRoute(nameof(CustomerDetailPage), typeof(CustomerDetailPage));
        Routing.RegisterRoute(nameof(NewSalePage), typeof(NewSalePage));
        Routing.RegisterRoute(nameof(LedgerEntryPage), typeof(LedgerEntryPage));
        Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
        Routing.RegisterRoute(nameof(StatementPage), typeof(StatementPage));
        Routing.RegisterRoute(nameof(VoiceEntryPage), typeof(VoiceEntryPage));
    }

    /// <summary>Wide window (tablet landscape, desktop): permanent sidebar, no bottom tabs.
    /// Narrow: phones/tablets use the bottom tab bar; a narrow desktop window gets a collapsible sidebar.</summary>
    public void ApplyNavigationMode(double width)
    {
        const double SidebarMinWidth = 820;
        var wide = width >= SidebarMinWidth;
        var desktop = DeviceInfo.Current.Idiom == DeviceIdiom.Desktop;
        var behavior = wide ? FlyoutBehavior.Locked : desktop ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;
        if (FlyoutBehavior != behavior) FlyoutBehavior = behavior;
        var showTabs = !wide && !desktop;
        if (Shell.GetTabBarIsVisible(this) != showTabs) Shell.SetTabBarIsVisible(this, showTabs);
    }
}
