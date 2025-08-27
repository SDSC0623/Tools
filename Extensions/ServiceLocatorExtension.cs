using System.Windows.Markup;

namespace Tools.Extensions;

[MarkupExtensionReturnType(typeof(object))]
public class ServiceLocatorExtension : MarkupExtension {
    public Type Type { get; set; } = null!;

    public override object ProvideValue(IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(Type);
        return App.GetService(Type)!;
    }
}