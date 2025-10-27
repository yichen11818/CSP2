using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 插件仓库服务接口
/// </summary>
public interface IPluginRepositoryService
{
    /// <summary>
    /// 获取插件清单（带缓存）
    /// </summary>
    /// <param name="forceRefresh">是否强制刷新</param>
    /// <returns>插件清单</returns>
    Task<PluginManifest> GetManifestAsync(bool forceRefresh = false);

    /// <summary>
    /// 搜索插件
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <returns>匹配的插件列表</returns>
    Task<List<PluginInfo>> SearchPluginsAsync(string keyword);

    /// <summary>
    /// 按分类过滤
    /// </summary>
    /// <param name="category">分类名称</param>
    /// <returns>插件列表</returns>
    Task<List<PluginInfo>> GetPluginsByCategoryAsync(string category);

    /// <summary>
    /// 获取插件详情
    /// </summary>
    /// <param name="pluginId">插件ID</param>
    /// <returns>插件信息，不存在返回null</returns>
    Task<PluginInfo?> GetPluginDetailsAsync(string pluginId);

    /// <summary>
    /// 检查插件更新
    /// </summary>
    /// <param name="installedPlugins">已安装的插件列表</param>
    /// <returns>插件ID到最新版本的映射</returns>
    Task<Dictionary<string, string>> CheckUpdatesAsync(List<InstalledPlugin> installedPlugins);

    /// <summary>
    /// 刷新缓存
    /// </summary>
    /// <returns>是否成功</returns>
    Task<bool> RefreshCacheAsync();
}

/// <summary>
/// 插件清单
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// 清单版本
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// 插件列表
    /// </summary>
    public List<PluginInfo> Plugins { get; set; } = new();

    /// <summary>
    /// 分类列表
    /// </summary>
    public List<CategoryInfo> Categories { get; set; } = new();
}

/// <summary>
/// 分类信息
/// </summary>
public class CategoryInfo
{
    /// <summary>
    /// 分类ID（小写，用于匹配）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 分类名称（英文）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类名称（中文）
    /// </summary>
    public string? NameZh { get; set; }

    /// <summary>
    /// 分类描述（英文）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 分类描述（中文）
    /// </summary>
    public string? DescriptionZh { get; set; }
}

