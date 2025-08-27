using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace Tools.Converters;

public class EnumDescriptionConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value == null ? DependencyProperty.UnsetValue : GetEnumDescription(value);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return string.Empty;
    }

    private static string GetEnumDescription(object enumObj) {
        var type = enumObj.GetType();
        if (!type.IsEnum) {
            return enumObj.ToString() ?? string.Empty;
        }

        var enumStr = enumObj.ToString();
        if (enumStr == null) {
            return string.Empty;
        }

        var fi = type.GetField(enumStr);
        if (fi == null) {
            return enumStr;
        }

        var attributes = fi.GetCustomAttribute<DescriptionAttribute>(false);
        return attributes?.Description ?? fi.Name;
    }
}