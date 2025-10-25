using System;
using System.Windows;

namespace CSP2.Desktop.Views;

/// <summary>
/// 错误对话框 - 显示详细错误信息并允许用户复制
/// </summary>
public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    /// <param name="errorMessage">简短错误消息</param>
    /// <param name="exception">异常对象（可选）</param>
    /// <param name="subtitle">副标题（可选）</param>
    public static void Show(string errorMessage, Exception? exception = null, string? subtitle = null)
    {
        var dialog = new ErrorDialog();
        
        // 设置副标题
        if (!string.IsNullOrEmpty(subtitle))
        {
            dialog.SubtitleText.Text = subtitle;
        }

        // 设置错误消息
        dialog.ErrorMessageText.Text = errorMessage;

        // 构建详细信息
        var detailText = $"=== CSP2 错误报告 ===\n\n";
        detailText += $"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";
        detailText += $"错误消息:\n{errorMessage}\n\n";

        if (exception != null)
        {
            detailText += $"异常类型: {exception.GetType().FullName}\n\n";
            detailText += $"异常详情:\n{exception.Message}\n\n";
            detailText += $"堆栈跟踪:\n{exception.StackTrace}\n\n";

            // 如果有内部异常
            if (exception.InnerException != null)
            {
                detailText += $"--- 内部异常 ---\n\n";
                detailText += $"类型: {exception.InnerException.GetType().FullName}\n";
                detailText += $"消息: {exception.InnerException.Message}\n";
                detailText += $"堆栈: {exception.InnerException.StackTrace}\n\n";
            }
        }

        detailText += $"=== 环境信息 ===\n";
        detailText += $"操作系统: {Environment.OSVersion}\n";
        detailText += $".NET 版本: {Environment.Version}\n";
        detailText += $"工作目录: {Environment.CurrentDirectory}\n";

        dialog.DetailTextBox.Text = detailText;

        // 显示对话框
        dialog.ShowDialog();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(DetailTextBox.Text);
            MessageBox.Show("错误信息已复制到剪贴板！", "CSP2", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制失败：{ex.Message}", "CSP2", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

