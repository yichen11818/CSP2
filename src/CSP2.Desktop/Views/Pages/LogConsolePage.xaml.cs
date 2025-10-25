using System.Windows.Controls;
using CSP2.Desktop.ViewModels;

namespace CSP2.Desktop.Views.Pages;

/// <summary>
/// LogConsolePage.xaml 的交互逻辑
/// </summary>
public partial class LogConsolePage : Page
{
    public LogConsolePage(LogConsoleViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

