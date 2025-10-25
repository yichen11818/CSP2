using System.Text.Json;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// 插件仓库服务实现
/// </summary>
public class PluginRepositoryService : IPluginRepositoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PluginRepositoryService> _logger;
    private readonly string _cacheDirectory;
    private readonly string _cacheFilePath;
    private PluginManifest? _cachedManifest;
    private DateTime? _lastCacheTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PluginRepositoryService(ILogger<PluginRepositoryService> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CSP2-Server-Panel");
        _logger = logger;

        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _cacheDirectory = Path.Combine(appDirectory, "data");
        _cacheFilePath = Path.Combine(_cacheDirectory, "plugins-cache.json");

        EnsureCacheDirectory();
    }

    private void EnsureCacheDirectory()
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<PluginManifest> GetManifestAsync(bool forceRefresh = false)
    {
        // 如果有缓存且未过期，直接返回
        if (!forceRefresh && _cachedManifest != null && _lastCacheTime != null)
        {
            if (DateTime.Now - _lastCacheTime.Value < _cacheExpiration)
            {
                return _cachedManifest;
            }
        }

        // 尝试从本地缓存文件加载
        if (!forceRefresh && File.Exists(_cacheFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
                
                if (manifest != null)
                {
                    _cachedManifest = manifest;
                    _lastCacheTime = DateTime.Now;
                    _logger.LogInformation("从缓存加载插件清单，共 {Count} 个插件", manifest.Plugins.Count);
                    return manifest;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载缓存的插件清单失败");
            }
        }

        // 创建默认清单（用于开发阶段）
        var defaultManifest = CreateDefaultManifest();
        
        // 保存到缓存
        try
        {
            var json = JsonSerializer.Serialize(defaultManifest, JsonOptions);
            await File.WriteAllTextAsync(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存插件清单缓存失败");
        }

        _cachedManifest = defaultManifest;
        _lastCacheTime = DateTime.Now;

        return defaultManifest;
    }

    public async Task<List<PluginInfo>> SearchPluginsAsync(string keyword)
    {
        var manifest = await GetManifestAsync();
        var lowerKeyword = keyword.ToLower();

        return manifest.Plugins
            .Where(p => 
                p.Name.ToLower().Contains(lowerKeyword) ||
                p.Description.ToLower().Contains(lowerKeyword) ||
                (p.DescriptionZh?.ToLower().Contains(lowerKeyword) ?? false) ||
                p.Tags.Any(t => t.ToLower().Contains(lowerKeyword)))
            .ToList();
    }

    public async Task<List<PluginInfo>> GetPluginsByCategoryAsync(string category)
    {
        var manifest = await GetManifestAsync();
        return manifest.Plugins
            .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<PluginInfo?> GetPluginDetailsAsync(string pluginId)
    {
        var manifest = await GetManifestAsync();
        return manifest.Plugins.FirstOrDefault(p => p.Id == pluginId);
    }

    public async Task<Dictionary<string, string>> CheckUpdatesAsync(List<InstalledPlugin> installedPlugins)
    {
        var manifest = await GetManifestAsync();
        var updates = new Dictionary<string, string>();

        foreach (var installed in installedPlugins)
        {
            var available = manifest.Plugins.FirstOrDefault(p => p.Id == installed.Id);
            if (available != null && available.Version != installed.Version)
            {
                // 简单的版本比较（实际应该使用 SemVer）
                updates[installed.Id] = available.Version;
            }
        }

        return updates;
    }

    public async Task<bool> RefreshCacheAsync()
    {
        try
        {
            await GetManifestAsync(forceRefresh: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新缓存失败");
            return false;
        }
    }

    /// <summary>
    /// 创建默认插件清单（用于开发阶段）
    /// </summary>
    private PluginManifest CreateDefaultManifest()
    {
        return new PluginManifest
        {
            Version = "1.0",
            LastUpdated = DateTime.Now,
            Categories = new List<string> { "gameplay", "admin", "utility", "fun" },
            Plugins = new List<PluginInfo>
            {
                new PluginInfo
                {
                    Id = "weaponpaints",
                    Name = "WeaponPaints",
                    Author = "Nereziel & daffyy",
                    Description = "Weapon skins plugin for CS2",
                    DescriptionZh = "CS2 武器皮肤插件",
                    Framework = "counterstrikesharp",
                    Dependencies = Array.Empty<string>(),
                    Category = "gameplay",
                    Tags = new[] { "skins", "weapons", "customization" },
                    Version = "2.0.0",
                    DownloadUrl = "https://github.com/Nereziel/cs2-WeaponPaints/releases/latest/download/WeaponPaints.zip",
                    DownloadSize = 5242880,
                    Verified = true,
                    Featured = true,
                    Downloads = 15000,
                    Rating = 4.8f
                },
                new PluginInfo
                {
                    Id = "matchzy",
                    Name = "MatchZy",
                    Author = "shobhit-pathak",
                    Description = "CS2 match plugin for competitive matches",
                    DescriptionZh = "CS2 竞技比赛管理插件",
                    Framework = "counterstrikesharp",
                    Dependencies = Array.Empty<string>(),
                    Category = "admin",
                    Tags = new[] { "match", "competitive", "tournament" },
                    Version = "0.8.4",
                    DownloadUrl = "https://github.com/shobhit-pathak/MatchZy/releases/latest/download/MatchZy.zip",
                    DownloadSize = 2097152,
                    Verified = true,
                    Featured = true,
                    Downloads = 8000,
                    Rating = 4.9f
                },
                new PluginInfo
                {
                    Id = "openprefiremc",
                    Name = "OpenPrefirePrac",
                    Author = "lengran",
                    Description = "Practice mode for CS2",
                    DescriptionZh = "CS2 练习模式插件",
                    Framework = "counterstrikesharp",
                    Dependencies = Array.Empty<string>(),
                    Category = "utility",
                    Tags = new[] { "practice", "training", "aim" },
                    Version = "1.0.5",
                    DownloadUrl = "https://github.com/lengran/OpenPrefirePrac/releases/latest/download/OpenPrefirePrac.zip",
                    DownloadSize = 1048576,
                    Verified = true,
                    Featured = false,
                    Downloads = 5000,
                    Rating = 4.6f
                }
            }
        };
    }
}


