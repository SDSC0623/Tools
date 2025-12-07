// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
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
    private const User32.WindowStylesEx IgnoreStyles = User32.WindowStylesEx.WS_EX_TOOLWINDOW;

    private static readonly string[] WindowsDirs = [
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.SystemDirectory,
        @"C:\Windows", // 硬编码备选
        @"C:\WinNT" // 兼容旧系统
    ];

    // Logger
    private readonly ILogger _logger;

    // 截图任务
    private Task? _captureTask;

    // 取消令牌
    private CancellationTokenSource? _cancellationTokenSource;

    public WindowsPickerService(ILogger logger) {
        _logger = logger;
    }

    public async Task<ObservableCollection<WindowInfo>> GetAllWindowsAsync(bool needVisible = true) {
        var windows = new ObservableCollection<WindowInfo>();
        try {
            var tempHandles = new List<HWND>();
            User32.EnumWindows((hwnd, _) => {
                try {
                    if (IsValidWindow(hwnd, needVisible: needVisible)) {
                        tempHandles.Add(hwnd);
                    }
                } catch (Exception e) {
                    _logger.Warning(e, "处理窗口 {Handle} 时发生错误，已跳过该窗口", (IntPtr)hwnd);
                }

                return true;
            }, IntPtr.Zero);

            var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
            var tasks = tempHandles.Select(async hwnd => {
                await semaphore.WaitAsync();
                try {
                    return await Task.Run(() => {
                        try {
                            var windowInfo = GetWindowInfo(hwnd);
                            if (windowInfo != null && !IsWindowsApp(windowInfo.ExecutablePath)) {
                                return windowInfo;
                            }
                        } catch (Exception ex) {
                            _logger.Warning(ex, "处理窗口 {Handle} 时发生错误，已跳过", (IntPtr)hwnd);
                        }

                        return null;
                    });
                } finally {
                    semaphore.Release();
                }
            }).ToList();

            // 等待所有任务完成
            var results = await Task.WhenAll(tasks);

            // 第三步：过滤、排序并添加到集合
            var validWindows = results
                .Where(info => info != null)
                .OrderBy(info => info!.ProcessName);

            foreach (var windowInfo in validWindows) {
                windows.Add(windowInfo!);
            }
        } catch (Exception ex) {
            _logger.Error(ex, "枚举系统窗口时发生严重错误");
            throw new WindowsEnumerationException("无法获取窗口列表，请检查系统权限", ex);
        }

        return windows;
    }

    private static bool IsValidWindow(HWND hWnd, bool needVisible = true, bool excludeSelf = true) {
        try {
            // 基本过滤：可见且有标题
            if ((needVisible && !User32.IsWindowVisible(hWnd)) || (excludeSelf && IsCurrentWindow(hWnd))) {
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
                ExecutablePath = GetExecutablePathAdvanced(process) ?? "[拒绝访问，无法获取]",
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

    private string? GetExecutablePathAdvanced(Process process) {
        // 1.  P/Invoke
        var path = GetExecutablePathByVanara(process);
        if (!string.IsNullOrEmpty(path)) {
            return path;
        }

        // 2. 快速方法
        path = GetExecutablePathByProcessModule(process);
        if (!string.IsNullOrEmpty(path)) {
            return path;
        }

        // 3. CIM
        path = GetExecutablePathByCim(process);
        return path;
    }

    private string? GetExecutablePathByVanara(Process process) {
        using var processHandle = OpenProcessWithVanara(process.Id);

        if (processHandle.IsInvalid) {
            _logger.Warning("无法打开进程 {ProcessId} 的句柄", process.Id);
            return null;
        }

        try {
            var pathBuilder = new StringBuilder(512);
            var result =
                Kernel32.GetModuleFileNameEx(processHandle, HINSTANCE.NULL, pathBuilder, (uint)pathBuilder.Capacity);

            if (result > 0) {
                var path = pathBuilder.ToString();
                return path;
            }

            _logger.Error("GetModuleFileNameEx 失败，最后一个错误: {Error}", Kernel32.GetLastError());
            return null;
        } catch (Exception ex) {
            _logger.Error("Vanara.PInvoke 方法失败: {Message}", ex.Message);
            return null;
        }
    }

    private Kernel32.SafeHPROCESS OpenProcessWithVanara(int processId) {
        try {
            var processHandle = Kernel32.OpenProcess(
                new ACCESS_MASK(Kernel32.ProcessAccess.PROCESS_QUERY_LIMITED_INFORMATION),
                false, (uint)processId);

            if (!processHandle.IsInvalid) {
                return processHandle;
            }

            processHandle = Kernel32.OpenProcess(
                new ACCESS_MASK(Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION |
                                Kernel32.ProcessAccess.PROCESS_VM_READ),
                false, (uint)processId);

            return processHandle;
        } catch (Exception ex) {
            _logger.Error("打开进程句柄失败: {Message}", ex.Message);
            return Kernel32.SafeHPROCESS.Null;
        }
    }

    private string? GetExecutablePathByProcessModule(Process process) {
        try {
            return process.MainModule?.FileName;
        } catch (Exception ex) {
            _logger.Error("Process.MainModule 方法失败: {Message}", ex.Message);
            return null;
        }
    }

    private string? GetExecutablePathByCim(Process process) {
        try {
            using var session = CimSession.Create(null);
            var instances = session.QueryInstances(
                @"root\cimv2",
                "WQL",
                $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {process.Id}");
            var instance = instances.FirstOrDefault();
            return instance?.CimInstanceProperties["ExecutablePath"]?.Value?.ToString();
        } catch (Exception ex) {
            _logger.Error("CIM 方法失败: {Message}", ex.Message);
            return null;
        }
    }

    private ImageSource? GetWindowIcon(HWND hWnd) {
        try {
            const int iconSmall = 0; // 小图标
            const int iconBig = 1; // 大图标
            const int gclHicon = -14; // 类图标索引

            var iconHandle = User32.SendMessage(hWnd, User32.WindowMessage.WM_GETICON, (IntPtr)iconBig, IntPtr.Zero);

            if (iconHandle == IntPtr.Zero) {
                iconHandle = User32.SendMessage(hWnd, User32.WindowMessage.WM_GETICON, (IntPtr)iconSmall, IntPtr.Zero);
            }

            if (iconHandle == IntPtr.Zero) {
                iconHandle = User32.GetClassLong(hWnd, gclHicon);
            }

            if (iconHandle != IntPtr.Zero) {
                var icon = Imaging.CreateBitmapSourceFromHIcon(
                    iconHandle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                if (icon.CanFreeze) {
                    icon.Freeze();
                }

                return icon;
            }
        } catch (Exception ex) {
            _logger.Error("获取窗口图标失败: {ExMessage}", ex.Message);
        }

        return null;
    }

    public void StartShowWindow(WindowInfo windowInfo, Action<ImageSource> onFrameCaptured, double fps = 24) {
        if (windowInfo.Handle == IntPtr.Zero) {
            return;
        }

        if (!CanCaptureWindow(windowInfo)) {
            _logger.Warning("无法捕获 {Title} 窗口", windowInfo.DisplayTitle);
            return;
        }

        StopShowWindow();

        var targetFps = Math.Clamp(fps, 10, 60);
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        var captureSemaphore = new SemaphoreSlim(1, 1);

        _captureTask = Task.Run(async () => {
            var interval = TimeSpan.FromMilliseconds(1000.0 / targetFps);
            var stopwatch = new Stopwatch();
            while (!token.IsCancellationRequested) {
                try {
                    stopwatch.Restart();

                    // 使用信号量确保同一时间只有一个截图在进行
                    await captureSemaphore.WaitAsync(token);

                    try {
                        var image = CaptureWithPrintWindow(windowInfo.Handle);
                        if (image != null) {
                            onFrameCaptured(image);
                        }
                    } finally {
                        captureSemaphore.Release();
                    }

                    var elapsed = stopwatch.Elapsed;
                    if (elapsed < interval) {
                        await Task.Delay(interval - elapsed, token);
                    }
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception ex) {
                    _logger.Error(ex, "实时捕获发生错误");
                    await Task.Delay(1000, token);
                }
            }
        }, token);

        if (_captureTask.IsFaulted) {
            _logger.Warning("实时捕获线程的异常: {Ex}", _captureTask.Exception);
        }
    }

    public void StopShowWindow() {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _captureTask = null;
    }

    private bool CanCaptureWindow(WindowInfo windowInfo) {
        try {
            if (User32.IsIconic(windowInfo.Handle)) {
                return false;
            }

            if (!User32.GetWindowRect(windowInfo.Handle, out var rect) || rect.Width <= 10 || rect.Height <= 10) {
                return false;
            }

            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            return rect is { Right: > 0, Bottom: > 0 } &&
                   rect.Left < screenWidth && rect.Top < screenHeight;
        } catch {
            return false;
        }
    }

    private ImageSource? CaptureWithPrintWindow(IntPtr hwnd) {
        try {
            if (!User32.GetWindowRect(hwnd, out var rect) || rect.Width <= 0 || rect.Height <= 0) {
                return null;
            }

            using var sourceDc = User32.GetDC(HWND.NULL);
            using var destDc = Gdi32.CreateCompatibleDC(sourceDc);
            using var bitmap = Gdi32.CreateCompatibleBitmap(sourceDc, rect.Width, rect.Height);

            var oldBitmap = Gdi32.SelectObject(destDc, bitmap);

            try {
                var success = User32.PrintWindow(hwnd, destDc, User32.PW.PW_RENDERFULLCONTENT);

                if (!success) {
                    success = User32.PrintWindow(hwnd, destDc, 0);
                }

                if (success) {
                    var temp = ConvertBitmapToImageSource(bitmap);
                    if (temp is not null && temp.CanFreeze) {
                        temp.Freeze();
                    }

                    return temp;
                }
            } finally {
                Gdi32.SelectObject(destDc, oldBitmap);
            }
        } catch (Exception ex) {
            _logger.Error("PrintWindow 捕获失败: {Message}", ex.Message);
        }

        return null;
    }

    private ImageSource? ConvertBitmapToImageSource(Gdi32.SafeHBITMAP hBitmap) {
        try {
            IntPtr hBitmapPtr = hBitmap.DangerousGetHandle();

            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmapPtr,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bitmapSource.Freeze();
            return bitmapSource;
        } catch (Exception ex) {
            _logger.Error(ex, "转换位图失败");
            return null;
        }
    }
}