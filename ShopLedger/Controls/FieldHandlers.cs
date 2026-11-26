using Microsoft.Maui.Handlers;

namespace ShopLedger.Controls;

/// <summary>Removes the native underline / border of text inputs. Every input in the app sits inside a rounded
/// "Field" Border, so the platform chrome would otherwise draw a second frame.</summary>
public static class FieldHandlers
{
    public static void Configure()
    {
#if ANDROID
        var clear = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        EntryHandler.Mapper.AppendToMapping("Borderless", (h, _) => h.PlatformView.BackgroundTintList = clear);
        EditorHandler.Mapper.AppendToMapping("Borderless", (h, _) => h.PlatformView.BackgroundTintList = clear);
        PickerHandler.Mapper.AppendToMapping("Borderless", (h, _) => h.PlatformView.BackgroundTintList = clear);
        DatePickerHandler.Mapper.AppendToMapping("Borderless", (h, _) => h.PlatformView.BackgroundTintList = clear);
#elif WINDOWS
        var none = new Microsoft.UI.Xaml.Thickness(0);
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        void Flatten(Microsoft.UI.Xaml.Controls.Control c)
        {
            c.BorderThickness = none;
            foreach (var key in new[] { "TextControlBorderThemeThickness", "TextControlBorderThemeThicknessFocused", "ComboBoxBorderThemeThickness", "CalendarDatePickerBorderThemeThickness" })
                c.Resources[key] = none;
            foreach (var key in new[] { "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
                                         "ComboBoxBackground", "ComboBoxBackgroundPointerOver", "ComboBoxBackgroundFocused",
                                         "CalendarDatePickerBackground", "CalendarDatePickerBackgroundPointerOver", "CalendarDatePickerBackgroundFocused" })
                c.Resources[key] = transparent;
        }
        EntryHandler.Mapper.AppendToMapping("Borderless", (h, _) => Flatten(h.PlatformView));
        EditorHandler.Mapper.AppendToMapping("Borderless", (h, _) => Flatten(h.PlatformView));
        PickerHandler.Mapper.AppendToMapping("Borderless", (h, _) => Flatten(h.PlatformView));
        // WinUI shows Picker.Title as a header above the box; inside our labelled fields it belongs in the box as a placeholder.
        void TitleAsPlaceholder(IPickerHandler h, IPicker v)
        {
            h.PlatformView.Header = null;
            h.PlatformView.HeaderTemplate = null;
            h.PlatformView.PlaceholderText = v.Title ?? "";
        }
        PickerHandler.Mapper.AppendToMapping(nameof(Picker.Title), TitleAsPlaceholder);
        PickerHandler.Mapper.AppendToMapping(nameof(Picker.TitleColor), TitleAsPlaceholder);
        DatePickerHandler.Mapper.AppendToMapping("Borderless", (h, _) => Flatten(h.PlatformView));
#endif
    }
}
