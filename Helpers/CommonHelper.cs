// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

// ReSharper disable ClassNeverInstantiated.Global

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