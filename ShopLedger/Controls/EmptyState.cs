using System.Windows.Input;

namespace ShopLedger.Controls;

/// <summary>Friendly placeholder for empty lists: large tinted icon, title, message and an optional action button.
/// Use as CollectionView.EmptyView or BindableLayout.EmptyView.</summary>
public class EmptyState : VerticalStackLayout
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(EmptyState), Icons.Package, propertyChanged: (b, _, n) => ((EmptyState)b)._icon.Text = (string?)n);
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyState), "", propertyChanged: (b, _, n) => ((EmptyState)b).SetText(((EmptyState)b)._title, n));
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(EmptyState), "", propertyChanged: (b, _, n) => ((EmptyState)b).SetText(((EmptyState)b)._message, n));
    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyState), null, propertyChanged: (b, _, _) => ((EmptyState)b).UpdateAction());
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EmptyState), null, propertyChanged: (b, _, _) => ((EmptyState)b).UpdateAction());
    public static readonly BindableProperty CompactProperty =
        BindableProperty.Create(nameof(Compact), typeof(bool), typeof(EmptyState), false, propertyChanged: (b, _, _) => ((EmptyState)b).UpdateSize());

    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Message { get => (string?)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    /// <summary>Smaller version for use inside a card.</summary>
    public bool Compact { get => (bool)GetValue(CompactProperty); set => SetValue(CompactProperty, value); }

    private readonly Label _icon = new() { FontFamily = Icons.FontFamily, FontSize = 30, Text = Icons.Package, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
    private readonly Border _circle = new() { WidthRequest = 72, HeightRequest = 72, StrokeThickness = 0, HorizontalOptions = LayoutOptions.Center, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 36 } };
    private readonly Label _title = new() { FontSize = 17, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
    private readonly Label _message = new() { FontSize = 13.5, HorizontalTextAlignment = TextAlignment.Center, IsVisible = false, MaximumWidthRequest = 360 };
    private readonly Button _action = new() { IsVisible = false, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 6, 0, 0) };

    public EmptyState()
    {
        Spacing = 8;
        Padding = new Thickness(24, 40);
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        _circle.Content = _icon;
        _circle.SetAppThemeColor(Border.BackgroundColorProperty, Color.FromArgb("#E0EAFF"), Color.FromArgb("#172C57"));
        _icon.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#2563EB"), Color.FromArgb("#60A5FA"));
        _title.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#0F172A"), Color.FromArgb("#E2E8F0"));
        _message.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#64748B"), Color.FromArgb("#8A9AB5"));
        _title.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        _message.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        Children.Add(_circle);
        Children.Add(_title);
        Children.Add(_message);
        Children.Add(_action);
    }

    private void SetText(Label label, object? value)
    {
        label.Text = value as string;
        label.IsVisible = !string.IsNullOrEmpty(label.Text);
    }

    private void UpdateAction()
    {
        _action.Text = ActionText;
        _action.Command = Command;
        _action.IsVisible = !string.IsNullOrEmpty(ActionText) && Command is not null;
    }

    private void UpdateSize()
    {
        Padding = Compact ? new Thickness(8, 12) : new Thickness(24, 40);
        _circle.WidthRequest = _circle.HeightRequest = Compact ? 48 : 72;
        _circle.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = Compact ? 24 : 36 };
        _icon.FontSize = Compact ? 20 : 30;
        _title.FontSize = Compact ? 14 : 17;
    }
}
