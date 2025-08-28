// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Pages.CodeforcesInfo;

namespace Tools.ViewModel.CodeforcesInfoPage;

public partial class UserInfoSettingDialogViewModel : ObservableObject {
    // 用户名
    [ObservableProperty] private string _username = string.Empty;

    // ApiKey
    [ObservableProperty] private string _apiKey = string.Empty;

    // ApiSecret
    [ObservableProperty] private string _apiSecret = string.Empty;

    // 是否开放编辑
    [ObservableProperty] private bool _isEditing;

    // 窗口对象
    private UserInfoSettingDialog? _window;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息
    private readonly SnackbarServiceHelper _snackbarService;

    public UserInfoSettingDialogViewModel(IPreferencesService preferencesService,
        SnackbarServiceHelper snackbarService) {
        _preferencesService = preferencesService;
        _snackbarService = snackbarService;
        Init();
    }

    public void SetWindow(UserInfoSettingDialog window) {
        _window = window;
    }

    private void Init() {
        var temp = _preferencesService.Get<string>("Username");
        if (temp != null) {
            Username = temp;
        }

        temp = _preferencesService.Get<string>("ApiKey");
        if (temp != null) {
            ApiKey = temp;
        }

        temp = _preferencesService.Get<string>("ApiSecret");
        if (temp != null) {
            ApiSecret = temp;
        }
    }

    [RelayCommand]
    private void StartEdit() {
        IsEditing = true;
    }

    [RelayCommand]
    private void EndEdit() {
        IsEditing = false;
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
        EndEdit();
        await _preferencesService.Set<string>("Username", Username);
        await _preferencesService.Set<string>("ApiKey", ApiKey);
        await _preferencesService.Set<string>("ApiSecret", ApiSecret);
        _snackbarService.ShowSuccess("保存成功", $"设置用户名为{Username}\n设置ApiKey为{ApiKey}\n设置ApiSecret为{ApiSecret}");
        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() {
        EndEdit();
        _snackbarService.ShowInfo("取消保存", "已取消保存");
        CloseDialog(false);
    }
}