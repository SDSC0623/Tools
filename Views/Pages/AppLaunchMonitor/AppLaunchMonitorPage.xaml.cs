// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.AppLaunchMonitor;

namespace Tools.Views.Pages.AppLaunchMonitor;

[Description("App启动监控工具")]
[AvailbleStartPage(4)]
[NeedStartupInit]
[NeedDisposePage(nameof(Dispose))]
public partial class AppLaunchMonitorPage : Page {
    public AppLaunchMonitorPage(AppLaunchMonitorViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void Dispose() {
        (DataContext as AppLaunchMonitorViewModel)!.Dispose();
    }
}