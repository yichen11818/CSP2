using CSP2.Core.Models;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace CSP2.Desktop.Views.Dialogs;

public partial class AddServerDialog : Window
{
    public string ServerName => ServerNameTextBox.Text;
    public string InstallPath => InstallPathTextBox.Text;
    public ServerConfig ServerConfig { get; private set; }

    public AddServerDialog()
    {
        InitializeComponent();
        ServerNameTextBox.Focus();
        ServerConfig = new ServerConfig();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择 CS2 服务器根目录",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            InstallPathTextBox.Text = dialog.FolderName;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            MessageBox.Show("请输入服务器名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            ServerNameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(InstallPath))
        {
            MessageBox.Show("请选择服务器路径", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            InstallPathTextBox.Focus();
            return;
        }

        if (!Directory.Exists(InstallPath))
        {
            var result = MessageBox.Show(
                $"路径 '{InstallPath}' 不存在。\n\n是否仍要添加？",
                "路径不存在",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return;
            }
        }

        // 验证端口号
        if (!int.TryParse(PortTextBox.Text, out int port) || port < 1 || port > 65535)
        {
            MessageBox.Show("请输入有效的端口号（1-65535）", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            PortTextBox.Focus();
            return;
        }

        // 验证最大玩家数
        if (!int.TryParse(MaxPlayersTextBox.Text, out int maxPlayers) || maxPlayers < 1 || maxPlayers > 64)
        {
            MessageBox.Show("请输入有效的最大玩家数（1-64）", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            MaxPlayersTextBox.Focus();
            return;
        }

        // 获取Tick Rate
        int tickRate = 64;
        if (TickRateComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
        {
            int.TryParse(selectedItem.Content.ToString(), out tickRate);
        }

        // 构建配置
        ServerConfig = new ServerConfig
        {
            IpAddress = IpAddressTextBox.Text,
            Port = port,
            Map = MapTextBox.Text,
            MaxPlayers = maxPlayers,
            TickRate = tickRate,
            MapGroup = MapGroupTextBox.Text,
            ServerName = ServerName,
            EnableConsole = true,
            EnableLogging = true
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

