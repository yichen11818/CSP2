namespace CSP2.Core.Models;

/// <summary>
/// 下载任务模型
/// </summary>
public class DownloadTask
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 任务名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 任务类型
    /// </summary>
    public DownloadTaskType TaskType { get; set; }

    /// <summary>
    /// 下载进度 (0-100)
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// 下载速度 (字节/秒)
    /// </summary>
    public long Speed { get; set; }

    /// <summary>
    /// 总大小 (字节)
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    /// 已下载大小 (字节)
    /// </summary>
    public long DownloadedSize { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public DownloadTaskStatus Status { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// 日志消息
    /// </summary>
    public List<string> LogMessages { get; set; } = new();

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 下载任务类型
/// </summary>
public enum DownloadTaskType
{
    /// <summary>
    /// SteamCMD下载
    /// </summary>
    SteamCmd,

    /// <summary>
    /// 插件下载
    /// </summary>
    Plugin,

    /// <summary>
    /// 框架下载
    /// </summary>
    Framework,

    /// <summary>
    /// 更新下载
    /// </summary>
    Update,

    /// <summary>
    /// 其他
    /// </summary>
    Other
}

/// <summary>
/// 下载任务状态
/// </summary>
public enum DownloadTaskStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending,

    /// <summary>
    /// 下载中
    /// </summary>
    Downloading,

    /// <summary>
    /// 暂停
    /// </summary>
    Paused,

    /// <summary>
    /// 完成
    /// </summary>
    Completed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 取消
    /// </summary>
    Cancelled
}

