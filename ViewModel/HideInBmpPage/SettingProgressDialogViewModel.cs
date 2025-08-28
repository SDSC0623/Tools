// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Helpers;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Pages.HideInBmpDialog;

namespace Tools.ViewModel.HideInBmpPage;

public partial class SettingProgressDialogViewModel : ObservableObject {
    [ObservableProperty] private ShowProgressMode _mode = ShowProgressMode.Percent;

    // 窗口对象
    private SettingProgressDialog? _window;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public SettingProgressDialogViewModel(SnackbarServiceHelper snackbarService,
        IPreferencesService preferencesService) {
        _snackbarService = snackbarService;
        _preferencesService = preferencesService;
        Init();
    }

    public void SetWindow(SettingProgressDialog window) {
        _window = window;
    }

    private void Init() {
        var mode = _preferencesService.Get("ShowProgressMode", ShowProgressMode.Percent);
        Mode = mode;
    }

    private void CloseDialog(bool result) {
        if (_window == null) {
            return;
        }

        _window.DialogResult = result;
        _window.Close();
    }

    [RelayCommand]
    private void Save() {
        _preferencesService.Set("ShowProgressMode", Mode);
        _snackbarService.ShowSuccess("保存成功", $"设置显示模式为{CommonHelper.GetEnumDescription(Mode)}");
        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() {
        CloseDialog(false);
    }
}