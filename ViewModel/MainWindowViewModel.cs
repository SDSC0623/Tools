// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Helpers;
using Tools.Services.IServices;
using Wpf.Ui.Appearance;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.ViewModel;

public partial class MainWindowViewModel : ObservableObject {
    public string Title => $"各种工具{(GlobalSettings.IsDebug ? " · Dev" : string.Empty)}";

    [ObservableProperty] private bool _isDark = true;

    // App运行辅助
    private readonly AppRunningHelper _appRunningHelper;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    public MainWindowViewModel(AppRunningHelper appRunningHelper, IPreferencesService preferencesService) {
        _appRunningHelper = appRunningHelper;
        _preferencesService = preferencesService;
        IsDark = _preferencesService.Get("ThemeIsDark", true);
    }

    [RelayCommand]
    private void Hide() {
        _appRunningHelper.Hide();
    }

    [RelayCommand]
    private void Show() {
        _appRunningHelper.Show();
    }

    [RelayCommand]
    private void Exit() {
        _appRunningHelper.ExitApp();
    }

    [RelayCommand]
    private void ExitInTray() {
        _appRunningHelper.ExitApp();
    }

    partial void OnIsDarkChanged(bool value) {
        ApplicationThemeManager.Apply(value ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }

    [RelayCommand]
    private void ChangeToDark() {
        IsDark = true;
        _ = _preferencesService.Set("ThemeIsDark", IsDark);
    }

    [RelayCommand]
    private void ChangeToLight() {
        IsDark = false;
        _ = _preferencesService.Set("ThemeIsDark", IsDark);
    }
}