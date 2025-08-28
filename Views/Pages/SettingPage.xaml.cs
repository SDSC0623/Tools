// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.SettingPage;

namespace Tools.Views.Pages;

public partial class SettingPage : Page {
    public SettingPage(SettingPageViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}