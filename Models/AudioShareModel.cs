// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Tools.Models;

// 音频共享 - 进程信息
public partial class AudioProcessItem : ObservableObject {
    [ObservableProperty] private int _pid;

    [ObservableProperty] private string _processName = string.Empty;

    [ObservableProperty] private string _mainWindowTitle = string.Empty;

    [ObservableProperty] private ImageSource? _icon;

    public string DisplayText => string.IsNullOrWhiteSpace(MainWindowTitle)
        ? $"{ProcessName} (PID: {Pid})"
        : $"{ProcessName} (PID: {Pid}) - {MainWindowTitle}";
}

// 音频共享 - 接收端信息
public partial class ReceiverItem : ObservableObject {
    [ObservableProperty] private string _ip = string.Empty;

    [ObservableProperty] private int _port;

    [ObservableProperty] private bool _isActive;

    [ObservableProperty] private string _statusText = "未连接";

    [ObservableProperty] private DateTime _lastActiveTime;

    public string DisplayText => $"{Ip}:{Port}";

    public void UpdateActive() {
        LastActiveTime = DateTime.Now;
        IsActive = true;
        StatusText = "在线";
    }

    public void UpdateInactive() {
        IsActive = false;
        StatusText = "离线";
    }
}
