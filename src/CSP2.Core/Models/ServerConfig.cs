using System.Text.Json.Serialization;

namespace CSP2.Core.Models;

/// <summary>
/// 服务器配置（简化版）
/// </summary>
public class ServerConfig
{
    // ============ 核心配置（必填）============
    
    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 27015;

    /// <summary>
    /// 启动地图
    /// </summary>
    public string Map { get; set; } = "de_dust2";

    /// <summary>
    /// 最大玩家数 (1-64)
    /// </summary>
    public int MaxPlayers { get; set; } = 10;

    /// <summary>
    /// 游戏模式 (0=休闲, 1=竞技, 2=翼人, 3=武器大师)
    /// </summary>
    public int GameMode { get; set; } = 1;

    /// <summary>
    /// 游戏类型 (0=经典, 1=军备竞赛, 2=死亡竞赛)
    /// </summary>
    public int GameType { get; set; } = 0;

    // ============ 常用选项（可选）============
    
    /// <summary>
    /// Tick速率 (64或128)
    /// </summary>
    public int TickRate { get; set; } = 128;
    
    /// <summary>
    /// 禁用 BOT
    /// </summary>
    public bool DisableBots { get; set; } = false;
    
    /// <summary>
    /// Insecure 模式（用于自定义地图，禁用VAC）
    /// </summary>
    public bool InsecureMode { get; set; } = false;
    
    /// <summary>
    /// 局域网模式
    /// </summary>
    public bool IsLanMode { get; set; } = false;
    
    /// <summary>
    /// 在应用内打开控制台（捕获服务器日志到应用内）
    /// </summary>
    public bool OpenConsoleInApp { get; set; } = true;
    
    // ============ 网络配置 ============
    
    /// <summary>
    /// 服务器IP地址（默认 0.0.0.0 监听所有网卡）
    /// </summary>
    public string IpAddress { get; set; } = "0.0.0.0";
    
    // ============ 自定义参数 ============
    
    /// <summary>
    /// 自定义启动参数（原始字符串）
    /// 例如: "-high +sv_cheats 1 +exec custom.cfg"
    /// </summary>
    public string CustomParameters { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户手动编辑的完整启动参数（如果用户通过UI手动编辑了启动参数，会保存到这里）
    /// 如果此字段有值，将优先使用它而不是自动生成参数
    /// </summary>
    public string? UserEditedFullArgs { get; set; } = null;
    
    // ============ 内部使用（不暴露给用户UI）============
    
    /// <summary>
    /// 地图组（自动推断，固定为 mg_active）
    /// </summary>
    [JsonIgnore]
    public string MapGroup => "mg_active";

    // ============ 高级配置（可选，建议使用 autoexec.cfg）============
    
    /// <summary>
    /// 服务器名称（显示在服务器浏览器中）
    /// 建议：在 autoexec.cfg 中使用 hostname "服务器名" 配置
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// 服务器密码
    /// 建议：在 autoexec.cfg 中使用 sv_password "密码" 配置
    /// </summary>
    public string? ServerPassword { get; set; }

    /// <summary>
    /// RCON密码（远程控制密码）
    /// 建议：在 autoexec.cfg 中使用 rcon_password "密码" 配置
    /// </summary>
    public string? RconPassword { get; set; }

    /// <summary>
    /// Steam令牌（用于公开服务器）
    /// 建议：在启动参数中使用 +sv_setsteamaccount TOKEN 或在 autoexec.cfg 中配置
    /// </summary>
    public string? SteamToken { get; set; }

    // ============ 向后兼容（已废弃的属性，保留以支持旧配置）============
    
    /// <summary>
    /// [已废弃] 是否启用控制台（现在默认总是启用）
    /// </summary>
    [Obsolete("控制台现在默认总是启用，请使用 CustomParameters 添加 -console")]
    [JsonIgnore]
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// [已废弃] 进程优先级（请使用 CustomParameters 添加 -high/-low）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 -high/-normal/-low")]
    [JsonIgnore]
    public string ProcessPriority { get; set; } = "normal";

    /// <summary>
    /// [已废弃] 最大FPS（请使用 CustomParameters 添加 +fps_max N）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 +fps_max N")]
    [JsonIgnore]
    public int? MaxFps { get; set; }

    /// <summary>
    /// [已废弃] 工作线程数（请使用 CustomParameters 添加 -threads N）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 -threads N")]
    [JsonIgnore]
    public int? ThreadCount { get; set; }

    /// <summary>
    /// [已废弃] 禁用HLTV/GOTV（请使用 CustomParameters 添加 +tv_enable 0）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 +tv_enable 0")]
    [JsonIgnore]
    public bool DisableHltv { get; set; } = false;

    /// <summary>
    /// [已废弃] 启用作弊命令（请使用 CustomParameters 添加 +sv_cheats 1）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 +sv_cheats 1")]
    [JsonIgnore]
    public bool EnableCheats { get; set; } = false;

    /// <summary>
    /// [已废弃] BOT数量（请使用 CustomParameters 添加 +bot_quota N）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 +bot_quota N")]
    [JsonIgnore]
    public int BotQuota { get; set; } = 0;

    /// <summary>
    /// [已废弃] BOT难度（请使用 CustomParameters 添加 +bot_difficulty N）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 +bot_difficulty N")]
    [JsonIgnore]
    public int BotDifficulty { get; set; } = 2;

    /// <summary>
    /// [已废弃] 自动踢出闲置玩家（请在 autoexec.cfg 中配置）
    /// </summary>
    [Obsolete("请在 autoexec.cfg 中使用 mp_autokick 配置")]
    [JsonIgnore]
    public int? KickIdleTime { get; set; }

    /// <summary>
    /// [已废弃] 是否启用日志（现在默认总是启用）
    /// </summary>
    [Obsolete("日志现在默认总是启用")]
    [JsonIgnore]
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// [已废弃] 控制台日志输出到文件（请使用 CustomParameters 添加 +con_logfile 1）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 +con_logfile 1")]
    [JsonIgnore]
    public bool ConsoleLogToFile { get; set; } = false;

    /// <summary>
    /// [已废弃] 启用日志回显（请使用 CustomParameters 添加 +sv_logecho 1）
    /// </summary>
    [Obsolete("请使用 CustomParameters 添加 +sv_logecho 1")]
    [JsonIgnore]
    public bool LogEcho { get; set; } = true;

    /// <summary>
    /// [已废弃] 自定义启动参数字典（已被 CustomParameters 字符串替代）
    /// </summary>
    [Obsolete("请使用 CustomParameters 字符串")]
    [JsonIgnore]
    public Dictionary<string, string> CustomArgs { get; set; } = new();

    /// <summary>
    /// 快捷命令列表
    /// </summary>
    public List<string> QuickCommands { get; set; } = new();
}

