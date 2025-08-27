using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Tools.Helpers;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views;
using Tools.Views.Pages;
using Tools.Views.Pages.CodeforcesInfo;
using Tools.ViewModel;
using Tools.ViewModel.HomePage;
using Tools.ViewModel.SettingPage;
using Tools.ViewModel.CodeforcesInfoPage;
using Tools.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantExtendsListEntry

namespace Tools;

public partial class App : Application {
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureLogging(logging => { logging.ClearProviders(); })
        .ConfigureServices((_, services) => {
            // 日志
            var logFile = Path.Combine(GlobleSettings.LogDirectory, "log.txt");
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.File(logFile,
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} {SourceContext} {Exception}{NewLine}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Exception}{NewLine}{NewLine}")
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning);


            Log.Logger = loggerConfiguration.CreateLogger();
            services.AddLogging(c => c.AddSerilog());
            services.AddSingleton(Log.Logger);

            // 导航服务提供器
            services.AddNavigationViewPageProvider();

            // 服务
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ICodeforcesApiService, CodeforcesApiService>();
            services.AddSingleton<IPreferencesService, JsonPreferencesService>();

            // 特殊服务
            services.AddSingleton<SnackbarServiceHelper>(); // 弹窗服务
            services.AddSingleton<AppRunningHelper>(); // App运行操作服务

            // 窗口
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddTransient<AboutWindow>();

            // 页面
            services.AddSingleton<CodeforcesInfoViewModel>();
            services.AddSingleton<CodeforcesInfoPage>();
            services.AddSingleton<HomePageViewModel>();
            services.AddSingleton<HomePage>();
            services.AddSingleton<SettingPageViewModel>();
            services.AddSingleton<SettingPage>();
            // 问询对话框
            services.AddTransient<UserInfoSettingDialogViewModel>();
            services.AddTransient<UserInfoSettingDialog>();
            services.AddTransient<ContestsLoadSettingDialogViewModel>();
            services.AddTransient<ContestsLoadSettingDialog>();
        })
        .Build();

    private static IServiceProvider ServiceProvider => Host.Services;

    private Serilog.ILogger _logger = Log.Logger;

    public new static App Current => (App)Application.Current;

    protected override async void OnStartup(StartupEventArgs e) {
        try {
            base.OnStartup(e);
            await Host.StartAsync();

            var mainWindow = GetService<MainWindow>()!;
            mainWindow.Show();

            _logger = GetService<Serilog.ILogger>()!;
            GetService<AppRunningHelper>()!.StartApp();
        } catch (Exception ex) {
            _logger.Error("启动时发生错误: {ExMessage}", ex.Message);
        }
    }

    protected override async void OnExit(ExitEventArgs e) {
        try {
            base.OnExit(e);

            await Host.StopAsync();

            Host.Dispose();
        } catch (Exception ex) {
            _logger.Error("退出时发生错误: {ExMessage}", ex.Message);
        }
    }

    public static T? GetService<T>() where T : class {
        return ServiceProvider.GetService(typeof(T)) as T;
    }

    public static object? GetService(Type type) {
        return ServiceProvider.GetService(type);
    }
}