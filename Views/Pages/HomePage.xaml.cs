using System.ComponentModel;
using System.Windows.Controls;
using Tools.ViewModel.HomePage;

namespace Tools.Views.Pages;

[Description("主页")]
public partial class HomePage : Page {
    public HomePage(HomePageViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}