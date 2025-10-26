using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// Steam Workshop 服务实现
/// </summary>
public class SteamWorkshopService : ISteamWorkshopService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SteamWorkshopService> _logger;
    private const string SteamApiUrl = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
    private const string PreviewsDirectory = "data/workshop_previews";

    public SteamWorkshopService(HttpClient httpClient, ILogger<SteamWorkshopService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // 确保预览图目录存在
        if (!Directory.Exists(PreviewsDirectory))
        {
            Directory.CreateDirectory(PreviewsDirectory);
        }
    }

    /// <summary>
    /// 从 Steam API 获取地图信息
    /// </summary>
    public async Task<MapHistoryEntry?> GetMapInfoAsync(string workshopId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("正在获取 Workshop 地图信息: {WorkshopId}", workshopId);

            // 构建请求
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "itemcount", "1" },
                { "publishedfileids[0]", workshopId }
            });

            // 发送请求
            var response = await _httpClient.PostAsync(SteamApiUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            // 解析响应
            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(jsonString);

            // 检查响应结构
            if (!document.RootElement.TryGetProperty("response", out var responseElement) ||
                !responseElement.TryGetProperty("publishedfiledetails", out var details) ||
                details.GetArrayLength() == 0)
            {
                _logger.LogWarning("Steam API 返回空数据: {WorkshopId}", workshopId);
                return null;
            }

            var mapInfo = details[0];

            // 检查是否成功获取
            if (mapInfo.TryGetProperty("result", out var result) && result.GetInt32() != 1)
            {
                _logger.LogWarning("Steam API 返回错误: WorkshopId={WorkshopId}, Result={Result}", 
                    workshopId, result.GetInt32());
                return null;
            }

            // 解析地图信息
            var entry = new MapHistoryEntry
            {
                WorkshopId = workshopId,
                MapName = GetStringProperty(mapInfo, "title") ?? "Unknown Map",
                AuthorId = GetStringProperty(mapInfo, "creator") ?? "",
                Description = GetStringProperty(mapInfo, "description") ?? "",
                PreviewImageUrl = GetStringProperty(mapInfo, "preview_url") ?? "",
                FileSize = GetLongProperty(mapInfo, "file_size"),
                TimeCreated = GetLongProperty(mapInfo, "time_created"),
                TimeUpdated = GetLongProperty(mapInfo, "time_updated"),
                FirstLoadedAt = DateTime.Now,
                LastLoadedAt = DateTime.Now,
                LoadCount = 1
            };

            _logger.LogInformation("成功获取地图信息: {MapName} ({WorkshopId})", entry.MapName, workshopId);
            return entry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Steam API 请求失败: {WorkshopId}", workshopId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析 Steam API 响应失败: {WorkshopId}", workshopId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取地图信息失败: {WorkshopId}", workshopId);
            return null;
        }
    }

    /// <summary>
    /// 下载地图预览图
    /// </summary>
    public async Task<string?> DownloadPreviewImageAsync(string previewUrl, string workshopId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(previewUrl))
        {
            _logger.LogWarning("预览图 URL 为空: {WorkshopId}", workshopId);
            return null;
        }

        try
        {
            var fileName = $"preview_{workshopId}.jpg";
            var filePath = Path.Combine(PreviewsDirectory, fileName);

            // 如果文件已存在，直接返回
            if (File.Exists(filePath))
            {
                _logger.LogDebug("预览图已存在: {FilePath}", filePath);
                return filePath;
            }

            _logger.LogDebug("正在下载预览图: {Url}", previewUrl);

            // 下载图片
            var response = await _httpClient.GetAsync(previewUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            // 保存到文件
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("预览图下载成功: {FilePath}", filePath);
            return filePath;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "下载预览图失败: {Url}", previewUrl);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "保存预览图失败: {WorkshopId}", workshopId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载预览图时发生未知错误: {WorkshopId}", workshopId);
            return null;
        }
    }

    /// <summary>
    /// 从 URL 或纯文本解析 Workshop ID
    /// </summary>
    public string? ParseWorkshopId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // 如果是纯数字，直接返回
        if (Regex.IsMatch(input, @"^\d+$"))
        {
            return input;
        }

        // 尝试从 URL 解析
        // 支持格式:
        // - https://steamcommunity.com/sharedfiles/filedetails/?id=123456
        // - https://steamcommunity.com/workshop/filedetails/?id=123456
        var match = Regex.Match(input, @"[?&]id=(\d+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        _logger.LogWarning("无法解析 Workshop ID: {Input}", input);
        return null;
    }

    // 辅助方法：安全获取 JSON 字符串属性
    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }
        return null;
    }

    // 辅助方法：安全获取 JSON 数字属性
    private static long GetLongProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt64();
            }
            if (property.ValueKind == JsonValueKind.String && 
                long.TryParse(property.GetString(), out var value))
            {
                return value;
            }
        }
        return 0;
    }
}

