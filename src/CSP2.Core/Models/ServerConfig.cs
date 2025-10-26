namespace CSP2.Core.Models;

/// <summary>
/// 服务器配置
/// </summary>
public class ServerConfig
{
    // ============ 基础配置 ============
    
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

    // ============ 服务器身份 ============
    
    /// <summary>
    /// 服务器名称（显示在服务器浏览器中）
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
    /// Steam令牌（用于公开服务器）
    /// </summary>
    public string? SteamToken { get; set; }

    // ============ 网络设置 ============
    
    /// <summary>
    /// 局域网模式 (0=互联网, 1=局域网)
    /// </summary>
    public bool IsLanMode { get; set; } = false;

    /// <summary>
    /// 禁用VAC (Valve反外挂系统)
    /// </summary>
    public bool InsecureMode { get; set; } = false;

    // ============ 性能优化 ============
    
    /// <summary>
    /// 是否启用控制台
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// 进程优先级（-high, -normal, -low）
    /// </summary>
    public string ProcessPriority { get; set; } = "normal";

    /// <summary>
    /// 最大FPS
    /// </summary>
    public int? MaxFps { get; set; }

    /// <summary>
    /// 工作线程数 (留空自动检测)
    /// </summary>
    public int? ThreadCount { get; set; }

    /// <summary>
    /// 禁用HLTV/GOTV
    /// </summary>
    public bool DisableHltv { get; set; } = false;

    // ============ 游戏规则 ============
    
    /// <summary>
    /// 启用作弊命令
    /// </summary>
    public bool EnableCheats { get; set; } = false;

    /// <summary>
    /// BOT数量 (0表示无BOT)
    /// </summary>
    public int BotQuota { get; set; } = 0;

    /// <summary>
    /// BOT难度 (0=简单, 1=普通, 2=困难, 3=专家)
    /// </summary>
    public int BotDifficulty { get; set; } = 2;

    /// <summary>
    /// 自动踢出闲置玩家 (分钟)
    /// </summary>
    public int? KickIdleTime { get; set; }

    // ============ 日志设置 ============
    
    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 控制台日志输出到文件
    /// </summary>
    public bool ConsoleLogToFile { get; set; } = false;

    /// <summary>
    /// 启用日志回显
    /// </summary>
    public bool LogEcho { get; set; } = true;

    // ============ 自定义参数 ============
    
    /// <summary>
    /// 自定义启动参数
    /// </summary>
    public Dictionary<string, string> CustomArgs { get; set; } = new();
}

