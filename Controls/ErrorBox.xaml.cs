using System.Windows;
using System.Windows.Controls;

namespace Tools.Controls;

public partial class ErrorBox : UserControl {

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ErrorBox),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(ErrorBox),
            new PropertyMetadata("发生错误，且错误数据绑定失败"));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public ErrorBox() {
        InitializeComponent();
    }
}