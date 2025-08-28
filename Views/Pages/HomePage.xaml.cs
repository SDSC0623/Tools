// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.HomePage;

namespace Tools.Views.Pages;

[Description("主页")]
[AvailbleStartPage(0)]
public partial class HomePage : Page {
    public HomePage(HomePageViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}