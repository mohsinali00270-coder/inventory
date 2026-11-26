using System.Globalization;

namespace ShopLedger.Services;

/// <summary>Binds a number into a translated template, e.g. Converter={StaticResource Fmt}, ConverterParameter=T_total
/// where T_total = "Total: Rs {0:N0}" (en) / "کل: {0:N0} روپے" (ur).</summary>
public class FormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || parameter is not string key) return "";
        return string.Format(Loc.Culture, Loc.T(key), value);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
