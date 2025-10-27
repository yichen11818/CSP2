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

    // GitHub源配置 - 可以通过配置文件修改
    private const string DEFAULT_REPOSITORY_URL = 
        "https://raw.githubusercontent.com/your-org/csp2-plugin-repository/main/manifest.json";
    
    // CDN加速源（推荐）
    private const string CDN_REPOSITORY_URL = 
        "https://cdn.jsdelivr.net/gh/your-org/csp2-plugin-repository@main/manifest.json";

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
        // 1. 如果有内存缓存且未过期，直接返回
        if (!forceRefresh && _cachedManifest != null && _lastCacheTime != null)
        {
            if (DateTime.Now - _lastCacheTime.Value < _cacheExpiration)
            {
                _logger.LogDebug("使用内存缓存的插件清单");
                return _cachedManifest;
            }
        }

        // 2. 尝试从远程GitHub拉取最新清单
        try
        {
            _logger.LogInformation("从远程仓库拉取插件清单: {Url}", CDN_REPOSITORY_URL);
            
            var response = await _httpClient.GetAsync(CDN_REPOSITORY_URL);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
                
                if (manifest != null && manifest.Plugins.Count > 0)
                {
                    // 保存到本地缓存
                    await File.WriteAllTextAsync(_cacheFilePath, json);
                    
                    _cachedManifest = manifest;
                    _lastCacheTime = DateTime.Now;
                    
                    _logger.LogInformation("成功从远程拉取插件清单，共 {Count} 个插件", manifest.Plugins.Count);
                    return manifest;
                }
            }
            else
            {
                _logger.LogWarning("远程拉取失败: HTTP {StatusCode}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "网络请求失败，尝试使用本地缓存");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从远程仓库拉取失败");
        }

        // 3. 降级：尝试从本地缓存文件加载
        if (File.Exists(_cacheFilePath))
        {
            try
            {
                _logger.LogInformation("从本地缓存加载插件清单");
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
                
                if (manifest != null)
                {
                    _cachedManifest = manifest;
                    _lastCacheTime = DateTime.Now;
                    _logger.LogInformation("从本地缓存加载插件清单，共 {Count} 个插件", manifest.Plugins.Count);
                    return manifest;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载本地缓存失败");
            }
        }

        // 4. 最后降级：使用内置默认清单（用于开发/离线场景）
        _logger.LogInformation("使用内置默认插件清单");
        var defaultManifest = CreateDefaultManifest();
        
        // 保存到缓存以便下次使用
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


