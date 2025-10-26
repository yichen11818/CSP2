using CSP2.Core.Models;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

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
                $"路径 '{InstallPath}' 不存在。\n\n是否仍要添加?",
                "路径不存在",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return;
            }
        }

        // ========== 基本配置 ==========
        
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
        if (TickRateComboBox.SelectedItem is ComboBoxItem tickRateItem)
        {
            int.TryParse(tickRateItem.Content.ToString(), out tickRate);
        }

        // ========== 性能选项 ==========
        
        // 最大FPS (可选)
        int? maxFps = null;
        if (!string.IsNullOrWhiteSpace(MaxFpsTextBox.Text))
        {
            if (int.TryParse(MaxFpsTextBox.Text, out int fps) && fps > 0)
            {
                maxFps = fps;
            }
            else
            {
                MessageBox.Show("最大FPS必须是大于0的整数", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxFpsTextBox.Focus();
                return;
            }
        }

        // 线程数 (可选)
        int? threadCount = null;
        if (!string.IsNullOrWhiteSpace(ThreadCountTextBox.Text))
        {
            if (int.TryParse(ThreadCountTextBox.Text, out int threads) && threads > 0)
            {
                threadCount = threads;
            }
            else
            {
                MessageBox.Show("线程数必须是大于0的整数", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                ThreadCountTextBox.Focus();
                return;
            }
        }

        // 进程优先级
        string processPriority = "normal";
        if (ProcessPriorityComboBox.SelectedItem is ComboBoxItem priorityItem && priorityItem.Tag != null)
        {
            processPriority = priorityItem.Tag.ToString() ?? "normal";
        }

        // ========== 游戏规则 ==========
        
        // BOT数量
        int botQuota = 0;
        if (!string.IsNullOrWhiteSpace(BotQuotaTextBox.Text))
        {
            if (!int.TryParse(BotQuotaTextBox.Text, out botQuota) || botQuota < 0 || botQuota > 64)
            {
                MessageBox.Show("BOT数量必须是0-64之间的整数", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                BotQuotaTextBox.Focus();
                return;
            }
        }

        // BOT难度
        int botDifficulty = BotDifficultyComboBox.SelectedIndex;

        // 踢出闲置时间 (可选)
        int? kickIdleTime = null;
        if (!string.IsNullOrWhiteSpace(KickIdleTimeTextBox.Text))
        {
            if (int.TryParse(KickIdleTimeTextBox.Text, out int idleTime) && idleTime > 0)
            {
                kickIdleTime = idleTime;
            }
            else
            {
                MessageBox.Show("踢出闲置时间必须是大于0的整数（分钟）", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                KickIdleTimeTextBox.Focus();
                return;
            }
        }

        // 构建配置
        ServerConfig = new ServerConfig
        {
            // 基础配置
            IpAddress = IpAddressTextBox.Text,
            Port = port,
            Map = MapTextBox.Text,
            MaxPlayers = maxPlayers,
            TickRate = tickRate,
            MapGroup = MapGroupTextBox.Text,

            // 服务器身份
            ServerName = string.IsNullOrWhiteSpace(ServerDisplayNameTextBox.Text) ? null : ServerDisplayNameTextBox.Text,
            SteamToken = string.IsNullOrWhiteSpace(SteamTokenTextBox.Text) ? null : SteamTokenTextBox.Text,
            ServerPassword = string.IsNullOrWhiteSpace(ServerPasswordBox.Password) ? null : ServerPasswordBox.Password,
            RconPassword = string.IsNullOrWhiteSpace(RconPasswordBox.Password) ? null : RconPasswordBox.Password,

            // 网络设置
            IsLanMode = LanModeCheckBox.IsChecked == true,
            InsecureMode = InsecureModeCheckBox.IsChecked == true,

            // 性能优化
            EnableConsole = EnableConsoleCheckBox.IsChecked == true,
            ProcessPriority = processPriority,
            MaxFps = maxFps,
            ThreadCount = threadCount,
            DisableHltv = DisableHltvCheckBox.IsChecked == true,

            // 游戏规则
            EnableCheats = EnableCheatsCheckBox.IsChecked == true,
            BotQuota = botQuota,
            BotDifficulty = botDifficulty,
            KickIdleTime = kickIdleTime,

            // 日志设置
            EnableLogging = EnableLoggingCheckBox.IsChecked == true,
            ConsoleLogToFile = ConsoleLogToFileCheckBox.IsChecked == true,
            LogEcho = LogEchoCheckBox.IsChecked == true
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
