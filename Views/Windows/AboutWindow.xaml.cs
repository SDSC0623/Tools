// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows;
using Tools.Helpers;
using Wpf.Ui.Controls;

namespace Tools.Views.Windows;

public partial class AboutWindow : FluentWindow {
    public string Version => GlobalSettings.Version;

    public AboutWindow() {
        InitializeComponent();
        DataContext = this;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) {
        Close();
    }
}