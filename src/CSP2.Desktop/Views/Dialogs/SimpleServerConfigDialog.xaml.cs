using CSP2.Core.Logging;
using CSP2.Core.Models;
using System.Windows;

namespace CSP2.Desktop.Views.Dialogs;

/// <summary>
/// 简化版服务器配置对话框 - 仅支持启动参数输入
/// </summary>
public partial class SimpleServerConfigDialog : Window
{
    public ServerConfig ServerConfig { get; private set; }

    public SimpleServerConfigDialog(ServerConfig? config = null)
    {
        InitializeComponent();

        ServerConfig = config ?? new ServerConfig();
        
        // 如果提供了配置，则加载现有配置的启动参数
        if (config != null)
        {
            LoadConfig(config);
        }
        else
        {
            // 新配置，显示默认启动参数
            LoadDefaultArgs();
        }
    }

    private void LoadConfig(ServerConfig config)
    {
        // 优先使用用户手动编辑的完整参数
        if (!string.IsNullOrEmpty(config.UserEditedFullArgs))
        {
            LaunchArgsTextBox.Text = config.UserEditedFullArgs;
        }
        else
        {
            // 从配置生成启动参数
            LoadDefaultArgs();
        }
    }

    private void LoadDefaultArgs()
    {
        // 显示默认的启动参数作为示例
        var defaultArgs = new List<string>
        {
            "-dedicated",
            "-console",
            "-ip 0.0.0.0",
            "-port 27015",
            "-maxplayers 10",
            "-tickrate 128",
            "+game_type 0",
            "+game_mode 1",
            "+mapgroup mg_active",
            "+map de_dust2"
        };
        
        LaunchArgsTextBox.Text = string.Join("\n", defaultArgs);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var argsText = LaunchArgsTextBox.Text.Trim();
        
        // 如果用户输入了启动参数，保存它们
        if (!string.IsNullOrWhiteSpace(argsText))
        {
            // 保存用户输入的完整启动参数
            ServerConfig.UserEditedFullArgs = argsText;
            
            // 尝试从启动参数中解析基本信息（端口等）以便在列表中显示
            TryParseBasicInfo(argsText);
        }
        else
        {
            // 如果留空，使用默认配置
            ServerConfig.UserEditedFullArgs = null;
            ApplyDefaultConfig();
        }

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// 尝试从启动参数中解析基本信息（用于列表显示）
    /// </summary>
    private void TryParseBasicInfo(string args)
    {
        var lines = args.Split(new[] { '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // 解析端口
            if (line == "-port" && i + 1 < lines.Length)
            {
                if (int.TryParse(lines[i + 1], out int port))
                    ServerConfig.Port = port;
            }
            // 解析地图
            else if (line == "+map" && i + 1 < lines.Length)
            {
                ServerConfig.Map = lines[i + 1].Trim();
            }
            // 解析最大玩家数
            else if (line == "-maxplayers" && i + 1 < lines.Length)
            {
                if (int.TryParse(lines[i + 1], out int maxPlayers))
                    ServerConfig.MaxPlayers = maxPlayers;
            }
            // 解析Tick Rate
            else if (line == "-tickrate" && i + 1 < lines.Length)
            {
                if (int.TryParse(lines[i + 1], out int tickRate))
                    ServerConfig.TickRate = tickRate;
            }
            // 解析IP地址
            else if (line == "-ip" && i + 1 < lines.Length)
            {
                ServerConfig.IpAddress = lines[i + 1].Trim();
            }
        }
    }

    /// <summary>
    /// 应用默认配置
    /// </summary>
    private void ApplyDefaultConfig()
    {
        ServerConfig.IpAddress = "0.0.0.0";
        ServerConfig.Port = 27015;
        ServerConfig.Map = "de_dust2";
        ServerConfig.MaxPlayers = 10;
        ServerConfig.TickRate = 128;
        ServerConfig.GameMode = 1;
        ServerConfig.GameType = 0;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

