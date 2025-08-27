using System.Windows;
using Tools.Helpers;
using Tools.ViewModel;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;

namespace Tools.Views;

public partial class MainWindow : INavigationWindow {
    // App运行辅助
    private readonly AppRunningHelper _appRunningHelper;

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService,
        ISnackbarService snackbarService, AppRunningHelper appRunningHelper) {
        InitializeComponent();
        DataContext = viewModel;

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(RootNavigation);

        Application.Current.MainWindow = this;

        _appRunningHelper = appRunningHelper;
    }

    private void OnNotifyIconLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e) {
        _appRunningHelper.Show();
    }

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetServiceProvider(IServiceProvider serviceProvider) {
        throw new NotImplementedException();
    }

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
        RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();
}