using Tools.ViewModel.CodeforcesInfoPage;
using Wpf.Ui.Controls;

namespace Tools.Views.Pages.CodeforcesInfo;

public partial class UserInfoSettingDialog : FluentWindow {
    public UserInfoSettingDialog(UserInfoSettingDialogViewModel viewModel) {
        InitializeComponent();
        viewModel.SetWindow(this);
        DataContext = viewModel;
    }
}