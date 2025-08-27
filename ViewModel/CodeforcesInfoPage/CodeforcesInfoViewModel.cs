using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Pages.CodeforcesInfo;

namespace Tools.ViewModel.CodeforcesInfoPage;

internal class NotSetUsernameException() : Exception("未设置用户名");

internal class NotSetApiKeyException() : Exception("未设置ApiKey");

internal class NotSetApiSecretException() : Exception("未设置ApiSecret");

public partial class CodeforcesInfoViewModel : ObservableObject {
    // 比赛列表
    public string ContestListTitle => $"Codeforces比赛列表(包含过去 {MaxLengthToLoad})";

    [ObservableProperty] private ObservableCollection<ContestModel> _contests = [];

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ContestListTitle))]
    private TimeRange _maxLengthToLoad = new() { Value = 30, Unit = TimeUnit.Day };

    [ObservableProperty] private bool _isContestsLoading;
    [ObservableProperty] private bool _hasContestsError;
    [ObservableProperty] private string _contestsErrorMessage = string.Empty;
    [ObservableProperty] private bool _finishedContestCodeLoading;

    // 用户信息
    public string UserInfoTitle => $"{UserInfo.Handle} 信息";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(UserInfoTitle))]
    private UserModel _userInfo = new();

    [ObservableProperty] private bool _isUserInfoLoading;
    [ObservableProperty] private string _userInfoLoadingMessage = string.Empty;
    [ObservableProperty] private bool _hasUserInfoError;
    [ObservableProperty] private string _userInfoErrorMessage = string.Empty;
    [ObservableProperty] private bool _finishedUserInfoLoading;


    // 评分变化
    [ObservableProperty] private ObservableCollection<RatingChangeModel> _visibleChanges = [];
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private int _showCount;
    [ObservableProperty] private int _maxShowCount;
    private ObservableCollection<RatingChangeModel> _allChanges = [];


    // 好友信息
    public string FriendsListTitle => $"好友列表({Friends.Count})";

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(FriendsListTitle))]
    private ObservableCollection<FriendModel> _friends = [];

    [ObservableProperty] private bool _isFriendsLoading;
    [ObservableProperty] private string _friendsLoadingMessage = string.Empty;
    [ObservableProperty] private bool _hasFriendsError;
    [ObservableProperty] private string _friendsErrorMessage = string.Empty;
    [ObservableProperty] private bool _hasFriends;
    [ObservableProperty] private bool _finishedFriendsLoading;


    // 倒计时更新定时器
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render) {
        Interval = TimeSpan.FromSeconds(1)
    };

    // 错误信息汇总
    private string _errorMessage = string.Empty;

    // Api服务
    private readonly ICodeforcesApiService _apiService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // Logger
    private readonly ILogger _logger;

    public CodeforcesInfoViewModel(ICodeforcesApiService apiService, IPreferencesService preferencesService,
        SnackbarServiceHelper snackbarService, ILogger logger) {
        _apiService = apiService;
        _preferencesService = preferencesService;
        _snackbarService = snackbarService;
        _logger = logger;
        LoadInitialDataAsync();
        StartTime();
    }

    private void LoadInitialDataAsync() {
        _ = RefreshContestList();
        _ = RefreshUserInfoAndFriends();
        _ = RefreshUserRatingChanges();
        _ = RefreshFriends();
    }

    private void AddErrorMessage(string errorMessage) {
        _errorMessage += errorMessage + "\n";
    }

    private void UpdateUserInfoLoadingMessage(string message) {
        UserInfoLoadingMessage = message;
    }


    private void StartTime() {
        _timer.Tick += (_, _) => { UpdateTime(); };
        _timer.Start();
        UpdateTime();
    }

    private static void UpdateTime() {
        WeakReferenceMessenger.Default.Send(new TimerTickMessage("MainTimer"));
    }

    [RelayCommand]
    private async Task RefreshContestList() {
        IsContestsLoading = true;
        HasContestsError = false;
        FinishedContestCodeLoading = false;
        _errorMessage = string.Empty;
        ContestsErrorMessage = string.Empty;
        try {
            MaxLengthToLoad = _preferencesService.Get("ContestsLoadTimeRange",
                new TimeRange { Value = 30, Unit = TimeUnit.Day })!;
            var contests = await _apiService.GetContestsAsync(MaxLengthToLoad.ToTimeSpan());
            Contests.Clear();

            var dispatcher = Application.Current.Dispatcher;

            // 分批更新 UI
            const int batchSize = 8; // 每批添加的项目数
            var totalItems = contests.Count;

            for (var i = 0; i < totalItems; i += batchSize) {
                var count = Math.Min(batchSize, totalItems - i);

                await dispatcher.InvokeAsync(() => {
                    for (var j = i; j < i + count; j++) {
                        Contests.Add(contests[j]);
                    }
                });

                // 让 UI 有时间渲染
                await Task.Delay(50);

                if (i >= batchSize) {
                    IsContestsLoading = false;
                }
            }

            UpdateTime();
        } catch (Exception ex) {
            HasContestsError = true;
            ContestsErrorMessage = ex.Message;
            AddErrorMessage(ex.Message);
            _logger.Error("刷新比赛列表错误，错误: {Message}", ex.Message);
            _snackbarService.ShowError("刷新比赛列表错误", _errorMessage);
        } finally {
            IsContestsLoading = false;
            FinishedContestCodeLoading = true;
        }
    }

    [RelayCommand]
    private async Task RefreshUserInfoAndFriends() {
        IsUserInfoLoading = true;
        UserInfoLoadingMessage = string.Empty;
        HasUserInfoError = false;
        _errorMessage = string.Empty;
        UserInfoErrorMessage = string.Empty;
        FinishedUserInfoLoading = false;
        try {
            var username = _preferencesService.Get<string>("Username");
            if (string.IsNullOrWhiteSpace(username)) {
                throw new NotSetUsernameException();
            }

            UpdateUserInfoLoadingMessage($"正在获取用户: {username} 信息...");
            UserInfo = await _apiService.GetUserInfoAsync(username);
            UpdateUserInfoLoadingMessage($"获取用户: {username} 信息成功！");

            await Task.Delay(300);

            UpdateUserInfoLoadingMessage($"开始获取用户: {username} Rating 变化信息...");
            await RefreshUserRatingChanges();
            UpdateUserInfoLoadingMessage($"获取用户: {username} Rating 变化信息成功！");

            await Task.Delay(300);
        } catch (Exception ex) {
            HasUserInfoError = true;
            UserInfoErrorMessage = ex.Message;
            AddErrorMessage(ex.Message);
            _logger.Error("刷新用户信息错误，错误: {Message}", ex.Message);
            _snackbarService.ShowError("刷新用户信息错误", _errorMessage);
        } finally {
            IsUserInfoLoading = false;
            FinishedUserInfoLoading = true;
        }
    }

    private async Task RefreshUserRatingChanges() {
        try {
            var username = _preferencesService.Get<string>("Username");
            if (string.IsNullOrWhiteSpace(username)) {
                throw new NotSetUsernameException();
            }

            _allChanges = await _apiService.GetUserRatingChangesAsync(username);
            MaxShowCount = _allChanges.Count;

            RefreshShowCount();
        } catch (Exception e) {
            AddErrorMessage(e.Message);
            _logger.Error("获取用户Rating变化错误，错误: {Message}", e.Message);
            _snackbarService.ShowError("获取用户Rating变化错误", e.Message);
        }
    }

    [RelayCommand]
    private async Task RefreshFriends() {
        HasFriendsError = false;
        IsFriendsLoading = true;
        FinishedFriendsLoading = false;
        _errorMessage = string.Empty;
        FriendsErrorMessage = string.Empty;
        FriendsLoadingMessage = string.Empty;
        try {
            var apiKey = _preferencesService.Get<string>("ApiKey");
            var apiSecret = _preferencesService.Get<string>("ApiSecret");
            if (string.IsNullOrWhiteSpace(apiKey)) {
                throw new NotSetApiKeyException();
            }

            if (string.IsNullOrWhiteSpace(apiSecret)) {
                throw new NotSetApiSecretException();
            }

            Friends = await _apiService.GetFriendsAsync(apiKey, apiSecret,
                message => { FriendsLoadingMessage = message; });

            HasFriends = Friends.Count > 0;
        } catch (Exception e) {
            HasFriendsError = true;
            FriendsErrorMessage = e.Message;
            AddErrorMessage(e.Message);
            _logger.Error("获取好友列表错误，错误: {Message}", e.Message);
            _snackbarService.ShowError("获取好友列表错误", _errorMessage);
        } finally {
            IsFriendsLoading = false;
            FinishedFriendsLoading = true;
        }
    }

    partial void OnShowCountChanged(int value) {
        _ = _preferencesService.Set("ShowCount", value);
        RefreshShowCount();
    }

    private void RefreshShowCount() {
        var tempMaxShowCount = _preferencesService.Get<int>("ShowCount");
        ShowCount = tempMaxShowCount;
        HasData = ShowCount > 0;
        if (ShowCount > MaxShowCount) {
            ShowCount = MaxShowCount;
        }

        var visibleChanges = new ObservableCollection<RatingChangeModel>();
        for (var i = 0; i < ShowCount; i++) {
            visibleChanges.Add(_allChanges[i]);
        }

        VisibleChanges = visibleChanges;
    }

    [RelayCommand]
    private void SettingContestsLoadTimeRange() {
        var dialog = App.GetService<ContestsLoadSettingDialog>()!;
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true) {
            _ = RefreshContestList();
        }
    }

    [RelayCommand]
    private void SettingUserInfo() {
        var oldUsername = _preferencesService.Get<string>("Username");
        var oldApiKey = _preferencesService.Get<string>("ApiKey");
        var oldApiSecret = _preferencesService.Get<string>("ApiSecret");

        var dialog = App.GetService<UserInfoSettingDialog>()!;
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true) {
            var newUsername = _preferencesService.Get<string>("Username");
            var newApiKey = _preferencesService.Get<string>("ApiKey");
            var newApiSecret = _preferencesService.Get<string>("ApiSecret");
            if (newUsername != oldUsername) {
                _ = RefreshUserInfoAndFriends();
                _ = RefreshUserRatingChanges();
            }

            if (newApiKey != oldApiKey || newApiSecret != oldApiSecret) {
                _ = RefreshFriends();
            }
        }
    }
}