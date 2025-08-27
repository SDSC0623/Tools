using System.Windows;
using Tools.Helpers;
using Wpf.Ui.Controls;

namespace Tools.Views.Windows;

public partial class AboutWindow : FluentWindow {
    public string Version => GlobleSettings.Version;

    public AboutWindow() {
        InitializeComponent();
        DataContext = this;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) {
        Close();
    }
}