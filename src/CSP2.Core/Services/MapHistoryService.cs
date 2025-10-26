using System.Text.Json;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// 地图历史服务实现
/// 管理 Workshop 地图加载历史的存储和检索
/// </summary>
public class MapHistoryService : IMapHistoryService
{
    private readonly ISteamWorkshopService _workshopService;
    private readonly ILogger<MapHistoryService> _logger;
    private readonly string _historyFilePath = "data/map_history.json";
    private Dictionary<string, List<MapHistoryEntry>> _historyData = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public MapHistoryService(
        ISteamWorkshopService workshopService,
        ILogger<MapHistoryService> logger)
    {
        _workshopService = workshopService;
        _logger = logger;

        // 确保数据目录存在
        var directory = Path.GetDirectoryName(_historyFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 加载历史数据
        _ = LoadHistoryAsync();
    }

    /// <summary>
    /// 获取所有地图历史记录
    /// </summary>
    public async Task<List<MapHistoryEntry>> GetAllEntriesAsync()
    {
        await LoadHistoryAsync();
        return _historyData.Values.SelectMany(x => x).ToList();
    }

    /// <summary>
    /// 获取指定服务器的地图历史记录
    /// </summary>
    public async Task<List<MapHistoryEntry>> GetServerEntriesAsync(string serverId)
    {
        await LoadHistoryAsync();
        
        if (_historyData.TryGetValue(serverId, out var entries))
        {
            return entries.OrderByDescending(e => e.LastLoadedAt).ToList();
        }

        return new List<MapHistoryEntry>();
    }

    /// <summary>
    /// 添加或更新地图历史记录
    /// </summary>
    public async Task AddOrUpdateEntryAsync(string serverId, MapHistoryEntry entry)
    {
        await LoadHistoryAsync();

        if (!_historyData.ContainsKey(serverId))
        {
            _historyData[serverId] = new List<MapHistoryEntry>();
        }

        var existingEntry = _historyData[serverId]
            .FirstOrDefault(e => e.WorkshopId == entry.WorkshopId);

        if (existingEntry != null)
        {
            // 更新现有记录
            existingEntry.LastLoadedAt = entry.LastLoadedAt;
            existingEntry.LoadCount++;
            
            // 更新其他可能变化的字段
            if (!string.IsNullOrEmpty(entry.MapName))
                existingEntry.MapName = entry.MapName;
            if (!string.IsNullOrEmpty(entry.PreviewImagePath))
                existingEntry.PreviewImagePath = entry.PreviewImagePath;
            if (!string.IsNullOrEmpty(entry.PreviewImageUrl))
                existingEntry.PreviewImageUrl = entry.PreviewImageUrl;

            _logger.LogDebug("更新地图记录: {MapName} ({WorkshopId}), 加载次数: {Count}",
                existingEntry.MapName, entry.WorkshopId, existingEntry.LoadCount);
        }
        else
        {
            // 添加新记录
            _historyData[serverId].Add(entry);
            _logger.LogInformation("添加地图记录: {MapName} ({WorkshopId})",
                entry.MapName, entry.WorkshopId);
        }

        await SaveHistoryAsync();
    }

    /// <summary>
    /// 记录地图加载（带 Steam API 查询）
    /// </summary>
    public async Task RecordMapLoadAsync(string serverId, string workshopId)
    {
        try
        {
            _logger.LogDebug("开始记录地图加载: ServerId={ServerId}, WorkshopId={WorkshopId}",
                serverId, workshopId);

            // 检查是否已存在
            await LoadHistoryAsync();
            var existing = _historyData.TryGetValue(serverId, out var entries)
                ? entries.FirstOrDefault(e => e.WorkshopId == workshopId)
                : null;

            if (existing != null)
            {
                // 已存在，只更新加载信息
                existing.LastLoadedAt = DateTime.Now;
                existing.LoadCount++;
                
                _logger.LogDebug("更新现有地图记录: {MapName} (加载次数: {Count})",
                    existing.MapName, existing.LoadCount);
                
                await SaveHistoryAsync();
                return;
            }

            // 不存在，从 Steam 获取信息
            _logger.LogDebug("从 Steam API 获取地图信息...");
            var mapInfo = await _workshopService.GetMapInfoAsync(workshopId);

            if (mapInfo == null)
            {
                _logger.LogWarning("无法从 Steam 获取地图信息，使用默认信息: {WorkshopId}", workshopId);
                
                // 创建默认条目
                mapInfo = new MapHistoryEntry
                {
                    WorkshopId = workshopId,
                    MapName = $"Workshop Map {workshopId}",
                    FirstLoadedAt = DateTime.Now,
                    LastLoadedAt = DateTime.Now,
                    LoadCount = 1
                };
            }

            // 下载预览图
            if (!string.IsNullOrEmpty(mapInfo.PreviewImageUrl))
            {
                _logger.LogDebug("下载地图预览图...");
                var previewPath = await _workshopService.DownloadPreviewImageAsync(
                    mapInfo.PreviewImageUrl, workshopId);
                
                if (!string.IsNullOrEmpty(previewPath))
                {
                    mapInfo.PreviewImagePath = previewPath;
                    _logger.LogDebug("预览图已保存: {Path}", previewPath);
                }
            }

            // 保存记录
            await AddOrUpdateEntryAsync(serverId, mapInfo);

            _logger.LogInformation("成功记录地图加载: {MapName} ({WorkshopId})",
                mapInfo.MapName, workshopId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录地图加载失败: ServerId={ServerId}, WorkshopId={WorkshopId}",
                serverId, workshopId);
        }
    }

    /// <summary>
    /// 删除地图历史记录
    /// </summary>
    public async Task<bool> DeleteEntryAsync(string serverId, string workshopId)
    {
        await LoadHistoryAsync();

        if (!_historyData.TryGetValue(serverId, out var entries))
        {
            return false;
        }

        var entry = entries.FirstOrDefault(e => e.WorkshopId == workshopId);
        if (entry == null)
        {
            return false;
        }

        entries.Remove(entry);
        
        // 如果有预览图，删除文件
        if (!string.IsNullOrEmpty(entry.PreviewImagePath) && File.Exists(entry.PreviewImagePath))
        {
            try
            {
                File.Delete(entry.PreviewImagePath);
                _logger.LogDebug("已删除预览图: {Path}", entry.PreviewImagePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除预览图失败: {Path}", entry.PreviewImagePath);
            }
        }

        await SaveHistoryAsync();
        _logger.LogInformation("已删除地图记录: {MapName} ({WorkshopId})",
            entry.MapName, workshopId);

        return true;
    }

    /// <summary>
    /// 清空指定服务器的所有地图历史
    /// </summary>
    public async Task ClearServerHistoryAsync(string serverId)
    {
        await LoadHistoryAsync();

        if (_historyData.TryGetValue(serverId, out var entries))
        {
            // 删除所有预览图
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.PreviewImagePath) && File.Exists(entry.PreviewImagePath))
                {
                    try
                    {
                        File.Delete(entry.PreviewImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除预览图失败: {Path}", entry.PreviewImagePath);
                    }
                }
            }

            _historyData.Remove(serverId);
            await SaveHistoryAsync();
            _logger.LogInformation("已清空服务器地图历史: {ServerId}", serverId);
        }
    }

    /// <summary>
    /// 清空所有地图历史
    /// </summary>
    public async Task ClearAllHistoryAsync()
    {
        await LoadHistoryAsync();

        // 删除所有预览图
        foreach (var entries in _historyData.Values)
        {
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.PreviewImagePath) && File.Exists(entry.PreviewImagePath))
                {
                    try
                    {
                        File.Delete(entry.PreviewImagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除预览图失败: {Path}", entry.PreviewImagePath);
                    }
                }
            }
        }

        _historyData.Clear();
        await SaveHistoryAsync();
        _logger.LogInformation("已清空所有地图历史");
    }

    /// <summary>
    /// 从文件加载历史数据
    /// </summary>
    private async Task LoadHistoryAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                _historyData = new Dictionary<string, List<MapHistoryEntry>>();
                return;
            }

            var json = await File.ReadAllTextAsync(_historyFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _historyData = JsonSerializer.Deserialize<Dictionary<string, List<MapHistoryEntry>>>(json, options)
                ?? new Dictionary<string, List<MapHistoryEntry>>();
            
            _logger.LogDebug("已加载地图历史: {Count} 个服务器", _historyData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载地图历史失败");
            _historyData = new Dictionary<string, List<MapHistoryEntry>>();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// 保存历史数据到文件
    /// </summary>
    private async Task SaveHistoryAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_historyData, options);
            await File.WriteAllTextAsync(_historyFilePath, json);
            
            _logger.LogDebug("已保存地图历史");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存地图历史失败");
        }
        finally
        {
            _fileLock.Release();
        }
    }
}

