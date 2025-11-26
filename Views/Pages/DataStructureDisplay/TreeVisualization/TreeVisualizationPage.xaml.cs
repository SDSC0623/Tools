// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;
using Tools.ViewModel.DataStructureDisplay.TreeVisualization;

namespace Tools.Views.Pages.DataStructureDisplay.TreeVisualization;

public partial class TreeVisualizationPage : Page {
    private readonly TreeVisualizationViewModel _viewModel;

    public TreeVisualizationPage(TreeVisualizationViewModel viewModel) {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e) {
        _viewModel.UpdateSize(Canvas.ActualWidth, Canvas.ActualHeight);
    }
}