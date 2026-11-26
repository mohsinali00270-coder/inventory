namespace ShopLedger.Controls;

/// <summary>Small rounded status pill: soft tinted background + strong coloured text, optional leading icon.
/// XAML: &lt;c:StatusBadge Text="{loc:Translate X_low_stock}" Tone="Warning" Icon="{x:Static c:Icons.Alert}" /&gt;</summary>
public class StatusBadge : Border
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(StatusBadge), "", propertyChanged: (b, _, n) => ((StatusBadge)b)._text.Text = (string?)n);
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(StatusBadge), null, propertyChanged: (b, _, _) => ((StatusBadge)b).UpdateIcon());
    public static readonly BindableProperty ToneProperty =
        BindableProperty.Create(nameof(Tone), typeof(Tone), typeof(StatusBadge), Tone.Info, propertyChanged: (b, _, _) => ((StatusBadge)b).ApplyTone());

    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public Tone Tone { get => (Tone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    private readonly Label _icon = new() { FontFamily = Icons.FontFamily, FontSize = 11, VerticalTextAlignment = TextAlignment.Center, IsVisible = false };
    private readonly Label _text = new() { FontSize = 11.5, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center };

    public StatusBadge()
    {
        StrokeThickness = 0;
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };
        Padding = new Thickness(8, 3);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        _text.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        Content = new HorizontalStackLayout { Spacing = 4, Children = { _icon, _text } };
        ApplyTone();
    }

    private void UpdateIcon()
    {
        _icon.Text = Icon;
        _icon.IsVisible = !string.IsNullOrEmpty(Icon);
    }

    private void ApplyTone()
    {
        var (sl, sd) = ToneColors.Strong(Tone);
        var (bl, bd) = ToneColors.Soft(Tone);
        this.SetAppThemeColor(BackgroundColorProperty, bl, bd);
        _text.SetAppThemeColor(Label.TextColorProperty, sl, sd);
        _icon.SetAppThemeColor(Label.TextColorProperty, sl, sd);
    }
}
