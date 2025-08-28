using System.ComponentModel;
using System.Windows.Controls;
using Tools.Attributes;
using Tools.ViewModel.CodeforcesInfoPage;

namespace Tools.Views.Pages.CodeforcesInfo;

[Description("Codeforces信息查看页")]
[AvailbleStartPage(1)]
public partial class CodeforcesInfoPage : Page {
    public CodeforcesInfoPage(CodeforcesInfoViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}