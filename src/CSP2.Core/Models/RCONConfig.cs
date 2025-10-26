namespace CSP2.Core.Models;

/// <summary>
/// RCON 连接配置
/// 用于远程连接服务器（可能与本地服务器不同）
/// </summary>
public class RCONConfig
{
    /// <summary>
    /// 是否启用 RCON 连接
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// RCON 服务器地址（默认 localhost）
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// RCON 端口（默认与服务器端口相同）
    /// </summary>
    public int Port { get; set; } = 27015;

    /// <summary>
    /// RCON 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int Timeout { get; set; } = 5000;
}

