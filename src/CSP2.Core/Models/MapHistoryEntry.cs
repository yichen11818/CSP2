namespace CSP2.Core.Models;

/// <summary>
/// Workshop 地图历史记录条目
/// </summary>
public class MapHistoryEntry
{
    /// <summary>
    /// Workshop ID (Steam Workshop 文件 ID)
    /// </summary>
    public string WorkshopId { get; set; } = string.Empty;

    /// <summary>
    /// 地图名称
    /// </summary>
    public string MapName { get; set; } = string.Empty;

    /// <summary>
    /// 地图作者 Steam ID
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// 地图作者名称
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// 地图描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 首次加载时间
    /// </summary>
    public DateTime FirstLoadedAt { get; set; }

    /// <summary>
    /// 最后加载时间
    /// </summary>
    public DateTime LastLoadedAt { get; set; }

    /// <summary>
    /// 加载次数
    /// </summary>
    public int LoadCount { get; set; }

    /// <summary>
    /// 预览图本地路径
    /// </summary>
    public string PreviewImagePath { get; set; } = string.Empty;

    /// <summary>
    /// 预览图 URL (Steam CDN)
    /// </summary>
    public string PreviewImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小 (字节)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 创建时间 (Unix timestamp)
    /// </summary>
    public long TimeCreated { get; set; }

    /// <summary>
    /// 更新时间 (Unix timestamp)
    /// </summary>
    public long TimeUpdated { get; set; }

    /// <summary>
    /// Workshop 页面 URL
    /// </summary>
    public string WorkshopUrl => $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopId}";

    /// <summary>
    /// 是否已下载预览图
    /// </summary>
    public bool HasPreviewImage => !string.IsNullOrEmpty(PreviewImagePath) && File.Exists(PreviewImagePath);
}

