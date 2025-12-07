// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Tools.Models;
using Tools.ViewModel.AppLaunchMonitor;
using Wpf.Ui.Controls;

namespace Tools.Views.Pages.AppLaunchMonitor;

public partial class PickWindowDialog : FluentWindow {
    public WindowInfo SelectedWindowResult { get; set; } = null!;

    public PickWindowDialog(PickWindowDialogViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
    }
}