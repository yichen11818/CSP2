using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 地图历史服务接口
/// </summary>
public interface IMapHistoryService
{
    /// <summary>
    /// 获取所有地图历史记录
    /// </summary>
    Task<List<MapHistoryEntry>> GetAllEntriesAsync();

    /// <summary>
    /// 获取指定服务器的地图历史记录
    /// </summary>
    /// <param name="serverId">服务器 ID</param>
    Task<List<MapHistoryEntry>> GetServerEntriesAsync(string serverId);

    /// <summary>
    /// 添加或更新地图历史记录
    /// </summary>
    /// <param name="serverId">服务器 ID</param>
    /// <param name="entry">地图历史条目</param>
    Task AddOrUpdateEntryAsync(string serverId, MapHistoryEntry entry);

    /// <summary>
    /// 记录地图加载（自动创建或更新记录）
    /// </summary>
    /// <param name="serverId">服务器 ID</param>
    /// <param name="workshopId">Workshop ID</param>
    Task RecordMapLoadAsync(string serverId, string workshopId);

    /// <summary>
    /// 删除地图历史记录
    /// </summary>
    /// <param name="serverId">服务器 ID</param>
    /// <param name="workshopId">Workshop ID</param>
    Task<bool> DeleteEntryAsync(string serverId, string workshopId);

    /// <summary>
    /// 清空指定服务器的所有地图历史
    /// </summary>
    /// <param name="serverId">服务器 ID</param>
    Task ClearServerHistoryAsync(string serverId);

    /// <summary>
    /// 清空所有地图历史
    /// </summary>
    Task ClearAllHistoryAsync();
}

