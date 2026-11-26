using System.Globalization;

namespace ShopLedger.Controls;

/// <summary>"Ahmed Khan" -> "AK", "ali" -> "A". Used for customer avatars.</summary>
public class InitialsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (value as string ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var letters = parts.Take(2).Select(p => char.ToUpper(p[0], culture)).ToArray();
        return letters.Length == 0 ? "?" : new string(letters);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>true when the value is a non-zero number (int / decimal / double).</summary>
public class NonZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        int i => i != 0,
        decimal d => d != 0,
        double x => x != 0,
        _ => false,
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>true when a count is 0. Used for empty states placed in a CollectionView header
/// (on Windows the EmptyView is not shown when the CollectionView also has a Header).</summary>
public class IsZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is int i && i == 0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
