// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;

namespace Tools.Controls;

public partial class LoadingBox : UserControl {
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(LoadingBox),
            new PropertyMetadata(false));

    public static readonly DependencyProperty LoadingMessageProperty =
        DependencyProperty.Register(nameof(LoadingMessage), typeof(string), typeof(LoadingBox),
            new PropertyMetadata("加载中...且加载信息绑定失败"));

    public bool IsActive {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string LoadingMessage {
        get => (string)GetValue(LoadingMessageProperty);
        set => SetValue(LoadingMessageProperty, value);
    }

    public LoadingBox() {
        InitializeComponent();
    }
}