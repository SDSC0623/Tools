// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Management.Infrastructure;
using Serilog;
using Tools.Models;
using Tools.Services.IServices;
using Vanara.PInvoke;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.Services;

// 自定义异常类
public class WindowsEnumerationException : Exception {
    public WindowsEnumerationException(string message, Exception innerException)
        : base(message, innerException) {
    }
}

public class WindowsPickerService : IWindowsPickerService {
    private const User32.WindowStylesEx IgnoreStyles =
        User32.WindowStylesEx.WS_EX_TOOLWINDOW;

    private static readonly string[] WindowsDirs = [
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.SystemDirectory,
        @"C:\Windows", // 硬编码备选
        @"C:\WinNT" // 兼容旧系统
    ];

    // Logger
    private readonly ILogger _logger;

    public WindowsPickerService(ILogger logger) {
        _logger = logger;
    }

    public ObservableCollection<WindowInfo> GetAllWindows() {
        var windows = new ObservableCollection<WindowInfo>();
        try {
            User32.EnumWindows((hwnd, _) => {
                try {
                    if (IsValidWindow(hwnd)) {
                        var windowInfo = GetWindowInfo(hwnd);
                        if (windowInfo != null && !IsWindowsApp(windowInfo.ExecutablePath)) {
                            windows.Add(windowInfo);
                        }
                    }
                } catch (Exception e) {
                    _logger.Warning(e, "处理窗口 {Handle} 时发生错误，已跳过该窗口", (IntPtr)hwnd);
                }

                return true;
            }, IntPtr.Zero);
        } catch (Exception ex) {
            _logger.Error(ex, "枚举系统窗口时发生严重错误");
            throw new WindowsEnumerationException("无法获取窗口列表，请检查系统权限", ex);
        }

        return windows;
    }

    private static bool IsValidWindow(HWND hWnd, bool excludeSelf = true) {
        try {
            // 基本过滤：可见且有标题
            if (!User32.IsWindowVisible(hWnd) || (excludeSelf && IsCurrentWindow(hWnd))) {
                return false;
            }

            var titleLength = User32.GetWindowTextLength(hWnd);
            if (titleLength == 0) {
                return false;
            }

            // 排除工具窗口等特殊窗口
            var exStyle = User32.GetWindowLong<User32.WindowStylesEx>(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

            return (exStyle & IgnoreStyles) == 0;
        } catch {
            return false;
        }
    }

    private static bool IsCurrentWindow(HWND hWnd) {
        try {
            _ = User32.GetWindowThreadProcessId(hWnd, out var processId);
            return processId == Environment.ProcessId;
        } catch {
            return false;
        }
    }

    private static bool IsWindowsApp(string path) {
        return WindowsDirs.Any(dir =>
            !string.IsNullOrEmpty(dir) &&
            path.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBenignException(Exception ex) {
        return ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception;
    }

    private WindowInfo? GetWindowInfo(HWND hWnd) {
        try {
            _ = User32.GetWindowThreadProcessId(hWnd, out var processId);
            var process = Process.GetProcessById((int)processId);

            var icon = GetWindowIcon(hWnd);

            if (icon == null) {
                return null;
            }

            return new WindowInfo {
                Handle = (IntPtr)hWnd,
                ProcessName = process.ProcessName,
                ExecutablePath = GetExecutablePath(process),
                Title = process.MainWindowTitle,
                Icon = icon
            };
        } catch (Exception ex) when (IsBenignException(ex)) {
            _logger.Warning("获取 {Handle} 窗口信息失败，出现良性异常，跳过: {ExMessage}", (IntPtr)hWnd, ex.Message);
            return null;
        } catch (Exception e) {
            _logger.Error("获取 {Handle} 窗口信息失败: {ExMessage}", (IntPtr)hWnd, e.Message);
            return null;
        }
    }

    private string GetExecutablePath(Process process) {
        try {
            return process.MainModule?.FileName ?? string.Empty;
        } catch (Exception ex) {
            _logger.Warning(ex, "获取 {ProcessId} 进程可执行文件路径时出现错误，可能是权限不足，尝试使用 CIM 获取", process.Id);
            try {
                using var session = CimSession.Create(null);
                var tempProcess = session.QueryInstances(
                    @"root\cimv2",
                    "WQL",
                    $"SELECT * FROM Win32_Process WHERE ProcessId = {process.Id}");
                var cimInstance = tempProcess.FirstOrDefault(instance =>
                    (uint)instance.CimInstanceProperties["ProcessId"].Value == process.Id);
                return cimInstance?.CimInstanceProperties["ExecutablePath"].Value.ToString() ?? string.Empty;
            } catch (Exception e) {
                _logger.Error(e, "使用 CIM 获取 {ProcessId} 进程可执行文件路径时出现错误，跳过", process.Id);
                return string.Empty;
            }
        }
    }

    private ImageSource? GetWindowIcon(HWND hWnd) {
        try {
            const int iconSmall = 0; // 小图标
            const int iconBig = 1; // 大图标
            const int gclHicon = -14; // 类图标索引

            // 尝试获取窗口大图标
            var iconHandle = User32.SendMessage(hWnd, User32.WindowMessage.WM_GETICON, (IntPtr)iconBig, IntPtr.Zero);

            if (iconHandle == IntPtr.Zero) {
                // 尝试获取窗口小图标
                iconHandle = User32.SendMessage(hWnd, User32.WindowMessage.WM_GETICON, (IntPtr)iconSmall, IntPtr.Zero);
            }

            if (iconHandle == IntPtr.Zero) {
                // 尝试获取窗口类图标
                iconHandle = User32.GetClassLong(hWnd, gclHicon);
            }

            if (iconHandle != IntPtr.Zero) {
                return Imaging.CreateBitmapSourceFromHIcon(
                    iconHandle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
        } catch (Exception ex) {
            _logger.Error("获取窗口图标失败: {ExMessage}", ex.Message);
        }

        return null;
    }
}