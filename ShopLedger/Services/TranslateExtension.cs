using Microsoft.Maui.Controls.Xaml;

namespace ShopLedger.Services;

/// <summary>XAML: Text="{loc:Translate Some_Key}". The language is applied by rebuilding the Shell, so a static lookup is enough.</summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public class TranslateExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";
    public string ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
