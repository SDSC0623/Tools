// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Serilog;
using Tools.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Tools.ViewModel.Base64ToolPage;

public partial class Base64ToolViewModel : ObservableObject {
    // 输入输出
    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;

    // URL安全编码
    [ObservableProperty] private bool _isUrlSafe;

    // 自动检测到的编码
    [ObservableProperty] private string _detectedEncodingName = string.Empty;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // 简单对话框服务
    private readonly IContentDialogService _contentDialogService;

    // 日志
    private readonly ILogger _logger;

    public Base64ToolViewModel(SnackbarServiceHelper snackbarService, IContentDialogService contentDialogService,
        ILogger logger) {
        _snackbarService = snackbarService;
        _contentDialogService = contentDialogService;
        _logger = logger;
        Init();
    }

    private void Init() {
        DetectedEncodingName = Encoding.Default.EncodingName;
    }

    [RelayCommand]
    private async Task ClearInput() {
        try {
            var result = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions {
                    Title = "清空输入框请求确认",
                    Content = "是否清空输入框？",
                    CloseButtonText = "取消",
                    PrimaryButtonText = "确定"
                });

            if (result == ContentDialogResult.Primary) {
                InputText = string.Empty;
            } else {
                _snackbarService.ShowInfo("操作取消", "已取消清空操作");
            }
        } catch (Exception e) {
            _snackbarService.ShowError("问询请求时发生错误", e.Message);
        }
    }

    [RelayCommand]
    private void ImportFromFile() {
        OpenFileDialog dialog = new() {
            Filter = "文本文件|*.txt",
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() != true) {
            return;
        }

        if (File.Exists(dialog.FileName)) {
            InputText = File.ReadAllText(dialog.FileName);
        }
    }

    [RelayCommand]
    private void Encode() {
        try {
            var temp = Convert.ToBase64String(Encoding.Default.GetBytes(InputText));
            OutputText = IsUrlSafe ? temp.Replace('+', '-').Replace('/', '_') : temp;
        } catch (Exception e) {
            _snackbarService.ShowError("编码时发生错误", e.Message);
            _logger.Error("编码时发生错误，错误: {Message}", e.Message);
        }
    }

    [RelayCommand]
    private void Decode() {
        try {
            if (InputText.Contains('-') || InputText.Contains('/')) {
                IsUrlSafe = true;
            } else {
                IsUrlSafe = false;
            }

            var temp = IsUrlSafe ? InputText.Replace('-', '+').Replace('_', '/') : InputText;
            OutputText = Encoding.Default.GetString(Convert.FromBase64String(temp));
        } catch (Exception e) {
            _snackbarService.ShowError("解码时发生错误", e.Message);
            _logger.Error("解码时发生错误，错误: {Message}", e.Message);
        }
    }

    [RelayCommand]
    private void CopyOutput() {
        try {
            Clipboard.SetText(OutputText);
        } catch (Exception e) {
            _logger.Error("复制到剪贴板时发生错误，错误: {Message}", e.Message);
            _snackbarService.ShowError("复制到剪贴板时发生错误", e.Message);
        }
    }

    [RelayCommand]
    private async Task ExportToFile(CancellationToken cancellation) {
        SaveFileDialog dialog = new() {
            Filter = "文本文件|*.txt",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            FileName = "Output.txt"
        };

        if (dialog.ShowDialog() != true) {
            return;
        }

        try {
            await File.WriteAllTextAsync(dialog.FileName, OutputText, cancellation);
        } catch (OperationCanceledException) {
            _snackbarService.ShowInfo("操作终止", "已中断保存操作");
        } catch (Exception e) {
            _logger.Error("导出文件时发生错误，错误: {Message}", e.Message);
            _snackbarService.ShowError("保存文件时发生错误", e.Message);
        }
    }
}