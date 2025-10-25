namespace CSP2.Core.Models;

/// <summary>
/// 服务器配置
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 27015;

    /// <summary>
    /// 服务器IP地址（通常为0.0.0.0表示监听所有网卡）
    /// </summary>
    public string IpAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// 启动地图
    /// </summary>
    public string Map { get; set; } = "de_dust2";

    /// <summary>
    /// 地图组（mapgroup）
    /// </summary>
    public string MapGroup { get; set; } = "mg_active";

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int MaxPlayers { get; set; } = 10;

    /// <summary>
    /// 游戏类型 (0=经典竞技, 1=军备竞赛, 2=休闲)
    /// </summary>
    public int GameType { get; set; } = 0;

    /// <summary>
    /// 游戏模式 (0=休闲, 1=竞技, 2=翼人, 3=武器大师)
    /// </summary>
    public int GameMode { get; set; } = 1;

    /// <summary>
    /// Tick速率 (64或128)
    /// </summary>
    public int TickRate { get; set; } = 128;

    /// <summary>
    /// Steam令牌（用于公开服务器）
    /// </summary>
    public string? SteamToken { get; set; }

    /// <summary>
    /// 服务器名称
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// 服务器密码
    /// </summary>
    public string? ServerPassword { get; set; }

    /// <summary>
    /// RCON密码（远程控制密码）
    /// </summary>
    public string? RconPassword { get; set; }

    /// <summary>
    /// 是否启用控制台
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// 进程优先级（-high, -normal, -low）
    /// </summary>
    public string ProcessPriority { get; set; } = "-high";

    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 自定义启动参数
    /// </summary>
    public Dictionary<string, string> CustomArgs { get; set; } = new();
}

