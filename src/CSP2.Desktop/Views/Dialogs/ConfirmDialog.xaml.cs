using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CSP2.Desktop.Views.Dialogs;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string subtitle = "此操作无法撤销", 
        string confirmButtonText = "确认", string icon = "⚠️", bool isDangerous = true)
    {
        InitializeComponent();

        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
        SubtitleTextBlock.Text = subtitle;
        ConfirmButton.Content = confirmButtonText;
        IconTextBlock.Text = icon;

        // 根据类型调整图标背景颜色
        if (isDangerous)
        {
            var iconBorder = (Border)IconTextBlock.Parent;
            iconBorder.Background = (System.Windows.Media.Brush)FindResource("DangerLightBrush");
            ConfirmButton.Style = (Style)FindResource("DangerButtonStyle");
        }
        else
        {
            var iconBorder = (Border)IconTextBlock.Parent;
            iconBorder.Background = (System.Windows.Media.Brush)FindResource("InfoLightBrush");
            ConfirmButton.Style = (Style)FindResource("PrimaryButtonStyle");
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public static bool Show(Window owner, string title, string message, 
        string subtitle = "此操作无法撤销", string confirmButtonText = "确认", 
        string icon = "⚠️", bool isDangerous = true)
    {
        var dialog = new ConfirmDialog(title, message, subtitle, confirmButtonText, icon, isDangerous)
        {
            Owner = owner
        };
        return dialog.ShowDialog() == true;
    }
}

