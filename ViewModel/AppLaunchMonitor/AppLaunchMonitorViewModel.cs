// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Tools.Helpers;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.ViewModel.GetTextsDialogViewModel;
using Tools.Views.Pages.AppLaunchMonitor;
using Tools.Views.Windows;
using Vanara.PInvoke;
using Wpf.Ui.Controls;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.ViewModel.AppLaunchMonitor;

public partial class AppLaunchMonitorViewModel : ObservableObject {
    [ObservableProperty] private ObservableCollection<WindowInfo> _monitoredApps = [];

    [ObservableProperty] private bool _hasMonitoredApps;

    [ObservableProperty] private int _todayLaunchCount;

    [ObservableProperty] private DateTime _lastCheckTime = DateTime.Now;

    [ObservableProperty] private TimeSpan _daySeparatorOffset;

    // 日志
    private readonly ILogger _logger;

    // 窗口获取服务
    private readonly IWindowsPickerService _windowsPickerService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // Window消息服务
    private readonly INotificationService _notificationService;

    // 钩子
    private User32.HWINEVENTHOOK _hook = User32.HWINEVENTHOOK.NULL;

    // 钩子处理程序
    private User32.WinEventProc _winEventProc = null!;

    public AppLaunchMonitorViewModel(IWindowsPickerService windowsPickerService, ILogger logger,
        IPreferencesService preferencesService, INotificationService notificationService) {
        _logger = logger;
        _preferencesService = preferencesService;
        _notificationService = notificationService;
        _windowsPickerService = windowsPickerService;
        Init();
    }

    private void Init() {
        MonitoredApps = _preferencesService.Get("MonitoredAppsList", new ObservableCollection<WindowInfo>())!;
        var temp = _preferencesService.Get("DaySeparatorOffset", new TimeRange { Value = 0, Unit = TimeUnit.Hour })!;
        DaySeparatorOffset = temp.ToTimeSpan();
        UpdateData();
        _ = RefreshAndCheck();
        _ = InitMonitor();
    }

    private Task InitMonitor() {
        _winEventProc = (hook, @event, hwnd, idObject, child, thread, time) => {
            User32.GetWindowThreadProcessId(hwnd, out var processId);
            var process = Process.GetProcessById((int)processId);
            if (idObject != User32.ObjectIdentifiers.OBJID_WINDOW || processId == Environment.ProcessId ||
                MonitoredApps.All(info => info.ProcessName != process.ProcessName)) {
                return;
            }

            _ = RefreshAndCheck();
            User32.UnhookWinEvent(_hook);
            CreateHook();
        };
        CreateHook();
        return Task.CompletedTask;
    }

    private void CreateHook() {
        _hook = User32.SetWinEventHook(
            User32.EventConstants.EVENT_OBJECT_SHOW,
            User32.EventConstants.EVENT_OBJECT_SHOW,
            IntPtr.Zero,
            _winEventProc,
            0, 0, User32.WINEVENT.WINEVENT_OUTOFCONTEXT | User32.WINEVENT.WINEVENT_SKIPOWNPROCESS
        );
    }

    public void Dispose() {
        if (_hook is { IsNull: false, IsInvalid: false }) {
            User32.UnhookWinEvent(_hook);
        }

        NotifiedHasStartByWindowsToast("App监视器已关闭");
        NotifiedHasStartByEmail();
    }

    private void SaveData() {
        _ = _preferencesService.Set("MonitoredAppsList", MonitoredApps);
    }

    private void UpdateData() {
        HasMonitoredApps = MonitoredApps.Count > 0;
        TodayLaunchCount = MonitoredApps.Count(x => x.HasStartToday);
    }

    private async Task CheckApp() {
        var temp = await _windowsPickerService.GetAllWindowsAsync();
        List<string> newStartApps = [];
        foreach (var windowInfo in MonitoredApps) {
            var newInfo = temp.FirstOrDefault(info => info == windowInfo);
            windowInfo.UpdateInfo(newInfo);
            windowInfo.UpdateStatus(DateTime.Now, DaySeparatorOffset, newInfo is not null, s => newStartApps.Add(s));
        }

        if (newStartApps.Count != 0) {
            NotifiedHasStartByWindowsToast($"检测到程序 {string.Join(", ", newStartApps)} 启动");
        }
    }

    private void NotifiedHasStartByWindowsToast(string title) {
        if (MonitoredApps.Count == 0 || !_preferencesService.Get("NeedWindowsToastNotification", false)) {
            return;
        }

        var apps = GetNotStartApps();

        var content = apps.Count == 0 ? "标记的应用已全部启动" : $"{apps.Count} 个应用未启动";

        _notificationService.ShowWindowsToastNotification(title, [content, string.Join(", ", apps)],
            TimeSpan.FromHours(1));
    }

    private void NotifiedHasStartByEmail() {
        try {
            if (MonitoredApps.Count == 0 || !_preferencesService.Get("NeedEmailNotification", false)) {
                return;
            }

            var smtpServerAddress = _preferencesService.Get("EmailNotificationSmtpServerAddress", "");
            var smtpServerPort = _preferencesService.Get<int?>("EmailNotificationSmtpServerPort");
            var emailAddress = _preferencesService.Get("EmailNotificationAddress", "");
            var emailAuthCode = _preferencesService.Get("EmailNotificationAuthCode", "");
            if (string.IsNullOrWhiteSpace(smtpServerAddress) ||
                smtpServerPort == null ||
                string.IsNullOrWhiteSpace(emailAddress) ||
                string.IsNullOrWhiteSpace(emailAuthCode)) {
                var temp = new List<string>();
                if (string.IsNullOrWhiteSpace(smtpServerAddress)) {
                    temp.Add("SMTP服务器地址");
                }

                if (smtpServerPort == null) {
                    temp.Add("SMTP服务器端口");
                }

                if (string.IsNullOrWhiteSpace(emailAddress)) {
                    temp.Add("通知邮箱地址");
                }

                if (string.IsNullOrWhiteSpace(emailAuthCode)) {
                    temp.Add("SMTP服务授权码");
                }

                _logger.Warning("{Ex}未配置", string.Join("、", temp));
                return;
            }

            var apps = GetNotStartApps();
            var status = apps.Count == 0 ? "已全部启动" : $"{apps.Count}个应用未启动";
            var subject = $"📱 应用启动报告 - {DateTime.Now:yyyy-MM-dd HH:mm} [{status}]";
            var body = EmailTemplateHelper.GenerateAppNotificationHtml(MonitoredApps.ToList(), DaySeparatorOffset);

            _notificationService.PostEmail(subject, body, smtpServerAddress, smtpServerPort.Value, emailAddress,
                emailAddress, emailAuthCode);
        } catch (Exception e) {
            _logger.Error("发送邮件通知时发生错误{Ex}", e);
        }
    }

    private List<string> GetNotStartApps() {
        List<string> apps = [];
        apps.AddRange(from monitoredApp in MonitoredApps
            where !monitoredApp.HasStartToday
            select $"{monitoredApp.ProcessName}: {monitoredApp.DisplayTitle}");
        return apps;
    }

    [RelayCommand]
    private async Task RefreshAndCheck() {
        await CheckApp();
        UpdateData();
        SaveData();
    }

    [RelayCommand]
    private void AddMonitorWindow() {
        var dialog = App.GetService<PickWindowDialog>()!;
        dialog.Owner = App.Current.MainWindow;
        if (dialog.ShowDialog() == true) {
            var result = dialog.SelectedWindowResult;
            if (MonitoredApps.Any(x => x == result)) {
                MonitoredApps.First(info => info == result).UpdateInfo(result);
                return;
            }

            MonitoredApps.Add(dialog.SelectedWindowResult);
            _ = RefreshAndCheck();
            UpdateData();
            SaveData();
        }
    }

    [RelayCommand]
    private void OpenOperateMenu(WindowInfo windowInfo) {
        windowInfo.IsOperateMenuOpen = true;
    }

    [RelayCommand]
    private void SetRemarksTitle(WindowInfo windowInfo) {
        var dialog = App.GetService<GetTextsDialog>()!;
        dialog.Owner = App.Current.MainWindow;
        dialog.SetTitle($"设置 {windowInfo.ProcessName} 的备注名");
        dialog.SetContent("请输入备注名");
        dialog.SetTexts(() => [new GetStringInfo { Text = windowInfo.RemarksTitle, PlaceHolderAndName = "在此输入备注名" }]);
        if (dialog.ShowDialog() == true) {
            windowInfo.RemarksTitle = dialog.Result[0].Text;
            UpdateData();
            SaveData();
        }
    }

    [RelayCommand]
    private void HasStart(WindowInfo windowInfo) {
        windowInfo.SetStartToday(DateTime.Now, DaySeparatorOffset);
        UpdateData();
        SaveData();
    }

    [RelayCommand]
    private void HasNotStart(WindowInfo windowInfo) {
        windowInfo.SetNotStartToday(DateTime.Now, DaySeparatorOffset);
        UpdateData();
        SaveData();
    }

    [RelayCommand]
    private async Task CancelMonitor(WindowInfo windowInfo) {
        try {
            var confirmDialog = new MessageBox {
                Title = "是否确认取消监控？",
                Content = "删除后无法恢复，请确认操作",
                CloseButtonText = "取消",
                PrimaryButtonText = "确认"
            };
            var result = await confirmDialog.ShowDialogAsync();
            if (result != MessageBoxResult.Primary) {
                return;
            }

            MonitoredApps.Remove(windowInfo);
            UpdateData();
            SaveData();
        } catch (PreferencesException ex) {
            _logger.Error("保存时出现错误：{0}", ex.Message);
        } catch (Exception e) {
            _logger.Error("取消监控时出现错误：{0}", e.Message);
        }
    }

    [RelayCommand]
    private void SettingAppLaunchMonitor() {
        var dialog = App.GetService<SettingAppLaunchMonitorDialog>()!;
        dialog.Owner = App.Current.MainWindow;
        if (dialog.ShowDialog() == true) {
            var temp = _preferencesService.Get("DaySeparatorOffset",
                new TimeRange { Value = 0, Unit = TimeUnit.Hour })!;
            DaySeparatorOffset = temp.ToTimeSpan();
            _ = RefreshAndCheck();
            UpdateData();
            SaveData();
        }
    }
}