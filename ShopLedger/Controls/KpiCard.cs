namespace ShopLedger.Controls;

/// <summary>Dashboard metric card: tinted icon chip, title, large value and a caption line.</summary>
public class KpiCard : Border
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(KpiCard), "", propertyChanged: (b, _, n) => ((KpiCard)b)._title.Text = (string?)n);
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(KpiCard), "", propertyChanged: (b, _, n) => ((KpiCard)b)._value.Text = (string?)n);
    public static readonly BindableProperty CaptionProperty =
        BindableProperty.Create(nameof(Caption), typeof(string), typeof(KpiCard), "", propertyChanged: (b, _, n) =>
        {
            var k = (KpiCard)b;
            k._caption.Text = (string?)n;
            k._caption.IsVisible = !string.IsNullOrEmpty((string?)n);
        });
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(KpiCard), "", propertyChanged: (b, _, n) => ((KpiCard)b)._iconLabel.Text = (string?)n);
    public static readonly BindableProperty ToneProperty =
        BindableProperty.Create(nameof(Tone), typeof(Tone), typeof(KpiCard), Tone.Info, propertyChanged: (b, _, _) => ((KpiCard)b).ApplyTone());

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public Tone Tone { get => (Tone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    private readonly Label _title = new() { FontSize = 13, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalTextAlignment = TextAlignment.Center };
    private readonly Label _value = new() { FontSize = 24, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Label _caption = new() { FontSize = 12, IsVisible = false, MaxLines = 2, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label _iconLabel = new() { FontFamily = Icons.FontFamily, FontSize = 20, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
    private readonly Border _chip = new()
    {
        WidthRequest = 40, HeightRequest = 40, StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
    };

    public KpiCard()
    {
        if (Application.Current?.Resources.TryGetValue("Card", out var style) == true) Style = (Style)style;
        _chip.Content = _iconLabel;

        foreach (var l in new[] { _title, _value, _caption }) l.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        _title.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#475569"), Color.FromArgb("#A5B4CB"));
        _value.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#0F172A"), Color.FromArgb("#F1F5F9"));
        _caption.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#64748B"), Color.FromArgb("#8A9AB5"));

        var header = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 10 };
        header.Add(_chip, 0);
        header.Add(_title, 1);

        Content = new VerticalStackLayout { Spacing = 8, Children = { header, _value, _caption } };
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level3);
        ApplyTone();
    }

    private void ApplyTone()
    {
        var (sl, sd) = ToneColors.Strong(Tone);
        var (bl, bd) = ToneColors.Soft(Tone);
        _chip.SetAppThemeColor(BackgroundColorProperty, bl, bd);
        _iconLabel.SetAppThemeColor(Label.TextColorProperty, sl, sd);
    }
}
