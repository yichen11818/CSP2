using System.Windows.Controls;
using System.Windows.Input;
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

    /// <summary>
    /// 命令输入框键盘事件处理（支持 ↑↓ 导航历史）
    /// </summary>
    private void CommandTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Up)
        {
            // ↑ 键：显示上一条命令
            _viewModel.NavigateHistoryOlder();
            
            // 将光标移到末尾
            CommandTextBox.SelectionStart = CommandTextBox.Text.Length;
            CommandTextBox.SelectionLength = 0;
            
            e.Handled = true; // 阻止默认行为
        }
        else if (e.Key == System.Windows.Input.Key.Down)
        {
            // ↓ 键：显示下一条命令
            _viewModel.NavigateHistoryNewer();
            
            // 将光标移到末尾
            CommandTextBox.SelectionStart = CommandTextBox.Text.Length;
            CommandTextBox.SelectionLength = 0;
            
            e.Handled = true; // 阻止默认行为
        }
    }
}

