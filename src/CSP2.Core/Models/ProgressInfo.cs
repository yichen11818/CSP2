namespace CSP2.Core.Models;

/// <summary>
/// 进度信息基类
/// </summary>
public class ProgressInfo
{
    /// <summary>
    /// 进度百分比（0-100）
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// 当前状态描述
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 下载进度信息
/// </summary>
public class DownloadProgress : ProgressInfo
{
    /// <summary>
    /// 已下载字节数
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// 总字节数
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// 下载速度（字节/秒）
    /// </summary>
    public long BytesPerSecond { get; set; }
}

/// <summary>
/// 安装进度信息
/// </summary>
public class InstallProgress : ProgressInfo
{
    /// <summary>
    /// 当前步骤
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// 总步骤数
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// 当前步骤索引（从1开始）
    /// </summary>
    public int CurrentStepIndex { get; set; }
}

