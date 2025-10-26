using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using CSP2.Core.Models;

namespace CSP2.Desktop.Views.Dialogs;

public partial class ServerConfigDialog : Window
{
    public ServerConfig Config { get; private set; }
    public RCONConfig RCONConfig { get; private set; }
    public bool IsConfirmed { get; private set; }

    public ServerConfigDialog(ServerConfig? config = null, RCONConfig? rconConfig = null)
    {
        InitializeComponent();
        
        // 如果提供了配置，则加载它
        if (config != null)
        {
            Config = config;
            LoadConfig(config);
        }
        else
        {
            Config = new ServerConfig();
        }

        if (rconConfig != null)
        {
            RCONConfig = rconConfig;
            LoadRCONConfig(rconConfig);
        }
        else
        {
            RCONConfig = new RCONConfig();
        }
    }

    /// <summary>
    /// 加载配置到UI控件
    /// </summary>
    private void LoadConfig(ServerConfig config)
    {
        // 基本配置
        IpAddressTextBox.Text = config.IpAddress;
        PortTextBox.Text = config.Port.ToString();
        MapComboBox.Text = config.Map;
        MapGroupComboBox.Text = config.MapGroup;
        MaxPlayersTextBox.Text = config.MaxPlayers.ToString();
        TickRateComboBox.Text = config.TickRate.ToString();
        GameTypeComboBox.SelectedIndex = config.GameType;
        GameModeComboBox.SelectedIndex = config.GameMode;

        // 服务器身份
        ServerNameTextBox.Text = config.ServerName ?? string.Empty;
        SteamTokenTextBox.Text = config.SteamToken ?? string.Empty;
        ServerPasswordBox.Password = config.ServerPassword ?? string.Empty;

        // 高级选项 - 网络
        IsLanModeCheckBox.IsChecked = config.IsLanMode;
        InsecureModeCheckBox.IsChecked = config.InsecureMode;

        // 高级选项 - 性能
        EnableConsoleCheckBox.IsChecked = config.EnableConsole;
        DisableHltvCheckBox.IsChecked = config.DisableHltv;
        ProcessPriorityComboBox.SelectedIndex = config.ProcessPriority switch
        {
            "low" => 0,
            "normal" => 1,
            "high" => 2,
            _ => 1
        };
        MaxFpsTextBox.Text = config.MaxFps?.ToString() ?? string.Empty;
        ThreadCountTextBox.Text = config.ThreadCount?.ToString() ?? string.Empty;

        // 高级选项 - 游戏规则
        EnableCheatsCheckBox.IsChecked = config.EnableCheats;
        BotQuotaTextBox.Text = config.BotQuota.ToString();
        BotDifficultyComboBox.SelectedIndex = config.BotDifficulty;
        KickIdleTimeTextBox.Text = config.KickIdleTime?.ToString() ?? string.Empty;

        // 高级选项 - 日志
        EnableLoggingCheckBox.IsChecked = config.EnableLogging;
        LogEchoCheckBox.IsChecked = config.LogEcho;
        ConsoleLogToFileCheckBox.IsChecked = config.ConsoleLogToFile;
    }

    /// <summary>
    /// 加载 RCON 配置到 UI 控件
    /// </summary>
    private void LoadRCONConfig(RCONConfig config)
    {
        RconEnabledCheckBox.IsChecked = config.Enabled;
        RconHostTextBox.Text = config.Host;
        RconPortTextBox.Text = config.Port.ToString();
        RconPasswordTextBox.Text = config.Password;
        RconTimeoutTextBox.Text = config.Timeout.ToString();
    }

    /// <summary>
    /// 从UI控件保存配置
    /// </summary>
    private bool SaveConfig()
    {
        try
        {
            // 基本配置
            Config.IpAddress = IpAddressTextBox.Text.Trim();
            
            if (!int.TryParse(PortTextBox.Text, out var port) || port < 1 || port > 65535)
            {
                MessageBox.Show("端口必须是 1-65535 之间的数字", "验证错误", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Config.Port = port;

            Config.Map = MapComboBox.Text.Trim();
            if (string.IsNullOrEmpty(Config.Map))
            {
                MessageBox.Show("请输入启动地图", "验证错误", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            
            if (!int.TryParse(MaxPlayersTextBox.Text, out var maxPlayers) || maxPlayers < 1 || maxPlayers > 64)
            {
                MessageBox.Show("最大玩家数必须是 1-64 之间的数字", "验证错误", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Config.MaxPlayers = maxPlayers;

            if (int.TryParse(TickRateComboBox.Text, out var tickRate))
            {
                Config.TickRate = tickRate;
            }

            Config.GameType = GameTypeComboBox.SelectedIndex;
            Config.GameMode = GameModeComboBox.SelectedIndex;

            // 服务器身份
            Config.ServerName = string.IsNullOrWhiteSpace(ServerNameTextBox.Text) 
                ? null 
                : ServerNameTextBox.Text.Trim();
            Config.SteamToken = string.IsNullOrWhiteSpace(SteamTokenTextBox.Text) 
                ? null 
                : SteamTokenTextBox.Text.Trim();
            Config.ServerPassword = string.IsNullOrWhiteSpace(ServerPasswordBox.Password) 
                ? null 
                : ServerPasswordBox.Password;

            // 高级选项 - 网络
            Config.IsLanMode = IsLanModeCheckBox.IsChecked == true;
            Config.InsecureMode = InsecureModeCheckBox.IsChecked == true;

            // 高级选项 - 性能
            Config.EnableConsole = EnableConsoleCheckBox.IsChecked == true;
            Config.DisableHltv = DisableHltvCheckBox.IsChecked == true;
            Config.ProcessPriority = ProcessPriorityComboBox.SelectedIndex switch
            {
                0 => "low",
                1 => "normal",
                2 => "high",
                _ => "normal"
            };

            Config.MaxFps = string.IsNullOrWhiteSpace(MaxFpsTextBox.Text) 
                ? null 
                : int.TryParse(MaxFpsTextBox.Text, out var maxFps) ? maxFps : null;
            
            Config.ThreadCount = string.IsNullOrWhiteSpace(ThreadCountTextBox.Text) 
                ? null 
                : int.TryParse(ThreadCountTextBox.Text, out var threads) ? threads : null;

            // 高级选项 - 游戏规则
            Config.EnableCheats = EnableCheatsCheckBox.IsChecked == true;
            
            if (!int.TryParse(BotQuotaTextBox.Text, out var botQuota) || botQuota < 0)
            {
                MessageBox.Show("BOT数量必须是非负数", "验证错误", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Config.BotQuota = botQuota;
            Config.BotDifficulty = BotDifficultyComboBox.SelectedIndex;

            Config.KickIdleTime = string.IsNullOrWhiteSpace(KickIdleTimeTextBox.Text) 
                ? null 
                : int.TryParse(KickIdleTimeTextBox.Text, out var kickTime) ? kickTime : null;

            // 高级选项 - 日志
            Config.EnableLogging = EnableLoggingCheckBox.IsChecked == true;
            Config.LogEcho = LogEchoCheckBox.IsChecked == true;
            Config.ConsoleLogToFile = ConsoleLogToFileCheckBox.IsChecked == true;

            // RCON 配置
            RCONConfig.Enabled = RconEnabledCheckBox.IsChecked == true;
            RCONConfig.Host = RconHostTextBox.Text.Trim();
            
            if (!int.TryParse(RconPortTextBox.Text, out var rconPort) || rconPort < 1 || rconPort > 65535)
            {
                MessageBox.Show("RCON 端口必须是 1-65535 之间的数字", "验证错误", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            RCONConfig.Port = rconPort;

            RCONConfig.Password = RconPasswordTextBox.Text.Trim();

            if (RCONConfig.Enabled && string.IsNullOrWhiteSpace(RCONConfig.Password))
            {
                var result = MessageBox.Show(
                    "启用 RCON 但未设置密码，连接将会失败！\n\n是否继续保存？",
                    "警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return false;
            }

            if (!int.TryParse(RconTimeoutTextBox.Text, out var rconTimeout) || rconTimeout < 1000)
            {
                MessageBox.Show("RCON 超时时间必须至少为 1000 毫秒", "验证错误", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            RCONConfig.Timeout = rconTimeout;

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置时出错：{ex.Message}", "错误", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (SaveConfig())
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 处理超链接导航
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接：{ex.Message}", "错误", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

