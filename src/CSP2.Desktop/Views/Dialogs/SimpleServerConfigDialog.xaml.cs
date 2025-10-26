using CSP2.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace CSP2.Desktop.Views.Dialogs;

/// <summary>
/// 简化版服务器配置对话框
/// </summary>
public partial class SimpleServerConfigDialog : Window
{
    public ServerConfig ServerConfig { get; private set; }

    public SimpleServerConfigDialog(ServerConfig? config = null)
    {
        InitializeComponent();

        // 如果提供了配置，则加载现有配置
        if (config != null)
        {
            LoadConfig(config);
        }

        ServerConfig = config ?? new ServerConfig();
    }

    private void LoadConfig(ServerConfig config)
    {
        // 核心配置
        IpAddressTextBox.Text = config.IpAddress;
        PortTextBox.Text = config.Port.ToString();
        MapComboBox.Text = config.Map;
        MaxPlayersTextBox.Text = config.MaxPlayers.ToString();
        TickRateComboBox.SelectedIndex = config.TickRate == 128 ? 1 : 0;
        GameModeComboBox.SelectedIndex = config.GameMode;

        // 常用选项
        DisableBotsCheckBox.IsChecked = config.DisableBots;
        InsecureModeCheckBox.IsChecked = config.InsecureMode;
        LanModeCheckBox.IsChecked = config.IsLanMode;
        OpenConsoleInAppCheckBox.IsChecked = config.OpenConsoleInApp;

        // 自定义参数
        CustomParametersTextBox.Text = config.CustomParameters;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证输入
        if (!ValidateInput())
        {
            return;
        }

        // 构建配置
        ServerConfig = new ServerConfig
        {
            // 核心配置
            IpAddress = IpAddressTextBox.Text.Trim(),
            Port = int.Parse(PortTextBox.Text),
            Map = MapComboBox.Text,
            MaxPlayers = int.Parse(MaxPlayersTextBox.Text),
            TickRate = TickRateComboBox.SelectedIndex == 1 ? 128 : 64,
            GameMode = GameModeComboBox.SelectedIndex,
            GameType = 0, // 固定为经典模式

            // 常用选项
            DisableBots = DisableBotsCheckBox.IsChecked == true,
            InsecureMode = InsecureModeCheckBox.IsChecked == true,
            IsLanMode = LanModeCheckBox.IsChecked == true,
            OpenConsoleInApp = OpenConsoleInAppCheckBox.IsChecked == true,

            // 自定义参数
            CustomParameters = CustomParametersTextBox.Text.Trim(),

            // 保留高级配置（如果之前有的话）
            ServerName = ServerConfig.ServerName,
            ServerPassword = ServerConfig.ServerPassword,
            RconPassword = ServerConfig.RconPassword,
            SteamToken = ServerConfig.SteamToken,
            QuickCommands = ServerConfig.QuickCommands
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool ValidateInput()
    {
        // 验证IP地址
        if (string.IsNullOrWhiteSpace(IpAddressTextBox.Text))
        {
            MessageBox.Show("请输入IP地址", 
                "验证失败", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            IpAddressTextBox.Focus();
            return false;
        }

        // 验证端口
        if (!int.TryParse(PortTextBox.Text, out int port) || port < 1 || port > 65535)
        {
            MessageBox.Show("请输入有效的端口号（1-65535）", 
                "验证失败", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            PortTextBox.Focus();
            return false;
        }

        // 验证地图
        if (string.IsNullOrWhiteSpace(MapComboBox.Text))
        {
            MessageBox.Show("请输入地图名称", 
                "验证失败", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            MapComboBox.Focus();
            return false;
        }

        // 验证最大玩家数
        if (!int.TryParse(MaxPlayersTextBox.Text, out int maxPlayers) || maxPlayers < 1 || maxPlayers > 64)
        {
            MessageBox.Show("请输入有效的最大玩家数（1-64）", 
                "验证失败", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
            MaxPlayersTextBox.Focus();
            return false;
        }

        return true;
    }
}

