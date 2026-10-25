// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.AudioShare;

namespace Tools.Views.Pages.AudioShare;

[Description("局域网音频同步共享 - 发送端")]
[AvailbleStartPage(5)]
public partial class AudioShareSenderPage : Page {
    public AudioShareSenderPage(AudioShareSenderViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}
