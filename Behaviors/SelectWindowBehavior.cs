// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Controls;

namespace Tools.Behaviors;

public class SelectWindowBehavior : Behavior<Border> {
    public Brush HoverBackgroundBrush {
        get => (Brush)GetValue(HoverBackgroundBrushProperty);
        set => SetValue(HoverBackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty HoverBackgroundBrushProperty = DependencyProperty.Register(
        nameof(HoverBackgroundBrush),
        typeof(Brush),
        typeof(SelectWindowBehavior),
        new UIPropertyMetadata(Brushes.Transparent)
    );

    public Brush NormalBackgroundBrush {
        get => (Brush)GetValue(NormalBackgroundBrushProperty);
        set => SetValue(NormalBackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty NormalBackgroundBrushProperty = DependencyProperty.Register(
        nameof(NormalBackgroundBrush),
        typeof(Brush),
        typeof(SelectWindowBehavior),
        new UIPropertyMetadata(Brushes.Transparent)
    );

    public ICommand MouseLeftClickCommand {
        get => (ICommand)GetValue(MouseLeftClickCommandProperty);
        set => SetValue(MouseLeftClickCommandProperty, value);
    }

    public static readonly DependencyProperty MouseLeftClickCommandProperty = DependencyProperty.Register(
        nameof(MouseLeftClickCommand),
        typeof(ICommand),
        typeof(SelectWindowBehavior),
        new UIPropertyMetadata(null)
    );

    public ICommand MouseLeftDoubleClickCommand {
        get => (ICommand)GetValue(MouseLeftDoubleClickCommandProperty);
        set => SetValue(MouseLeftDoubleClickCommandProperty, value);
    }

    public static readonly DependencyProperty MouseLeftDoubleClickCommandProperty = DependencyProperty.Register(
        nameof(MouseLeftDoubleClickCommand),
        typeof(ICommand),
        typeof(SelectWindowBehavior),
        new UIPropertyMetadata(null)
    );

    public object CommandParameter {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(SelectWindowBehavior),
        new UIPropertyMetadata(null)
    );

    protected override void OnAttached() {
        AssociatedObject.MouseEnter += OnMouseEnter;
        AssociatedObject.MouseLeave += OnMouseLeave;
        AssociatedObject.MouseDown += OnMouseLeftButtonUp;
    }

    protected override void OnDetaching() {
        AssociatedObject.MouseEnter -= OnMouseEnter;
        AssociatedObject.MouseLeave -= OnMouseLeave;
        AssociatedObject.MouseDown -= OnMouseLeftButtonUp;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        if (e.ClickCount == 1) {
            if (MouseLeftClickCommand.CanExecute(CommandParameter)) {
                MouseLeftClickCommand.Execute(CommandParameter);
            }
        } else {
            if (MouseLeftDoubleClickCommand.CanExecute(CommandParameter)) {
                MouseLeftDoubleClickCommand.Execute(CommandParameter);
            }
        }
    }

    private void OnMouseEnter(object sender, MouseEventArgs e) {
        AssociatedObject.Background = HoverBackgroundBrush;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) {
        AssociatedObject.Background = NormalBackgroundBrush;
    }
}