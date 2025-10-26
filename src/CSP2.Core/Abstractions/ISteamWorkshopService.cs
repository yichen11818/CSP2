using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// Steam Workshop 服务接口
/// 提供与 Steam Workshop API 交互的功能
/// </summary>
public interface ISteamWorkshopService
{
    /// <summary>
    /// 从 Steam Workshop 获取地图信息
    /// </summary>
    /// <param name="workshopId">Workshop ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>地图历史条目（包含地图信息）</returns>
    Task<MapHistoryEntry?> GetMapInfoAsync(string workshopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载地图预览图
    /// </summary>
    /// <param name="previewUrl">预览图 URL</param>
    /// <param name="workshopId">Workshop ID (用于命名文件)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本地预览图路径</returns>
    Task<string?> DownloadPreviewImageAsync(string previewUrl, string workshopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从 URL 解析 Workshop ID
    /// </summary>
    /// <param name="url">Steam Workshop URL 或纯 ID</param>
    /// <returns>Workshop ID，解析失败返回 null</returns>
    string? ParseWorkshopId(string url);
}

