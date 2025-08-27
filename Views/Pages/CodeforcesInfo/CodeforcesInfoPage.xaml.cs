using System.ComponentModel;
using System.Windows.Controls;
using Tools.ViewModel.CodeforcesInfoPage;

namespace Tools.Views.Pages.CodeforcesInfo;

[Description("Codeforces信息查看页")]
public partial class CodeforcesInfoPage : Page {
    public CodeforcesInfoPage(CodeforcesInfoViewModel viewModel) {
        InitializeComponent();
        DataContext = viewModel;
    }
}