using System;
using System.Windows;
using System.Windows.Media.Animation;
using CSP2.Core.Models;

namespace CSP2.Desktop.Views.Dialogs;

public partial class FrameworkInstallProgressDialog : Window
{
    private bool _isCancelled;
    private bool _isCompleted;

    public bool IsCancelled => _isCancelled;
    public bool IsCompleted => _isCompleted;

    public FrameworkInstallProgressDialog(string frameworkName)
    {
        InitializeComponent();
        TitleText.Text = $"正在安装 {frameworkName}";
        Title = $"安装 {frameworkName}";
    }

    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(InstallProgress progress)
    {
        Dispatcher.BeginInvoke(() =>
        {
            // 更新百分比
            PercentageText.Text = $"{progress.Percentage:F0}%";

            // 更新进度条宽度（动画）
            var targetWidth = (ActualWidth - 60) * (progress.Percentage / 100);
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ProgressBar.BeginAnimation(WidthProperty, animation);

            // 更新当前步骤
            if (!string.IsNullOrEmpty(progress.CurrentStep))
            {
                CurrentStepText.Text = progress.CurrentStep;
            }

            // 更新详细消息
            if (!string.IsNullOrEmpty(progress.Message))
            {
                MessageText.Text = progress.Message;
            }

            // 根据进度阶段更新图标
            if (progress.Percentage < 10)
            {
                IconText.Text = "⏳"; // 准备中
            }
            else if (progress.Percentage < 60)
            {
                IconText.Text = "⬇️"; // 下载中
            }
            else if (progress.Percentage < 95)
            {
                IconText.Text = "📦"; // 解压中
            }
            else if (progress.Percentage >= 100)
            {
                IconText.Text = "✅"; // 完成
            }
        });
    }

    /// <summary>
    /// 显示安装依赖的状态
    /// </summary>
    public void ShowInstallingDependency(string dependencyName)
    {
        Dispatcher.BeginInvoke(() =>
        {
            TitleText.Text = $"正在安装依赖: {dependencyName}";
            CurrentStepText.Text = "CounterStrikeSharp 需要先安装 Metamod";
            IconText.Text = "🔗";
        });
    }

    /// <summary>
    /// 显示安装成功
    /// </summary>
    public void ShowSuccess(string message = "安装成功！")
    {
        Dispatcher.BeginInvoke(() =>
        {
            _isCompleted = true;
            IconText.Text = "✅";
            TitleText.Text = "安装成功";
            CurrentStepText.Text = message;
            PercentageText.Text = "100%";
            CancelButton.Content = "关闭";
            CancelButton.Style = (Style)FindResource("PrimaryButtonStyle");

            // 进度条填满
            var targetWidth = ActualWidth - 60;
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ProgressBar.BeginAnimation(WidthProperty, animation);
        });
    }

    /// <summary>
    /// 显示安装失败
    /// </summary>
    public void ShowError(string errorMessage)
    {
        Dispatcher.BeginInvoke(() =>
        {
            IconText.Text = "❌";
            TitleText.Text = "安装失败";
            CurrentStepText.Text = "安装过程中出现错误";
            MessageText.Text = errorMessage;
            CancelButton.Content = "关闭";
            CancelButton.Style = (Style)FindResource("SecondaryButtonStyle");
        });
    }

    /// <summary>
    /// 取消按钮点击
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isCompleted)
        {
            // 已完成，直接关闭
            DialogResult = true;
            Close();
        }
        else
        {
            // 确认取消
            var result = MessageBox.Show(
                "确定要取消安装吗？",
                "确认取消",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _isCancelled = true;
                DialogResult = false;
                Close();
            }
        }
    }
}

