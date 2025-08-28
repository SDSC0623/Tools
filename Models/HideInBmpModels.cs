// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Tools.Models;

public enum ShowProgressMode {
    [Description("进度条+百分比")] Percent,
    [Description("文字")] Text
}