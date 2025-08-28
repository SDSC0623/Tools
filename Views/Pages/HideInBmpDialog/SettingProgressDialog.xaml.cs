// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Tools.ViewModel.HideInBmpPage;
using Wpf.Ui.Controls;

namespace Tools.Views.Pages.HideInBmpDialog;

public partial class SettingProgressDialog : FluentWindow {
    public SettingProgressDialog(SettingProgressDialogViewModel viewModel) {
        InitializeComponent();
        viewModel.SetWindow(this);
        DataContext = viewModel;
    }
}