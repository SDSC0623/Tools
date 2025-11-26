// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Controls;
using Tools.ViewModel.DataStructureDisplay.GraphVisualization;

namespace Tools.Views.Pages.DataStructureDisplay.GraphVisualization;

public partial class GraphVisualizationPage : Page {
    public GraphVisualizationPage(GraphVisualizationViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}