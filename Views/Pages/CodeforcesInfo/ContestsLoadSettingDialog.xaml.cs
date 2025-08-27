using Tools.ViewModel.CodeforcesInfoPage;
using Wpf.Ui.Controls;

namespace Tools.Views.Pages.CodeforcesInfo;

public partial class ContestsLoadSettingDialog : FluentWindow {
    public ContestsLoadSettingDialog(ContestsLoadSettingDialogViewModel viewModel) {
        InitializeComponent();
        viewModel.SetWindow(this);
        DataContext = viewModel;
    }
}