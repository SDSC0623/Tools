// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Tools.Models;
using Tools.Services.IServices;
using Tools.Views.Pages.AppLaunchMonitor;

namespace Tools.ViewModel.AppLaunchMonitor;

public partial class PickWindowDialogViewModel : ObservableObject {
    private ObservableCollection<WindowInfo> _allWindows = [];

    [ObservableProperty] private ObservableCollection<WindowInfo> _allDisplayWindows = [];

    [ObservableProperty] private bool _hasWindows;

    [ObservableProperty] private bool _isLoadingWindows;

    [ObservableProperty] private WindowInfo _selectedWindow = null!;

    [ObservableProperty] private ImageSource _selectedWindowScreenshot = null!;

    [ObservableProperty] private bool _hasSelectedWindow;

    [ObservableProperty] private bool _isCapturingScreenshot;

    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private bool _isSearchVisible;

    [ObservableProperty] private string _searchText = string.Empty;

    private double _fps;

    // 窗口对象
    private PickWindowDialog? _window;

    // App运行辅助
    private readonly ILogger _logger;

    // 窗口获取服务
    private readonly IWindowsPickerService _windowsPickerService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    public PickWindowDialogViewModel(IWindowsPickerService windowsPickerService, ILogger logger,
        IPreferencesService preferencesService) {
        _windowsPickerService = windowsPickerService;
        _logger = logger;
        _preferencesService = preferencesService;
        Init();
    }

    private void Init() {
        Refresh();
        _fps = _preferencesService.Get("ShowWindowFps", 30.0);
    }

    public void SetWindow(PickWindowDialog window) {
        _window = window;
    }

    partial void OnAllDisplayWindowsChanged(ObservableCollection<WindowInfo> value) {
        HasWindows = value.Count > 0;
    }

    private void CloseDialog(bool result) {
        _windowsPickerService.StopShowWindow();
        if (_window == null) {
            _logger.Error("窗口绑定异常");
            throw new Exception("窗口绑定异常");
        }

        _window.DialogResult = result;
        _window.Close();
    }

    private async Task RefreshWindows() {
        IsLoadingWindows = true;
        HasWindows = true;
        HasSelectedWindow = false;
        _windowsPickerService.StopShowWindow();
        _allWindows = await _windowsPickerService.GetAllWindowsAsync();
        ClearSearch();
        AllDisplayWindows = _allWindows;
        IsLoadingWindows = false;
    }

    [RelayCommand]
    private void Cancel() {
        CloseDialog(false);
    }

    private void ConfirmSelected(WindowInfo? info) {
        if (_window == null) {
            _logger.Error("窗口绑定异常");
            throw new Exception("窗口绑定异常");
        }

        _window.SelectedWindowResult = info ?? SelectedWindow;
        CloseDialog(true);
    }

    [RelayCommand]
    private void Confirm(WindowInfo? info) {
        ConfirmSelected(info);
    }

    [RelayCommand]
    private void ToggleSearch() {
        IsSearchVisible = !IsSearchVisible;
    }

    [RelayCommand]
    private void ClearSearch() {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void Refresh() {
        _ = RefreshWindows();
    }

    [RelayCommand]
    private void SelectWindow(WindowInfo info) {
        SelectedWindow = info;
        HasSelectedWindow = true;
    }

    partial void OnSelectedWindowChanged(WindowInfo value) {
        SelectedWindowScreenshot = null!;
        _windowsPickerService.StartShowWindow(value, source => { SelectedWindowScreenshot = source; }, _fps);
    }

    partial void OnSearchTextChanged(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            AllDisplayWindows = _allWindows;
        } else {
            var temp = _allWindows.Where(x => x.Title.Contains(value) || x.ProcessName.Contains(value));
            AllDisplayWindows = new ObservableCollection<WindowInfo>(temp);
        }
    }
}