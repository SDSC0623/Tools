// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Media;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Tools.Models;

// 本地化存储窗口信息
public class WindowBaseInfo {
    public string ProcessName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public IntPtr Handle { get; set; } = IntPtr.Zero;
}

// 窗口信息
public class WindowInfo : WindowBaseInfo {
    public string Title { get; set; } = string.Empty;
    public ImageSource Icon { get; set; } = null!;

    // 存储时使用
    public WindowBaseInfo ToBase() => new() {
        ProcessName = ProcessName,
        ExecutablePath = ExecutablePath,
        Handle = Handle
    };
}