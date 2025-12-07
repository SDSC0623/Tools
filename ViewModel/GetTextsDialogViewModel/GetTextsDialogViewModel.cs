// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tools.Views.Windows;

namespace Tools.ViewModel.GetTextsDialogViewModel;

public partial class GetStringInfo : ObservableObject {
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private string? _placeHolderAndName;
}

public partial class GetTextsDialogViewModel : ObservableObject {
    [ObservableProperty] private string _title = "获取文本";

    [ObservableProperty] private string _content = "请输入要获取的文本";

    [ObservableProperty] private ObservableCollection<GetStringInfo> _texts = [];

    // 窗口对象
    private GetTextsDialog? _window;

    public void SetWindow(GetTextsDialog window) {
        _window = window;
    }

    public void SetTitle(string title) {
        Title = title;
    }

    public void SetContent(string content) {
        Content = content;
    }

    private void CloseDialog(bool result) {
        if (_window == null) {
            throw new Exception("窗口绑定异常");
        }

        _window.DialogResult = result;
        _window.Close();
    }

    [RelayCommand]
    private void Cancel() {
        CloseDialog(false);
    }

    [RelayCommand]
    private void Confirm() {
        if (_window == null) {
            throw new Exception("窗口绑定异常");
        }

        _window.Result = Texts.ToList();
        CloseDialog(true);
    }
}