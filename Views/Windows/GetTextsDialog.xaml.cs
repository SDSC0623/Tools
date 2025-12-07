// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Tools.ViewModel.GetTextsDialogViewModel;
using Wpf.Ui.Controls;

namespace Tools.Views.Windows;

public partial class GetTextsDialog : FluentWindow {
    public List<GetStringInfo> Result { get; set; } = [];

    public GetTextsDialog(GetTextsDialogViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
    }

    public void SetTexts(Func<List<GetStringInfo>> getTexts) {
        var getStringInfos = getTexts();
        var temp = (DataContext as GetTextsDialogViewModel)!.Texts;
        temp.Clear();
        for (int i = 0; i < getStringInfos.Count; i++) {
            temp.Add(new GetStringInfo {
                Text = getStringInfos[i].Text,
                PlaceHolderAndName = getStringInfos[i].PlaceHolderAndName ?? $"请输入第{i + 1}个字符串"
            });
        }
    }

    public void SetTitle(string title) {
        (DataContext as GetTextsDialogViewModel)!.SetTitle(title);
    }

    public void SetContent(string content) {
        (DataContext as GetTextsDialogViewModel)!.SetContent(content);
    }
}