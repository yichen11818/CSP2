using System.Windows.Controls;
using CSP2.Desktop.ViewModels;

namespace CSP2.Desktop.Views.Pages;

/// <summary>
/// ServerManagementPage.xaml 的交互逻辑
/// </summary>
public partial class ServerManagementPage : UserControl
{
    public ServerManagementPage(ServerManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

