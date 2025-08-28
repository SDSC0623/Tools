using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.HideInBmpPage;

namespace Tools.Views.Pages;

[Description("Bmp图片隐写")]
[AvailbleStartPage(2)]
public partial class HideInBmpPage : Page {
    public HideInBmpPage(HideInBmpViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}