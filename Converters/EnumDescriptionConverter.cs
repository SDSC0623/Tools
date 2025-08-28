// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Tools.Helpers;

namespace Tools.Converters;

public class EnumDescriptionConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value == null ? DependencyProperty.UnsetValue : CommonHelper.GetEnumDescription(value);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return string.Empty;
    }
}