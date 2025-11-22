// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows;
using Tools.Converters;
using Tools.Helpers;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.ViewModel;
using Tools.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using Wpf.Ui.Tray.Controls;

namespace Tools.Views;

public partial class MainWindow : INavigationWindow {
    // App运行辅助
    private readonly AppRunningHelper _appRunningHelper;

    // 对话框服务
    private readonly IContentDialogService _contentDialogService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService,
        ISnackbarService snackbarService, AppRunningHelper appRunningHelper, IContentDialogService contentDialogService,
        SnackbarServiceHelper snackbarServiceHelper,
        IPreferencesService preferencesService) {
        InitializeComponent();
        DataContext = viewModel;

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        _snackbarService = snackbarServiceHelper;
        navigationService.SetNavigationControl(RootNavigation);

        Application.Current.MainWindow = this;

        _appRunningHelper = appRunningHelper;
        contentDialogService.SetDialogHost(RootContentDialog);
        _contentDialogService = contentDialogService;

        _preferencesService = preferencesService;
    }

    private void OnNotifyIconLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e) {
        _appRunningHelper.Show();
    }

    protected override void OnClosing(CancelEventArgs e) {
        try {
            e.Cancel = true;
            var mode = _preferencesService.Get("ExitMode", ExitMode.Ask);
            if (mode == ExitMode.Ask) {
                var dialog = App.GetService<AskBeforeExit>()!;
                dialog.Owner = Application.Current.MainWindow;
                mode = dialog.ShowDialog() == true ? dialog.ExitMode : ExitMode.Ask;
            }
            if (mode == ExitMode.Exit) {
                _appRunningHelper.ExitApp();
                base.OnClosing(e);
            } else if (mode == ExitMode.Hide) {
                _appRunningHelper.Hide();
            }
        } catch (Exception ex) {
            _snackbarService.ShowError("退出时发生错误", ex.Message);
        }
    }

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetServiceProvider(IServiceProvider serviceProvider) {
        throw new UnexpectedCallException();
    }

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
        RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();
}