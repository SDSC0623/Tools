// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Tools.Models;
using Tools.Services;
using Tools.Services.IServices;
using Tools.Views.Pages.HideInBmp;

namespace Tools.ViewModel.HideInBmpPage;

public partial class HideInBmpViewModel : ObservableObject {
    public static string HideInBmpPageTitle => "Bmp图片隐写";

    // Bmp图片
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartHideCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartExtractCommand))]
    private string _bmpPath = string.Empty; //Bmp图片路径

    [ObservableProperty] private long _bmpSize; //Bmp图片大小


    // 隐藏文件
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartHideCommand))]
    private string _fileToHidePath = string.Empty; //隐藏文件路径

    [ObservableProperty] private long _fileToHideSize; //隐藏图片大小
    [ObservableProperty] private long _maxHideSize; //最大隐藏图片大小

    // 输出文件夹
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartHideCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartExtractCommand))]
    private string _outputFolderPath = string.Empty; //输出文件夹路径

    // 输出图片路径(预览)
    [ObservableProperty] private string _outputBmpPath = string.Empty;

    // 百分比模式隐写进度
    [ObservableProperty] private double _hideOrVerifyOrExtractProgress;

    // 文字模式隐写进度
    [ObservableProperty] private string _hideOrVerifyOrExtractText = "尚未进行操作";

    // 不在执行操作
    [ObservableProperty] private bool _notExecuting = true;

    // 进度模式
    [ObservableProperty] private bool _isPercentMode = true;
    [ObservableProperty] private bool _isTextMode;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // Bmp图片隐写服务
    private readonly IBmpSteganographyService _bmpSteganographyService;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    public HideInBmpViewModel(SnackbarServiceHelper snackbarService, IBmpSteganographyService bmpSteganographyService,
        IPreferencesService preferencesService) {
        _snackbarService = snackbarService;
        _bmpSteganographyService = bmpSteganographyService;
        _preferencesService = preferencesService;
        Init();
    }

    private void Init() {
        HideOrVerifyOrExtractProgress = 0;
        HideOrVerifyOrExtractText = "尚未进行操作";
        var newMode = _preferencesService.Get<ShowProgressMode>("ShowProgressMode");
        UpdateMode(newMode);
    }

    [RelayCommand]
    private void SettingProgressMode() {
        var oldMode = _preferencesService.Get<ShowProgressMode>("ShowProgressMode");
        var dialog = App.GetService<SettingProgressDialog>()!;
        dialog.Owner = Application.Current.MainWindow;

        if (dialog.ShowDialog() == true) {
            var newMode = _preferencesService.Get<ShowProgressMode>("ShowProgressMode");
            if (oldMode == newMode) {
                return;
            }

            UpdateMode(newMode);
        }
    }

    private void UpdateMode(ShowProgressMode newMode) {
        IsPercentMode = newMode == ShowProgressMode.Percent;
        IsTextMode = newMode == ShowProgressMode.Text;
    }

    [RelayCommand]
    private void SelectBmpPath() {
        OpenFileDialog openBmpDialog = new() {
            Filter = "Bmp图片|*.bmp",
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (openBmpDialog.ShowDialog() != true) {
            return;
        }

        if (!CheckFile(openBmpDialog.FileName)) {
            _snackbarService.ShowWarning("选择文件错误", "文件不存在");
            return;
        }

        BmpPath = openBmpDialog.FileName;
    }

    async partial void OnBmpPathChanged(string value) {
        try {
            if (!CheckFile(value)) {
                _snackbarService.ShowWarning("选择文件错误", "文件不存在");
                return;
            }

            BmpSize = new FileInfo(value).Length;
            MaxHideSize = await _bmpSteganographyService.GetMaxHideSize(value);
        } catch (Exception e) {
            _snackbarService.ShowError("文件变更加载信息时错误", e.Message);
        }
    }

    [RelayCommand]
    private void SelectFileToHidePath() {
        OpenFileDialog openFileDialog = new() {
            Filter = "所有文件|*.*",
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (openFileDialog.ShowDialog() != true) {
            return;
        }

        if (!CheckFile(openFileDialog.FileName)) {
            _snackbarService.ShowWarning("选择文件错误", "文件不存在");
            return;
        }

        FileToHidePath = openFileDialog.FileName;
    }

    partial void OnFileToHidePathChanged(string value) {
        if (!CheckFile(value)) {
            _snackbarService.ShowWarning("选择文件错误", "文件不存在");
            return;
        }

        FileToHideSize = new FileInfo(value).Length;
    }

    private static bool CheckFile(string filePath) {
        return File.Exists(filePath);
    }

    [RelayCommand]
    private void SelectOutputFolder() {
        OpenFolderDialog openFolderDialog = new() {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (openFolderDialog.ShowDialog() != true) {
            return;
        }

        if (!CheckDirectory(openFolderDialog.FolderName)) {
            _snackbarService.ShowWarning("选择文件夹错误", "文件夹不存在");
            return;
        }

        OutputFolderPath = openFolderDialog.FolderName;
    }

    partial void OnOutputFolderPathChanged(string value) {
        if (!CheckDirectory(value)) {
            _snackbarService.ShowWarning("选择文件夹错误", "文件夹不存在");
        }
    }

    private static bool CheckDirectory(string directoryPath) {
        return Directory.Exists(directoryPath);
    }

    [RelayCommand(CanExecute = nameof(CanStartHide))]
    private async Task StartHide() {
        if (FileToHideSize > MaxHideSize) {
            _snackbarService.ShowWarning("禁止隐写", "隐藏文件大小超出最大隐写大小");
            return;
        }

        NotExecuting = false;
        OutputBmpPath = string.Empty;

        try {
            await _bmpSteganographyService.Hide(BmpPath, FileToHidePath, OutputFolderPath,
                IsPercentMode ? progressPercent => { HideOrVerifyOrExtractProgress = progressPercent; } : null,
                IsTextMode ? progressText => { HideOrVerifyOrExtractText = progressText; } : null,
                outputBmpPath => { OutputBmpPath = outputBmpPath; });
        } catch (Exception e) {
            _snackbarService.ShowError("隐写时发生错误", e.Message);
        } finally {
            NotExecuting = true;
        }
    }

    private bool CanStartHide() {
        return CheckFile(BmpPath) && CheckFile(FileToHidePath) && CheckDirectory(OutputFolderPath);
    }

    [RelayCommand(CanExecute = nameof(CanVerify))]
    private async Task Verify() {
        NotExecuting = false;
        OutputBmpPath = string.Empty;
        try {
            var result =
                await _bmpSteganographyService.Verify(BmpPath,
                    IsPercentMode ? progressPercent => { HideOrVerifyOrExtractProgress = progressPercent; } : null,
                    IsTextMode ? progressText => { HideOrVerifyOrExtractText = progressText; } : null);
            if (result) {
                _snackbarService.ShowSuccess("校验成功", "此文件是符合本软件规则的隐藏文件");
            } else {
                _snackbarService.ShowWarning("校验失败", "此文件内无隐藏内容，或不是符合本软件规则的隐藏文件");
            }
        } catch (Exception e) {
            _snackbarService.ShowError("校验时发生错误", e.Message);
        } finally {
            NotExecuting = true;
        }
    }

    private bool CanVerify() {
        return CheckFile(BmpPath);
    }

    [RelayCommand(CanExecute = nameof(CanStartExtract))]
    private async Task StartExtract() {
        NotExecuting = false;
        OutputBmpPath = string.Empty;
        try {
            await _bmpSteganographyService.Extract(BmpPath, OutputFolderPath,
                IsPercentMode ? progressPercent => { HideOrVerifyOrExtractProgress = progressPercent; } : null,
                IsTextMode ? progressText => { HideOrVerifyOrExtractText = progressText; } : null);
        } catch (Exception e) {
            _snackbarService.ShowError("提取隐藏文件时发生错误", e.Message);
        } finally {
            NotExecuting = true;
        }
    }

    private bool CanStartExtract() {
        return CheckFile(BmpPath) && CheckDirectory(OutputFolderPath);
    }
}