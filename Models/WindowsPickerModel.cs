// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Tools.Helpers;

// ReSharper disable NonReadonlyMemberInGetHashCode

#pragma warning disable CS0657 // 不是此声明的有效特性位置

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Tools.Models;

// 窗口信息
public partial class WindowInfo : ObservableObject {
    [ObservableProperty] private string _processName = string.Empty;
    [ObservableProperty] private string _executablePath = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _title = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _remarksTitle = string.Empty;

    [JsonIgnore]
    public string DisplayTitle => string.IsNullOrWhiteSpace(RemarksTitle) ? Title : $"{RemarksTitle}\n(当前标题: {Title})";

    [ObservableProperty] private IntPtr _handle = IntPtr.Zero;
    [ObservableProperty] private DateTime _lastStartTime;

    [property: JsonIgnore] [ObservableProperty]
    private string _describeText = string.Empty;

    [JsonProperty] private string IconData { get; set; } = string.Empty;

    [property: JsonIgnore] [ObservableProperty]
    private ImageSource _icon = null!;

    [ObservableProperty] private bool _hasStartToday;

    [property: JsonIgnore] [ObservableProperty]
    private bool _isOperateMenuOpen;

    partial void OnIconChanged(ImageSource value) {
        IconData = IconHelper.ImageSourceToBase64(value);
    }

    public WindowInfo() {
    }

    [JsonConstructor]
    public WindowInfo(string iconData) {
        IconData = iconData;
        Init();
    }

    private void Init() {
        Icon = IconHelper.Base64ToImageSource(IconData) ?? null!;
    }

    public void SetStartToday(DateTime now, TimeSpan daySeparatorOffset) {
        HasStartToday = true;
        LastStartTime = now;
        UpdateText(now.Date + daySeparatorOffset);
    }

    public void SetNotStartToday(DateTime now, TimeSpan daySeparatorOffset) {
        HasStartToday = false;
        LastStartTime = now.Date - TimeSpan.FromHours(12);
        UpdateText(now.Date + daySeparatorOffset);
    }

    public void UpdateInfo(WindowInfo? newInfo) {
        if (newInfo is null) {
            return;
        }

        Handle = newInfo.Handle;
        Title = string.IsNullOrEmpty(newInfo.Title) ? Title : newInfo.Title;
        Icon = newInfo.Icon;
    }

    public void UpdateStatus(DateTime now, TimeSpan daySeparatorOffset, bool isRunning,
        Action<string> notifiedHasStart) {
        if (isRunning) {
            if (!HasStartToday && now >= now.Date + daySeparatorOffset) {
                HasStartToday = true;
                LastStartTime = now;
                notifiedHasStart(ProcessName);
            } else if (HasStartToday && now >= now.Date + daySeparatorOffset &&
                       LastStartTime < now.Date + daySeparatorOffset) {
                LastStartTime = now;
                notifiedHasStart(ProcessName);
            }
        } else {
            if (HasStartToday && now >= now.Date + daySeparatorOffset &&
                LastStartTime < now.Date + daySeparatorOffset) {
                HasStartToday = false;
            }
        }


        UpdateText(now.Date + daySeparatorOffset);
    }

    private void UpdateText(DateTime daysSeparatorTime) {
        if (LastStartTime == DateTime.MinValue) {
            DescribeText = "未启动过";
            return;
        }

        if (LastStartTime < daysSeparatorTime) {
            DescribeText = $"上次启动时间:\n{LastStartTime:yyyy-MM-dd HH:mm:ss}";
            return;
        }

        DescribeText = $"今天首次启动时间:\n{LastStartTime:yyyy-MM-dd HH:mm:ss}";
    }

    public override bool Equals(object? obj) {
        if (obj is WindowInfo windowInfo) {
            if (!string.IsNullOrEmpty(ExecutablePath) && !string.IsNullOrEmpty(windowInfo.ExecutablePath)) {
                return ExecutablePath == windowInfo.ExecutablePath;
            }

            return ProcessName == windowInfo.ProcessName;
        }

        return false;
    }

    public override int GetHashCode() {
        unchecked {
            int hash = 17;

            if (!string.IsNullOrEmpty(ExecutablePath)) {
                hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(ExecutablePath);
            } else {
                hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(ProcessName);
            }

            return hash;
        }
    }

    public static bool operator ==(WindowInfo? left, WindowInfo? right) {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(WindowInfo? left, WindowInfo? right) {
        return !(left == right);
    }
}