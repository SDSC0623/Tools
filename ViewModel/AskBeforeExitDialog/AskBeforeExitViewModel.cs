// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Helpers;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Windows;

namespace Tools.ViewModel.AskBeforeExitDialog;

public partial class AskBeforeExitViewModel : ObservableObject {
    [ObservableProperty] private bool _doNotAskAgain;

    // 窗口对象
    private AskBeforeExit? _window;

    // App运行辅助
    private readonly AppRunningHelper _appRunningHelper;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public AskBeforeExitViewModel(AppRunningHelper appRunningHelper, SnackbarServiceHelper snackbarService,
        IPreferencesService preferencesService) {
        _appRunningHelper = appRunningHelper;
        _snackbarService = snackbarService;
        _preferencesService = preferencesService;
    }

    public void SetWindow(AskBeforeExit window) {
        _window = window;
    }

    private void CloseDialog(bool result) {
        if (_window == null) {
            return;
        }

        _window.DialogResult = result;
        _window.Close();
    }

    [RelayCommand]
    private void Exit() {
        try {
            _preferencesService.Set("ExitMode", DoNotAskAgain ? ExitMode.Exit : ExitMode.Ask);
            _window!.ExitMode = ExitMode.Exit;
        } catch (Exception e) {
            _snackbarService.ShowError("保存失败", e.Message);
        }

        CloseDialog(true);
    }

    [RelayCommand]
    private void Hide() {
        try {
            _preferencesService.Set("ExitMode", DoNotAskAgain ? ExitMode.Hide : ExitMode.Ask);
            _window!.ExitMode = ExitMode.Hide;
        } catch (Exception e) {
            _snackbarService.ShowError("保存失败", e.Message);
        }

        CloseDialog(true);
    }

    [RelayCommand]
    private void Cancel() {
        CloseDialog(false);
    }
}