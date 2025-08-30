// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Tools.Behaviors;

public class DropFileOrFolderBehavior : Behavior<FrameworkElement> {
    public string FilePath {
        get => (string)GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
        nameof(FilePath),
        typeof(string),
        typeof(DropFileOrFolderBehavior),
        new UIPropertyMetadata(default(string))
    );

    public bool OnlyBmp {
        get => (bool)GetValue(OnlyBmpProperty);
        set => SetValue(OnlyBmpProperty, value);
    }

    public static readonly DependencyProperty OnlyBmpProperty = DependencyProperty.Register(
        nameof(OnlyBmp),
        typeof(bool),
        typeof(DropFileOrFolderBehavior),
        new UIPropertyMetadata(false)
    );

    public bool IsFolder {
        get => (bool)GetValue(IsFolderProperty);
        set => SetValue(IsFolderProperty, value);
    }

    public static readonly DependencyProperty IsFolderProperty = DependencyProperty.Register(
        nameof(IsFolder),
        typeof(bool),
        typeof(DropFileOrFolderBehavior),
        new UIPropertyMetadata(false)
    );

    protected override void OnAttached() {
        AssociatedObject.AllowDrop = true;
        AssociatedObject.AddHandler(UIElement.DragOverEvent, new DragEventHandler(DragOverHandler), true);
        AssociatedObject.AddHandler(UIElement.DropEvent, new DragEventHandler(DropHandler), true);
    }

    protected override void OnDetaching() {
        AssociatedObject.RemoveHandler(UIElement.DragOverEvent, new DragEventHandler(DragOverHandler));
        AssociatedObject.RemoveHandler(UIElement.DropEvent, new DragEventHandler(DropHandler));
    }

    private void DragOverHandler(object sender, DragEventArgs e) {
        if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files is { Length: > 0 }) {
                var file = files[0];
                if ((!OnlyBmp || Path.GetExtension(file).Equals(".bmp")) &&
                    (!IsFolder || File.GetAttributes(file) == FileAttributes.Directory)) {
                    e.Effects = DragDropEffects.All;
                } else {
                    e.Effects = DragDropEffects.None;
                }
            }
        }

        e.Handled = true;
    }

    private void DropHandler(object sender, DragEventArgs e) {
        if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);

            if (files is { Length: > 0 }) {
                FilePath = files[0];
            }
        }
    }
}