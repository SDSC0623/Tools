// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.RichTextBox.Abstraction;
using Tools.Helpers;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views;
using Tools.Views.Pages;
using Tools.Views.Pages.CodeforcesInfo;
using Tools.Views.Pages.HideInBmp;
using Tools.ViewModel;
using Tools.ViewModel.AskBeforeExitDialog;
using Tools.ViewModel.Base64ToolPage;
using Tools.ViewModel.HomePage;
using Tools.ViewModel.SettingPage;
using Tools.ViewModel.CodeforcesInfoPage;
using Tools.ViewModel.DataStructureDisplay.GraphVisualization;
using Tools.ViewModel.DataStructureDisplay.TreeVisualization;
using Tools.ViewModel.HideInBmpPage;
using Tools.Views.Pages.Base64Tool;
using Tools.Views.Pages.DataStructureDisplay.GraphVisualization;
using Tools.Views.Pages.DataStructureDisplay.TreeVisualization;
using Tools.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantExtendsListEntry

namespace Tools;

public partial class App : Application {
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureLogging(logging => { logging.ClearProviders(); })
        .ConfigureServices((_, services) => {
            // 日志
            var logFile = Path.Combine(GlobalSettings.LogDirectory, "log.txt");
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.File(logFile,
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message} {SourceContext} {Exception}{NewLine}{NewLine}",
                    rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning);
#if DEBUG
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Exception}{NewLine}{NewLine}");
#endif
            var richTextBox = new RichTextBoxImpl();
            services.AddSingleton<IRichTextBox>(richTextBox);

            loggerConfiguration.WriteTo.RichTextBox(richTextBox, LogEventLevel.Information);
            // OutputTemplate "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"

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
            services.AddSingleton<IBmpSteganographyService, BmpSteganographyService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();
            services.AddSingleton<INotificationService, WindowsToastNotificationService>();

            // 特殊服务
            services.AddSingleton<SnackbarServiceHelper>(); // 弹窗服务
            services.AddSingleton<AppRunningHelper>(); // App运行操作服务

            // 窗口
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddTransient<AboutWindow>();

            // 页面
            // Codeforces信息查看
            services.AddSingleton<CodeforcesInfoViewModel>();
            services.AddSingleton<CodeforcesInfoPage>();
            // 主页
            services.AddSingleton<HomePageViewModel>();
            services.AddSingleton<HomePage>();
            // 设置页面
            services.AddSingleton<SettingPageViewModel>();
            services.AddSingleton<SettingPage>();
            // Bmp隐写
            services.AddSingleton<HideInBmpViewModel>();
            services.AddSingleton<HideInBmpPage>();
            // Base64编解码工具
            services.AddSingleton<Base64ToolViewModel>();
            services.AddSingleton<Base64ToolPage>();
            // 数据结构可视化
            // 图
            services.AddSingleton<GraphVisualizationViewModel>();
            services.AddSingleton<GraphVisualizationPage>();
            // 树
            services.AddSingleton<TreeVisualizationViewModel>();
            services.AddSingleton<TreeVisualizationPage>();
            // 问询对话框
            services.AddTransient<UserInfoSettingDialogViewModel>();
            services.AddTransient<UserInfoSettingDialog>();
            services.AddTransient<ContestsLoadSettingDialogViewModel>();
            services.AddTransient<ContestsLoadSettingDialog>();
            services.AddTransient<SettingProgressDialogViewModel>();
            services.AddTransient<SettingProgressDialog>();
            services.AddTransient<AskBeforeExitViewModel>();
            services.AddTransient<AskBeforeExit>();
        })
        .Build();

    private static IServiceProvider ServiceProvider => Host.Services;

    private Serilog.ILogger _logger = Log.Logger;

    public new static App Current => (App)Application.Current;

    protected override async void OnStartup(StartupEventArgs e) {
        try {
            base.OnStartup(e);
            if (GlobalSettings.BaseDirectory.Split(Path.DirectorySeparatorChar).Length == 1 ||
                new DirectoryInfo(GlobalSettings.BaseDirectory).Parent == null) {
                var messageBox = new Wpf.Ui.Controls.MessageBox {
                    Title = "程序主动停止",
                    Content = "请不要在根目录下运行程序，你应当把本程序放在一个文件夹内运行。",
                    CloseButtonText = "确认"
                };
                await messageBox.ShowDialogAsync();
                Environment.Exit(-1);
                return;
            }

            await Host.StartAsync();

            _logger = GetService<Serilog.ILogger>()!;

            var mainWindow = GetService<MainWindow>()!;
            mainWindow.Show();

            GetService<INavigationService>()!.Navigate(typeof(SettingPage));

            GetService<AppRunningHelper>()!.StartApp();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
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