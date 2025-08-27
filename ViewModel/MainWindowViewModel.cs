using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Helpers;

// ReSharper disable ConvertToPrimaryConstructor

namespace Tools.ViewModel;

public partial class MainWindowViewModel : ObservableObject {
    public string Title => $"各种工具{(GlobleSettings.IsDebug ? " · Dev" : string.Empty)}";

    // App运行辅助
    private readonly AppRunningHelper _appRunningHelper;

    public MainWindowViewModel(AppRunningHelper appRunningHelper) {
        _appRunningHelper = appRunningHelper;
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
}