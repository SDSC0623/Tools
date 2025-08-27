using System.Windows.Controls;
using Tools.ViewModel.SettingPage;

namespace Tools.Views.Pages;

public partial class SettingPage : Page {
    public SettingPage(SettingPageViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}