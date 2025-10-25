namespace CSP2.Core.Models;

/// <summary>
/// 服务器实体
/// </summary>
public class Server
{
    /// <summary>
    /// 唯一标识（GUID）
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 服务器名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 服务器安装路径
    /// </summary>
    public required string InstallPath { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public ServerStatus Status { get; set; } = ServerStatus.Stopped;

    /// <summary>
    /// 配置信息
    /// </summary>
    public ServerConfig Config { get; set; } = new();

    /// <summary>
    /// 已安装的框架列表
    /// </summary>
    public List<InstalledFramework> Frameworks { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后启动时间
    /// </summary>
    public DateTime? LastStartedAt { get; set; }
}

/// <summary>
/// 已安装的框架信息
/// </summary>
public class InstalledFramework
{
    /// <summary>
    /// 框架ID
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 已安装版本
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// 安装时间
    /// </summary>
    public DateTime InstalledAt { get; set; } = DateTime.Now;
}

