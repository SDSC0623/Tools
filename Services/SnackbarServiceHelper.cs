using Wpf.Ui;
using Wpf.Ui.Controls;

// ReSharper disable UnusedMember.Global

namespace Tools.Services;

public class SnackbarServiceHelper {
    private readonly ISnackbarService _snackbarService;

    public SnackbarServiceHelper(ISnackbarService snackbarService) {
        _snackbarService = snackbarService;
    }

    public void ShowInfo(string title, string context) {
        _snackbarService.Show(
            title,
            context,
            ControlAppearance.Info,
            new SymbolIcon(SymbolRegular.Info16),
            TimeSpan.FromSeconds(3));
    }

    public void ShowSuccess(string title, string context) {
        _snackbarService.Show(
            title,
            context,
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle32),
            TimeSpan.FromSeconds(3));
    }

    public void ShowWarning(string title, string context) {
        _snackbarService.Show(
            title,
            context,
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24),
            TimeSpan.FromSeconds(5));
    }

    public void ShowError(string title, string context) {
        _snackbarService.Show(
            title,
            context,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle32),
            TimeSpan.FromSeconds(5));
    }

    public void ShowDebugMessage(string title, string context) {
        _snackbarService.Show(
            title,
            context,
            ControlAppearance.Secondary,
            new SymbolIcon(SymbolRegular.Code24),
            TimeSpan.FromSeconds(2));
    }
}