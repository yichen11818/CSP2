using System.Windows.Controls;
using CSP2.Desktop.ViewModels;

namespace CSP2.Desktop.Views.Pages;

/// <summary>
/// PluginMarketPage.xaml 的交互逻辑
/// </summary>
public partial class PluginMarketPage : UserControl
{
    public PluginMarketPage(PluginMarketViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

