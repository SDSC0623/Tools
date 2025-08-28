// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.CodeforcesInfoPage;

namespace Tools.Views.Pages.CodeforcesInfo;

[Description("Codeforces信息查看")]
[AvailbleStartPage(1)]
public partial class CodeforcesInfoPage : Page {
    public CodeforcesInfoPage(CodeforcesInfoViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}