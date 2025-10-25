using System.Windows.Controls;
using CSP2.Desktop.ViewModels;

namespace CSP2.Desktop.Views.Pages;

/// <summary>
/// DebugConsolePage.xaml 的交互逻辑
/// </summary>
public partial class DebugConsolePage : UserControl
{
    public DebugConsolePage(DebugConsoleViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

