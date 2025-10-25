using CSP2.Desktop.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace CSP2.Desktop.Views.Pages;

/// <summary>
/// ServerInstallPage.xaml 的交互逻辑
/// </summary>
public partial class ServerInstallPage : UserControl
{
    public ServerInstallPage()
    {
        InitializeComponent();
    }

    private void SteamCmdOption_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ServerInstallPageViewModel vm)
        {
            vm.SelectSteamCmdModeCommand.Execute(null);
        }
    }

    private void ExistingOption_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ServerInstallPageViewModel vm)
        {
            vm.SelectExistingModeCommand.Execute(null);
        }
    }
}

