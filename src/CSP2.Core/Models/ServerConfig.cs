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
    /// 启动地图
    /// </summary>
    public string Map { get; set; } = "de_dust2";

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int MaxPlayers { get; set; } = 10;

    /// <summary>
    /// 游戏类型
    /// </summary>
    public int GameType { get; set; } = 0;

    /// <summary>
    /// 游戏模式
    /// </summary>
    public int GameMode { get; set; } = 1;

    /// <summary>
    /// Tick速率
    /// </summary>
    public int TickRate { get; set; } = 128;

    /// <summary>
    /// Steam令牌（用于公开服务器）
    /// </summary>
    public string? SteamToken { get; set; }

    /// <summary>
    /// 自定义启动参数
    /// </summary>
    public Dictionary<string, string> CustomArgs { get; set; } = new();
}

