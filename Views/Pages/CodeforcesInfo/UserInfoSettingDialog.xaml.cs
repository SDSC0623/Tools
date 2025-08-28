// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Tools.ViewModel.CodeforcesInfoPage;
using Wpf.Ui.Controls;

namespace Tools.Views.Pages.CodeforcesInfo;

public partial class UserInfoSettingDialog : FluentWindow {
    public UserInfoSettingDialog(UserInfoSettingDialogViewModel viewModel) {
        InitializeComponent();
        viewModel.SetWindow(this);
        DataContext = viewModel;
    }
}