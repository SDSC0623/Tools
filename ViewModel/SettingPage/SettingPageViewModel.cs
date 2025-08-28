using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Tools.Attributes;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Windows;

// ReSharper disable NotAccessedField.Local

namespace Tools.ViewModel.SettingPage;

public partial class SettingPageViewModel : ObservableObject {
    // 选中的启动Page
    [ObservableProperty] private Type _startPageType = typeof(Views.Pages.HomePage);

    // 启动Page列表
    public ObservableCollection<PageInfo> AvailblePages { get; }

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // Logger
    private readonly ILogger _logger;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    public SettingPageViewModel(SnackbarServiceHelper snackbarService, ILogger logger,
        IPreferencesService preferencesService) {
        _snackbarService = snackbarService;
        _logger = logger;
        _preferencesService = preferencesService;
        AvailblePages = GetAvailblePages();
        Init();
    }

    private void Init() {
        StartPageType = _preferencesService.Get("StartPage", typeof(Views.Pages.HomePage))!;
    }

    partial void OnStartPageTypeChanged(Type value) {
        _preferencesService.Set("StartPage", value);
    }

    private ObservableCollection<PageInfo> GetAvailblePages() {
        var availblePages = new ObservableCollection<PageInfo>();

        var assembly = Assembly.GetExecutingAssembly();

        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Page)))
            .Where(t => t.GetCustomAttribute<AvailbleStartPageAttribute>() != null)
            .OrderBy(t => t.GetCustomAttribute<AvailbleStartPageAttribute>()!.SortWeight)
            .ThenBy(t => t.Name);

        foreach (var pageType in pageTypes) {
            var displayName = GetDisplayNameByDescriptionAttribute(pageType);
            availblePages.Add(new PageInfo {
                DisplayName = displayName,
                PageType = pageType
            });
        }

        return availblePages;
    }

    private string GetDisplayNameByDescriptionAttribute(Type pageType) {
        var descriptionAttribute = pageType.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttribute?.Description ?? pageType.Name;
    }

    [RelayCommand]
    private void OpenAboutWindow() {
        var aboutWindow = App.GetService<AboutWindow>()!;
        aboutWindow.Owner = App.Current.MainWindow;
        aboutWindow.ShowDialog();
    }
}