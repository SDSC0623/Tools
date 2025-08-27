using System.Windows;
using Serilog;
using Tools.Views.Pages;
using Tools.Services;
using Tools.Services.IServices;
using Wpf.Ui;


namespace Tools.Helpers;

public class AppRunningHelper {
    // 导航服务
    private readonly INavigationService _navigationService;

    // Logger
    private readonly ILogger _logger;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    public AppRunningHelper(INavigationService navigationService, ILogger logger,
        IPreferencesService preferencesService, SnackbarServiceHelper snackbarService) {
        _navigationService = navigationService;
        _logger = logger;
        _preferencesService = preferencesService;
        _snackbarService = snackbarService;
        Init();
    }

    private void Init() {
        _logger.Information("程序启动完成，详细版本: [{FullVersion}]", GlobleSettings.FullVersion);
    }

    public void StartApp() {
        try {
            var targetPage = _preferencesService.Get("StartPage", typeof(HomePage))!;
            _navigationService.Navigate(targetPage);
        } catch (PreferencesException ex) {
            _navigationService.Navigate(typeof(HomePage));
            _preferencesService.Set("StartPage", typeof(HomePage));
            _snackbarService.ShowError("加载设定页面失败，已跳转到首页并重置本地化配置为首页", ex.Message);
        } catch (Exception ex) {
            _logger.Error("导航到设定启动页面时出错，错误：{Message}", ex.Message);
            _snackbarService.ShowError("导航到设定页面是时发生错误", ex.Message);
        }
    }

    public void Hide() {
        if (Application.Current.MainWindow!.Visibility != Visibility.Hidden) {
            Application.Current.MainWindow!.Hide();
        }
    }

    public void Show() {
        if (Application.Current.MainWindow!.Visibility != Visibility.Visible) {
            Application.Current.MainWindow.Activate();
            Application.Current.MainWindow.Focus();
            Application.Current.MainWindow.Show();
        }
    }

    public void ExitApp() {
        Application.Current.Shutdown();
    }
}