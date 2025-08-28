using System.ComponentModel;

namespace Tools.Models;

public enum ShowProgressMode {
    [Description("进度条+百分比")] Percent,
    [Description("文字")] Text
}