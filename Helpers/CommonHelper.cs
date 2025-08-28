using System.ComponentModel;
using System.Reflection;

namespace Tools.Helpers;

public class CommonHelper {
    public static string GetEnumDescription(object enumObj) {
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