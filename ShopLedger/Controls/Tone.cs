namespace ShopLedger.Controls;

/// <summary>Semantic colour of a badge / KPI accent. Each tone has a strong colour (text, icon) and a soft tint (background),
/// with separate values for light and dark theme so contrast stays readable.</summary>
public enum Tone { Info, Success, Warning, Danger, Teal, Neutral }

public static class ToneColors
{
    public static (Color Light, Color Dark) Strong(Tone t) => t switch
    {
        Tone.Success => (Color.FromArgb("#047857"), Color.FromArgb("#34D399")),
        Tone.Warning => (Color.FromArgb("#B45309"), Color.FromArgb("#FBBF24")),
        Tone.Danger => (Color.FromArgb("#DC2626"), Color.FromArgb("#F87171")),
        Tone.Teal => (Color.FromArgb("#0F766E"), Color.FromArgb("#2DD4BF")),
        Tone.Neutral => (Color.FromArgb("#475569"), Color.FromArgb("#A5B4CB")),
        _ => (Color.FromArgb("#2563EB"), Color.FromArgb("#60A5FA")),
    };

    public static (Color Light, Color Dark) Soft(Tone t) => t switch
    {
        Tone.Success => (Color.FromArgb("#D1FAE5"), Color.FromArgb("#0F3B2E")),
        Tone.Warning => (Color.FromArgb("#FEF3C7"), Color.FromArgb("#3D2C0A")),
        Tone.Danger => (Color.FromArgb("#FEE2E2"), Color.FromArgb("#401518")),
        Tone.Teal => (Color.FromArgb("#CCFBF1"), Color.FromArgb("#0D3A36")),
        Tone.Neutral => (Color.FromArgb("#F1F5F9"), Color.FromArgb("#1E293B")),
        _ => (Color.FromArgb("#DBEAFE"), Color.FromArgb("#172C57")),
    };
}
