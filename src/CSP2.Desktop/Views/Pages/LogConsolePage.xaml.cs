using System.Windows.Controls;
using CSP2.Desktop.ViewModels;

namespace CSP2.Desktop.Views.Pages;

/// <summary>
/// LogConsolePage.xaml 的交互逻辑
/// </summary>
public partial class LogConsolePage : UserControl
{
    private readonly LogConsoleViewModel _viewModel;
    
    public LogConsolePage(LogConsoleViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
    }
    
    /// <summary>
    /// 日志文本变化时自动滚动到底部
    /// </summary>
    private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel.AutoScroll && LogTextBox.Text.Length > 0)
        {
            LogTextBox.ScrollToEnd();
        }
    }
}

