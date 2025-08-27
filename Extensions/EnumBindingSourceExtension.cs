using System.Windows.Markup;

// ReSharper disable UnusedMember.Global

namespace Tools.Extensions;

public class EnumBindingSourceExtension : MarkupExtension {
    private Type? _enumType;

    public Type? EnumType {
        get => _enumType;
        set {
            if (value != _enumType) {
                if (value != null) {
                    var enumType = Nullable.GetUnderlyingType(value) ?? value;
                    if (!enumType.IsEnum) {
                        throw new ArgumentException("需传入枚举类型");
                    }
                }

                _enumType = value;
            }
        }
    }

    public EnumBindingSourceExtension() {
    }

    public EnumBindingSourceExtension(Type enumType) {
        EnumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) {
        if (_enumType == null) {
            throw new InvalidOperationException("需设定枚举类型");
        }

        var actualEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
        var enumValues = Enum.GetValues(actualEnumType);

        if (actualEnumType == _enumType) {
            return enumValues;
        }

        var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
        enumValues.CopyTo(tempArray, 1);
        return tempArray;
    }
}