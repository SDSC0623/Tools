// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.GraphVisualization;

namespace Tools.Views.Pages.DataStructureDisplay.GraphVisualization;

[Description("数据结构课专用可视化界面")]
[AvailbleStartPage(4)]
public partial class GraphVisualizationPage : Page {
    public GraphVisualizationPage(GraphVisualizationViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}