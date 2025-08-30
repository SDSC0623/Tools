// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.Base64ToolPage;

namespace Tools.Views.Pages.Base64Tool;

[Description("Base64编解码工具")]
[AvailbleStartPage(3)]
public partial class Base64ToolPage : Page {
    public Base64ToolPage(Base64ToolViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}