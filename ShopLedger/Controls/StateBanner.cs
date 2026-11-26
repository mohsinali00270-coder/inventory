using System.Windows.Input;
using ShopLedger.Services;

namespace ShopLedger.Controls;

/// <summary>Loading / error strip for list pages. Bind IsBusy and Error (message); shows a slim spinner row while
/// loading and a red banner with a "Try again" button when the last load failed.</summary>
public class StateBanner : ContentView
{
    public static readonly BindableProperty IsBusyProperty =
        BindableProperty.Create(nameof(IsBusy), typeof(bool), typeof(StateBanner), false, propertyChanged: (b, _, _) => ((StateBanner)b).Update());
    public static readonly BindableProperty ErrorProperty =
        BindableProperty.Create(nameof(Error), typeof(string), typeof(StateBanner), null, propertyChanged: (b, _, _) => ((StateBanner)b).Update());
    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(StateBanner), null, propertyChanged: (b, _, n) => ((StateBanner)b)._retry.Command = (ICommand?)n);

    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    private readonly HorizontalStackLayout _loading;
    private readonly Border _error;
    private readonly Label _errorText = new() { FontSize = 13, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Button _retry = new() { HeightRequest = 34, MinimumHeightRequest = 34, Padding = new Thickness(12, 0), FontSize = 13, CornerRadius = 10, VerticalOptions = LayoutOptions.Center };

    public StateBanner()
    {
        var spinner = new ActivityIndicator { IsRunning = true, WidthRequest = 18, HeightRequest = 18, VerticalOptions = LayoutOptions.Center };
        var loadingText = new Label { Text = Loc.T("UI_Loading"), FontSize = 12.5, VerticalTextAlignment = TextAlignment.Center };
        loadingText.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#64748B"), Color.FromArgb("#8A9AB5"));
        loadingText.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        _loading = new HorizontalStackLayout { Spacing = 8, Padding = new Thickness(4, 2), IsVisible = false, Children = { spinner, loadingText } };

        var icon = new Label { FontFamily = Icons.FontFamily, Text = Icons.AlertCircle, FontSize = 18, VerticalTextAlignment = TextAlignment.Center };
        icon.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#DC2626"), Color.FromArgb("#F87171"));
        _errorText.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#991B1B"), Color.FromArgb("#FECACA"));
        _errorText.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        _retry.Text = Loc.T("UI_Retry");
        _retry.BackgroundColor = Color.FromArgb("#DC2626");
        var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 10 };
        row.Add(icon, 0); row.Add(_errorText, 1); row.Add(_retry, 2);
        _error = new Border
        {
            StrokeThickness = 0, Padding = new Thickness(12, 8), IsVisible = false,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Content = row,
        };
        _error.SetAppThemeColor(Border.BackgroundColorProperty, Color.FromArgb("#FEF2F2"), Color.FromArgb("#3B1518"));

        Content = new VerticalStackLayout { Spacing = 6, Children = { _loading, _error } };
        Update();
    }

    private void Update()
    {
        var hasError = !string.IsNullOrWhiteSpace(Error);
        _errorText.Text = hasError ? $"{Loc.T("UI_LoadError")}: {Error}" : "";
        _error.IsVisible = hasError && !IsBusy;
        _loading.IsVisible = IsBusy;
        IsVisible = IsBusy || hasError;
    }
}
