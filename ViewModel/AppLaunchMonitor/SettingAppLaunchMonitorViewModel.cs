// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Pages.AppLaunchMonitor;

namespace Tools.ViewModel.AppLaunchMonitor;

public partial class SettingAppLaunchMonitorViewModel : ObservableValidator {
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Range))]
    [Required]
    [Range(0.0, 1000000.0, ErrorMessage = "输入不合法，请输入 0 - 1000000 之内的实数")]
    private double _value;

    // 时间单位部分
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Range))] [Required]
    private TimeUnit _unit;

    // 取用对象
    public TimeRange Range => new() { Value = Value, Unit = Unit };

    // 是否启用邮件通知
    [ObservableProperty] private bool _isEmailNotificationEnabled;

    // 邮件通知地址
    [ObservableProperty] private string _notifyEmailAddress = string.Empty;

    // 邮件授权码
    [ObservableProperty] private string _emailAuthCode = string.Empty;

    // 是否启用Windows通知
    [ObservableProperty] private bool _isWindowsNotificationEnabled;

    // fps
    [ObservableProperty] [Required] [Range(10.0, 60.0, ErrorMessage = "输入不合法，请输入 10 - 60 之内的实数")]
    private double _fps;

    // 窗口对象
    private SettingAppLaunchMonitorDialog? _window;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public SettingAppLaunchMonitorViewModel(IPreferencesService preferencesService,
        SnackbarServiceHelper snackbarService) {
        _preferencesService = preferencesService;
        _snackbarService = snackbarService;
        Init();
    }

    private void Init() {
        var temp = _preferencesService.Get("DaySeparatorOffset", new TimeRange { Value = 0, Unit = TimeUnit.Hour })!;
        Value = temp.Value;
        Unit = temp.Unit;
        Fps = _preferencesService.Get("ShowWindowFps", 30.0);
        IsEmailNotificationEnabled = _preferencesService.Get("NeedEmailNotification", false);
        NotifyEmailAddress = _preferencesService.Get("EmailNotificationAddress", string.Empty)!;
        EmailAuthCode = _preferencesService.Get("EmailNotificationAuthCode", string.Empty)!;
        IsWindowsNotificationEnabled = _preferencesService.Get("NeedWindowsToastNotification", false);
    }

    public void SetWindow(SettingAppLaunchMonitorDialog window) {
        _window = window;
    }

    // 实时校验
    partial void OnValueChanged(double value) {
        ValidateProperty(value, nameof(Value));
    }

    partial void OnFpsChanged(double value) {
        ValidateProperty(value, nameof(Fps));
    }

    private void CloseWindow(bool result) {
        if (_window is null) {
            throw new Exception("窗口绑定异常");
        }

        _window.DialogResult = result;
        _window.Close();
    }

    [RelayCommand]
    private void Reset() {
        Value = 0;
        Unit = TimeUnit.Hour;
        Fps = 30;
        IsEmailNotificationEnabled = false;
        IsWindowsNotificationEnabled = false;
    }

    [RelayCommand]
    private void Cancel() {
        CloseWindow(false);
    }

    [RelayCommand]
    private void Save() {
        ValidateAllProperties();
        if (HasErrors) {
            _snackbarService.ShowWarning("不可保存", string.Join(Environment.NewLine, GetErrors(nameof(Value))));
            return;
        }

        _preferencesService.Set("DaySeparatorOffset", Range);
        _preferencesService.Set("NeedEmailNotification", IsEmailNotificationEnabled);
        _preferencesService.Set("EmailNotificationAddress", NotifyEmailAddress);
        _preferencesService.Set("EmailNotificationAuthCode", EmailAuthCode);
        _preferencesService.Set("NeedWindowsToastNotification", IsWindowsNotificationEnabled);
        _preferencesService.Set("ShowWindowFps", Fps);
        _snackbarService.ShowSuccess("保存成功", $"设置偏移时间为{Range}, 预览帧率{Fps} 等");
        CloseWindow(true);
    }
}