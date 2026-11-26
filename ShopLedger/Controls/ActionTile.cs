using System.Windows.Input;

namespace ShopLedger.Controls;

/// <summary>Tappable quick-action card: coloured icon chip + label. Exposed to screen readers as a button.</summary>
public class ActionTile : Border
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ActionTile), "", propertyChanged: (b, _, n) =>
        {
            var t = (ActionTile)b;
            t._text.Text = (string?)n;
            SemanticProperties.SetDescription(t._button, (string?)n);
            ToolTipProperties.SetText(t._button, (string?)n ?? "");
        });
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(ActionTile), "", propertyChanged: (b, _, n) => ((ActionTile)b)._icon.Text = (string?)n);
    public static readonly BindableProperty ToneProperty =
        BindableProperty.Create(nameof(Tone), typeof(Tone), typeof(ActionTile), Tone.Info, propertyChanged: (b, _, _) => ((ActionTile)b).ApplyTone());
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ActionTile), null, propertyChanged: (b, _, n) => ((ActionTile)b)._button.Command = (ICommand?)n);

    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public Tone Tone { get => (Tone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private readonly Label _icon = new() { FontFamily = Icons.FontFamily, FontSize = 20, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
    private readonly Label _text = new() { FontSize = 14, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center, LineBreakMode = LineBreakMode.WordWrap, MaxLines = 2 };
    private readonly Border _chip = new() { WidthRequest = 40, HeightRequest = 40, StrokeThickness = 0, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 } };
    // Transparent native button on top: keyboard focus, screen readers and click handling for the whole tile.
    private readonly Button _button = new()
    {
        BackgroundColor = Colors.Transparent, BorderWidth = 0, Padding = 0, Text = "",
        MinimumHeightRequest = 0, MinimumWidthRequest = 0, CornerRadius = 16,
    };

    public ActionTile()
    {
        if (Application.Current?.Resources.TryGetValue("Card", out var style) == true) Style = (Style)style;
        Padding = new Thickness(14, 12);
        MinimumHeightRequest = 64;
        _chip.Content = _icon;
        _text.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
        _text.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#0F172A"), Color.FromArgb("#E2E8F0"));
        var grid = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 12 };
        grid.Add(_chip, 0);
        grid.Add(_text, 1);
        AutomationProperties.SetIsInAccessibleTree(_chip, false);
        AutomationProperties.SetIsInAccessibleTree(_text, false);
        _button.Margin = new Thickness(-14, -12);   // cover the tile padding
        var root = new Grid();
        root.Add(grid);
        root.Add(_button);
        Content = root;
        ApplyTone();
    }

    private void ApplyTone()
    {
        var color = Tone switch
        {
            Tone.Teal => Color.FromArgb("#0F766E"),
            Tone.Success => Color.FromArgb("#059669"),
            Tone.Warning => Color.FromArgb("#D97706"),
            Tone.Danger => Color.FromArgb("#DC2626"),
            Tone.Neutral => Color.FromArgb("#14213D"),
            _ => Color.FromArgb("#2563EB"),
        };
        // navy disappears on dark cards: use a lighter slate-blue there
        _chip.SetAppThemeColor(BackgroundColorProperty, color, Tone == Tone.Neutral ? Color.FromArgb("#334E86") : color);
    }
}
