// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.HideInBmpPage;

namespace Tools.Views.Pages.HideInBmp;

[Description("Bmp图片隐写")]
[AvailbleStartPage(2)]
public partial class HideInBmpPage : Page {
    public HideInBmpPage(HideInBmpViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}