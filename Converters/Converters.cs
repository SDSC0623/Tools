using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Tools.Converters;

public class UnexpectedCallException(string message = "这不应该被调用，请检查逻辑") : Exception(message);

// 比赛类型到图标转换器
public class ContestTypeToIconConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is string type) {
            return type switch {
                "CF" => SymbolRegular.Trophy24,
                "ICPC" => SymbolRegular.PeopleTeam24,
                "IOI" => SymbolRegular.Star24,
                _ => SymbolRegular.QuestionCircle24
            };
        }

        return SymbolRegular.QuestionCircle24;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 时间戳转换器
public class DateTimeConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is DateTime time) {
            return time.ToString("yyyy-MM-dd HH:mm");
        }

        return "未知时间";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 持续时间转换器
public class TimeSpanConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not TimeSpan duration) {
            return "未知时长";
        }

        var flag = parameter is "True";

        if (flag) {
            // 拆解时间间隔组件
            var days = duration.Days;
            var hours = duration.Hours;
            var minutes = duration.Minutes;

            // 优先使用字符串构建器提高性能
            var parts = new List<string>();

            // 添加天数组件
            if (days > 0) {
                parts.Add($"{days}天");
            }

            // 添加小时组件
            if (hours > 0 || (days > 0 && minutes > 0)) {
                parts.Add($"{hours}小时");
            }

            // 添加分钟组件
            if (minutes > 0) {
                parts.Add($"{minutes}分");
            }

            // 处理所有单位都为零的情况
            return parts.Count == 0 ? $"{duration.TotalMinutes}秒" : string.Join("", parts);
        } else {
            // 拆解时间间隔组件
            var days = duration.Days;
            var hours = duration.Hours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;

            // 优先使用字符串构建器提高性能
            var parts = new List<string>();

            // 添加天数组件
            if (days > 0) {
                parts.Add($"{days}天");
            }

            // 添加小时组件（当天数>0时，显示完整小时）
            if (hours > 0 || days > 0) {
                parts.Add($"{hours:D2}小时");
            }

            // 添加分钟组件（当天数或小时>0时，显示完整分钟）
            if (minutes > 0 || hours > 0 || days > 0) {
                parts.Add($"{minutes:D2}分钟");
            }

            // 添加秒组件（没有超过1天时，显示完整秒数）
            if (seconds > 0 || minutes > 0 || hours > 0 || days > 0) {
                parts.Add($"{seconds:D2}秒");
            }

            return string.Join("", parts);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 比赛状态到颜色转换器
public class PhaseToColorConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is string phase) {
            return phase switch {
                "BEFORE" => Brushes.DodgerBlue,
                "CODING" => Brushes.ForestGreen,
                "PENDING_SYSTEM_TEST" => Brushes.Orange,
                "SYSTEM_TEST" => Brushes.OrangeRed,
                "FINISHED" => Brushes.Blue,
                _ => Brushes.Gray
            };
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

public class PhaseToIconConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is string phase) {
            return phase switch {
                "BEFORE" => SymbolRegular.Clock24,
                "CODING" => SymbolRegular.Play24,
                "PENDING_SYSTEM_TEST" => SymbolRegular.Hourglass24,
                "SYSTEM_TEST" => SymbolRegular.DesktopSync16,
                "FINISHED" => SymbolRegular.Checkmark24,
                _ => SymbolRegular.QuestionCircle24
            };
        }

        return SymbolRegular.QuestionCircle24;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 比赛状态到描述转换器
public class PhaseToTextConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is string phase) {
            return phase switch {
                "BEFORE" => "未开始",
                "CODING" => "进行中",
                "PENDING_SYSTEM_TEST" => "等待系统评测",
                "SYSTEM_TEST" => "系统评测中",
                "FINISHED" => "已结束",
                _ => "未知状态"
            };
        }

        return "未知状态";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 冻结状态到是否显示转换器
public class BooleanToVisibleConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not bool frozen) {
            return Visibility.Visible;
        }

        if (parameter is "True") {
            frozen = !frozen;
        }

        return frozen switch {
            true => Visibility.Visible,
            false => Visibility.Collapsed
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 开始时间到描述转换器
public class StartTimeToTextConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        if (values[0] is not DateTime startTime || values[1] is not DateTime endTime) {
            return "已结束，此部分应当隐藏，请检查代码";
        }

        if (startTime > DateTime.Now) {
            return "距离开始还有: ";
        }

        return endTime > DateTime.Now ? "距离结束还有: " : "已结束，此部分应当隐藏，请检查代码";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 结束时间点到是否显示转换器
public class EndTimeToVisibleConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not DateTime time) {
            return Visibility.Collapsed;
        }

        return time > DateTime.Now ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 结束时间点到是否显示转换器
public class OnlineStatusToTextConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool isOnline) {
            return isOnline ? "在线" : "离线";
        }

        return "未知状态";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// 结束时间点到是否显示转换器
public class OnlineStatusToColorConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is bool isOnline) {
            return isOnline ? Brushes.Green : Brushes.DarkGray;
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// double到百分比转换器
public class ProgressToPercentConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is double progress) {
            return $"{progress * 100:F2}%";
        }

        return "未知数值 %";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}

// long到文件大小转换器
// ReSharper disable InconsistentNaming
public class LongToFileSizeConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is long size) {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            const long TB = GB * 1024;

            if (parameter is "True") {
                return $"具体大小: {size} B";
            }

            return size switch {
                < KB => $"{size} B",
                < MB => $"{size / (double)KB:F2} KB",
                < GB => $"{size / (double)MB:F2} MB",
                < TB => $"{size / (double)GB:F2} GB",
                _ => $"{size / (double)TB:F2} TB"
            };
        }

        return $"未知文件大小: {value}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new UnexpectedCallException();
    }
}