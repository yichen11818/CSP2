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
    private readonly IConfigurationService _configurationService;
    private readonly string _cacheDirectory;
    private readonly string _cacheFilePath;
    private PluginManifest? _cachedManifest;
    private DateTime? _lastCacheTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    // 默认仓库源配置（多源降级策略）
    private static readonly string[] DEFAULT_REPOSITORY_URLS = new[]
    {
        
        
        // GitHub Pages（备用）
        "https://yichen11818.github.io/csp2-plugin-repository/manifest.json",
// CDN加速源（推荐，速度快）
        
        "https://cdn.jsdelivr.net/gh/yichen11818/csp2-plugin-repository@main/manifest.json",

        
        // GitHub Raw（最后降级）
        "https://raw.githubusercontent.com/yichen11818/csp2-plugin-repository/main/manifest.json"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PluginRepositoryService(
        ILogger<PluginRepositoryService> logger,
        IConfigurationService configurationService)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10) // 设置超时避免长时间等待
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CSP2-Server-Panel/1.0");
        _logger = logger;
        _configurationService = configurationService;

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
        _logger.LogDebug("GetManifestAsync 开始执行，forceRefresh={ForceRefresh}", forceRefresh);
        
        // 1. 如果有内存缓存且未过期，直接返回
        if (!forceRefresh && _cachedManifest != null && _lastCacheTime != null)
        {
            var cacheAge = DateTime.Now - _lastCacheTime.Value;
            if (cacheAge < _cacheExpiration)
            {
                _logger.LogDebug("使用内存缓存的插件清单 (缓存年龄: {Age}，包含 {Count} 个插件)", 
                    cacheAge, _cachedManifest.Plugins?.Count ?? 0);
                return _cachedManifest;
            }
            else
            {
                _logger.LogDebug("内存缓存已过期 (年龄: {Age})，需要刷新", cacheAge);
            }
        }
        else
        {
            _logger.LogDebug("无内存缓存 (cachedManifest={HasCache}, lastCacheTime={HasTime})", 
                _cachedManifest != null, _lastCacheTime != null);
        }

        // 2. 尝试从远程仓库拉取最新清单（多源降级策略）
        _logger.LogInformation("开始尝试从远程仓库拉取插件清单");
        
        var manifest = await TryFetchFromRemoteAsync();
        if (manifest != null)
        {
            _logger.LogInformation("✓ 成功从远程获取清单，包含 {Count} 个插件", manifest.Plugins?.Count ?? 0);
            
            // 保存到本地缓存
            try
            {
                var json = JsonSerializer.Serialize(manifest, JsonOptions);
                await File.WriteAllTextAsync(_cacheFilePath, json);
                _logger.LogDebug("已保存插件清单到本地缓存: {Path}", _cacheFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "保存插件清单缓存失败");
            }
            
            _cachedManifest = manifest;
            _lastCacheTime = DateTime.Now;
            return manifest;
        }
        else
        {
            _logger.LogWarning("从远程仓库拉取失败，尝试使用本地缓存");
        }

        // 3. 降级：尝试从本地缓存文件加载
        if (File.Exists(_cacheFilePath))
        {
            try
            {
                _logger.LogInformation("从本地缓存加载插件清单: {Path}", _cacheFilePath);
                
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                _logger.LogDebug("缓存文件大小: {Length} 字符", json.Length);
                
                manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
                
                if (manifest != null)
                {
                    _cachedManifest = manifest;
                    _lastCacheTime = DateTime.Now;
                    _logger.LogInformation("✓ 从本地缓存加载插件清单成功，共 {Count} 个插件", manifest.Plugins?.Count ?? 0);
                    return manifest;
                }
                else
                {
                    _logger.LogWarning("反序列化本地缓存失败，返回null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载本地缓存失败");
            }
        }
        else
        {
            _logger.LogInformation("本地缓存文件不存在: {Path}", _cacheFilePath);
        }

        // 4. 最后降级：使用空的默认清单
        _logger.LogWarning("⚠ 无法从任何源获取插件清单，使用空的默认清单");
        
        var defaultManifest = CreateDefaultManifest();
        
        _cachedManifest = defaultManifest;
        _lastCacheTime = DateTime.Now;

        return defaultManifest;
    }

    /// <summary>
    /// 从远程仓库获取清单（多源降级策略）
    /// </summary>
    private async Task<PluginManifest?> TryFetchFromRemoteAsync()
    {
        // 获取仓库URL列表（优先使用配置文件中的自定义源）
        var urls = GetRepositoryUrls();
        
        _logger.LogInformation("共有 {Count} 个仓库源可尝试", urls.Length);
        
        for (int i = 0; i < urls.Length; i++)
        {
            var url = urls[i];
            try
            {
                _logger.LogInformation("尝试源 [{Index}/{Total}]: {Url}", i + 1, urls.Length, url);
                
                var response = await _httpClient.GetAsync(url);
                
                _logger.LogDebug("收到响应: HTTP {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("响应内容长度: {Length} 字符", json.Length);
                    
                    // 显示JSON的前200个字符用于调试
                    if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                    {
                        var jsonPreview = json.Length > 200 ? json.Substring(0, 200) + "..." : json;
                        _logger.LogDebug("JSON预览: {Preview}", jsonPreview);
                    }
                    
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);
                    
                    if (manifest != null)
                    {
                        var pluginCount = manifest.Plugins?.Count ?? 0;
                        _logger.LogInformation("✓ 成功从源 [{Index}/{Total}] 拉取插件清单，共 {Count} 个插件", 
                            i + 1, urls.Length, pluginCount);
                        
                        if (pluginCount > 0 && manifest.Plugins != null && _logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                        {
                            var pluginList = string.Join(", ", manifest.Plugins.Take(5).Select(p => p.Name));
                            _logger.LogDebug("插件列表预览: {Preview}...", pluginList);
                        }
                        
                        return manifest;
                    }
                    else
                    {
                        _logger.LogWarning("JSON反序列化返回null (源 [{Index}/{Total}]: {Url})", i + 1, urls.Length, url);
                    }
                }
                else
                {
                    _logger.LogWarning("源 [{Index}/{Total}] HTTP错误 {StatusCode}: {Url}", 
                        i + 1, urls.Length, response.StatusCode, url);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("源 [{Index}/{Total}] 请求超时: {Url}", i + 1, urls.Length, url);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("源 [{Index}/{Total}] 请求失败: {Url}, {Message}", 
                    i + 1, urls.Length, url, ex.Message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "源 [{Index}/{Total}] JSON解析错误: {Url}", i + 1, urls.Length, url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "源 [{Index}/{Total}] 失败: {Url}", i + 1, urls.Length, url);
            }
        }

        _logger.LogError("⚠ 所有 {Count} 个远程源均无法访问", urls.Length);
        return null;
    }

    /// <summary>
    /// 获取仓库URL列表（优先使用自定义配置）
    /// </summary>
    private string[] GetRepositoryUrls()
    {
        var urls = new List<string>();

        _logger.LogDebug("开始构建仓库URL列表");

        // 1. 尝试从配置文件读取自定义源
        try
        {
            var settings = _configurationService.LoadSettings();
            if (!string.IsNullOrWhiteSpace(settings.Repository?.Url))
            {
                urls.Add(settings.Repository.Url);
                _logger.LogDebug("添加自定义仓库源: {Url}", settings.Repository.Url);
            }

            // 如果配置了多个镜像源
            if (settings.Repository?.MirrorUrls != null && settings.Repository.MirrorUrls.Length > 0)
            {
                urls.AddRange(settings.Repository.MirrorUrls);
                _logger.LogDebug("添加 {Count} 个镜像源", settings.Repository.MirrorUrls.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "读取自定义仓库配置失败，使用默认源");
        }

        // 2. 添加默认源作为降级
        urls.AddRange(DEFAULT_REPOSITORY_URLS);
        _logger.LogDebug("添加 {Count} 个默认源", DEFAULT_REPOSITORY_URLS.Length);

        // 3. 去重
        var distinctUrls = urls.Distinct().ToArray();
        var removedCount = urls.Count - distinctUrls.Length;
        
        if (removedCount > 0)
        {
            _logger.LogDebug("去重后删除 {Count} 个重复URL", removedCount);
        }
        
        _logger.LogInformation("最终仓库URL列表包含 {Count} 个源", distinctUrls.Length);
        
        // 列出所有URL用于调试
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
        {
            for (int i = 0; i < distinctUrls.Length; i++)
            {
                _logger.LogDebug("  [{Index}] {Url}", i + 1, distinctUrls[i]);
            }
        }
        
        return distinctUrls;
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
    /// 创建空的默认插件清单（降级方案）
    /// </summary>
    private PluginManifest CreateDefaultManifest()
    {
        _logger.LogWarning("使用空默认清单 - 建议检查网络连接或刷新插件列表");
        
        return new PluginManifest
        {
            Version = "1.0",
            LastUpdated = DateTime.Now,
            Categories = new List<CategoryInfo>
            {
                new CategoryInfo { Id = "gameplay", Name = "Gameplay", NameZh = "游戏玩法" },
                new CategoryInfo { Id = "admin", Name = "Administration", NameZh = "服务器管理" },
                new CategoryInfo { Id = "utility", Name = "Utility", NameZh = "实用工具" },
                new CategoryInfo { Id = "fun", Name = "Fun", NameZh = "娱乐" },
                new CategoryInfo { Id = "stats", Name = "Statistics", NameZh = "数据统计" },
                new CategoryInfo { Id = "other", Name = "Other", NameZh = "其他" }
            },
            Plugins = new List<PluginInfo>()  // 空列表，不再包含测试数据
        };
    }
}



