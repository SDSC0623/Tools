// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Pages.CodeforcesInfo;

namespace Tools.ViewModel.CodeforcesInfoPage;

public partial class ContestsLoadSettingDialogViewModel : ObservableValidator {
    // 时间数字部分
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Range))]
    [Required]
    [Range(0.0, 1000000.0, ErrorMessage = "输入不合法，请输入 0 - 1000000 之内的实数")]
    private string _value = string.Empty;

    // 时间单位部分
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Range))] [Required]
    private TimeUnit _unit;

    // 取用对象
    public TimeRange Range => new() { Value = double.Parse((string)Value), Unit = Unit };

    // 窗口对象
    private ContestsLoadSettingDialog? _window;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public ContestsLoadSettingDialogViewModel(IPreferencesService preferencesService,
        SnackbarServiceHelper snackbarService) {
        _preferencesService = preferencesService;
        _snackbarService = snackbarService;
        Init();
    }

    public void SetWindow(ContestsLoadSettingDialog window) {
        _window = window;
    }

    private void Init() {
        var range = _preferencesService.Get("ContestsLoadTimeRange",
            new TimeRange { Value = 30, Unit = TimeUnit.Day })!;
        Value = range.Value + "";
        Unit = range.Unit;
    }

    // 实时校验
    partial void OnValueChanged(string value) {
        ValidateProperty(value, nameof(Value));
    }

    [RelayCommand]
    private void Reset() {
        Value = "30";
        Unit = TimeUnit.Day;
    }

    private void CloseDialog(bool result) {
        if (_window == null) {
            return;
        }

        _window.DialogResult = result;
        _window.Close();
    }

    [RelayCommand]
    private async Task Save() {
        ValidateAllProperties();
        if (HasErrors) {
            _snackbarService.ShowWarning("不可保存", string.Join(Environment.NewLine, GetErrors(nameof(Value))));
            return;
        }

        await _preferencesService.Set("ContestsLoadTimeRange", Range);
        _snackbarService.ShowSuccess("保存成功", $"设置展示时间为{Range}");
        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() {
        _snackbarService.ShowInfo("取消保存", "已取消保存");
        CloseDialog(false);
    }
}