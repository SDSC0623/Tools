using Tools.ViewModel.HideInBmpPage;
using Wpf.Ui.Controls;

namespace Tools.Views.Pages.HideInBmpDialog;

public partial class SettingProgressDialog : FluentWindow {
    public SettingProgressDialog(SettingProgressDialogViewModel viewModel) {
        InitializeComponent();
        viewModel.SetWindow(this);
        DataContext = viewModel;
    }
}