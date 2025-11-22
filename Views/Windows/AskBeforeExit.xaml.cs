// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows;
using Tools.Models;
using Tools.ViewModel.AskBeforeExitDialog;
using Wpf.Ui.Controls;

namespace Tools.Views.Windows;

public partial class AskBeforeExit : FluentWindow {
    public ExitMode ExitMode { get; set; } = ExitMode.Ask;

    public AskBeforeExit(AskBeforeExitViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SetWindow(this);
    }
}