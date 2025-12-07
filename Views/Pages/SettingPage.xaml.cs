// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Controls;
using System.Windows.Documents;
using Serilog;
using Serilog.Sinks.RichTextBox.Abstraction;
using Tools.Attributes;
using Tools.Helpers;
using Tools.ViewModel.SettingPage;

namespace Tools.Views.Pages;
[NeedStartupInit]
public partial class SettingPage : Page {
    public SettingPage(SettingPageViewModel viewModel, IRichTextBox richTextBox) {
        InitializeComponent();
        DataContext = viewModel;
        LogTextBox.TextChanged += LogTextBoxTextChanged;
        richTextBox.RichTextBox = LogTextBox;
    }

    private void LogTextBoxTextChanged(object sender, TextChangedEventArgs e) {
        var textRange = new TextRange(LogTextBox.Document.ContentStart, LogTextBox.Document.ContentEnd);
        if (textRange.Text.Length > 10000) {
            LogTextBox.Document.Blocks.Clear();
        }

        LogTextBox.ScrollToEnd();
    }
}