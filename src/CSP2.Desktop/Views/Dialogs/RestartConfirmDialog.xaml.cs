using System.Windows;

namespace CSP2.Desktop.Views.Dialogs;

/// <summary>
/// 重启确认对话框
/// </summary>
public partial class RestartConfirmDialog : Window
{
    /// <summary>
    /// 对话框结果
    /// </summary>
    public bool ShouldRestart { get; private set; } = false;

    /// <summary>
    /// 更改类型（用于显示不同的消息）
    /// </summary>
    public string ChangeType { get; set; } = "";

    public RestartConfirmDialog()
    {
        InitializeComponent();
        UpdateMessage();
    }

    public RestartConfirmDialog(string changeType) : this()
    {
        ChangeType = changeType;
        UpdateMessage();
    }

    /// <summary>
    /// 更新消息内容
    /// </summary>
    private void UpdateMessage()
    {
        if (MessageTextBlock == null) return;

        var message = ChangeType switch
        {
            "Language" => "语言设置已更改，需要重启应用程序以完全生效。",
            "Theme" => "主题设置已更改，需要重启应用程序以完全生效。",
            _ => "设置已更改，需要重启应用程序以完全生效。"
        };

        MessageTextBlock.Text = message;
    }

    /// <summary>
    /// 重启按钮点击事件
    /// </summary>
    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        ShouldRestart = true;
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ShouldRestart = false;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CancelButton_Click(sender, e);
    }

    /// <summary>
    /// 显示重启确认对话框
    /// </summary>
    /// <param name="owner">父窗口</param>
    /// <param name="changeType">更改类型</param>
    /// <returns>是否选择重启</returns>
    public static bool ShowDialog(Window? owner, string changeType = "")
    {
        var dialog = new RestartConfirmDialog(changeType)
        {
            Owner = owner
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.ShouldRestart;
    }
}
