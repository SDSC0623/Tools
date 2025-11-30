// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Serilog;
using Tools.Views.Pages;
using Tools.Services;
using Tools.Services.IServices;
using Wpf.Ui;
using MessageBox = Wpf.Ui.Violeta.Controls.MessageBox;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.Helpers;

public class AppRunningHelper {
    // 导航服务
    private readonly INavigationService _navigationService;

    // Logger
    private readonly ILogger _logger;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    private readonly IWindowsPickerService _windowsPickerService;

    public AppRunningHelper(INavigationService navigationService, ILogger logger,
        IPreferencesService preferencesService, SnackbarServiceHelper snackbarService,
        IWindowsPickerService windowsPickerService) {
        _navigationService = navigationService;
        _logger = logger;
        _preferencesService = preferencesService;
        _snackbarService = snackbarService;
        _windowsPickerService = windowsPickerService;
    }

    private static string ReArguments() {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        for (int i = 0; i < args.Length; i++) {
            args[i] = $"""
                       "{args[i]}"
                       """;
        }

        return string.Join(" ", args);
    }

    private static void RestartAsAdmin(bool forced = false) {
        try {
            ProcessStartInfo startInfo = new() {
                UseShellExecute = true,
                WorkingDirectory = GlobalSettings.BaseDirectory,
                FileName = "Tools.exe",
                Arguments = ReArguments(),
                Verb = "runas"
            };
            try {
                _ = Process.Start(startInfo);
            } catch (Exception e) {
                App.GetService<ILogger>()!.Error("自动以管理员权限启动失败{ExMessage}", e.Message);
                MessageBox.Error("自动以管理员权限启动失败，非管理员权限下所有模拟操作功能均不可用！\r\n请尝试 右键 —— 以管理员身份运行的方式启动");
                return;
            }
        } catch (Win32Exception) {
            return;
        }

        if (forced) {
            Process.GetCurrentProcess().Kill();
        }

        Environment.Exit('r' + 'u' + 'n' + 'a' + 's');
    }

    public static void EnsureAdmin() {
        if (!GlobalSettings.IsAdmin) {
            RestartAsAdmin();
        }
    }

    public void StartApp() {
        try {
            var targetPage = _preferencesService.Get("StartPage", typeof(HomePage))!;
            _navigationService.Navigate(targetPage);
            _logger.Information("程序启动完成，详细版本: [{FullVersion}]", GlobalSettings.FullVersion);

            var windows = _windowsPickerService.GetAllWindows();
            string temp = "\n";
            foreach (var windowInfo in windows) {
                temp += windowInfo.Title + "\n" + windowInfo.ProcessName + "\n" + windowInfo.Handle + "\n" +
                        windowInfo.ExecutablePath + "\n" + "\n";
            }

            _logger.Information("获取所有窗口信息: {Windows}", temp);
        } catch (PreferencesException ex) {
            _navigationService.Navigate(typeof(HomePage));
            _preferencesService.Set("StartPage", typeof(HomePage));
            _snackbarService.ShowError("加载设定页面失败，已跳转到首页并重置本地化配置为首页", ex.Message);
        } catch (Exception ex) {
            _logger.Error("导航到设定启动页面时出错，错误：{Message}", ex.Message);
            _snackbarService.ShowError("导航到设定页面是时发生错误", ex.Message);
        }
    }

    public void Hide() {
        if (Application.Current.MainWindow!.Visibility != Visibility.Hidden) {
            Application.Current.MainWindow!.Hide();
        }
    }

    public void Show() {
        if (Application.Current.MainWindow!.Visibility != Visibility.Visible) {
            Application.Current.MainWindow.Activate();
            Application.Current.MainWindow.Focus();
            Application.Current.MainWindow.Show();
        }
    }

    public void ExitApp() {
        Application.Current.Shutdown();
    }
}