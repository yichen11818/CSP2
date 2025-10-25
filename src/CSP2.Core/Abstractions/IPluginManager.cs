using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 插件管理器接口
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// 获取可用插件列表（从仓库）
    /// </summary>
    /// <param name="forceRefresh">是否强制刷新缓存</param>
    /// <returns>插件列表</returns>
    Task<List<PluginInfo>> GetAvailablePluginsAsync(bool forceRefresh = false);

    /// <summary>
    /// 获取指定服务器已安装的插件
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>已安装的插件列表</returns>
    Task<List<InstalledPlugin>> GetInstalledPluginsAsync(string serverId);

    /// <summary>
    /// 安装插件
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <param name="pluginId">插件ID</param>
    /// <param name="progress">进度报告</param>
    /// <returns>安装结果</returns>
    Task<InstallResult> InstallPluginAsync(string serverId, string pluginId, 
        IProgress<InstallProgress>? progress = null);

    /// <summary>
    /// 更新插件
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <param name="pluginId">插件ID</param>
    /// <param name="progress">进度报告</param>
    /// <returns>安装结果</returns>
    Task<InstallResult> UpdatePluginAsync(string serverId, string pluginId, 
        IProgress<InstallProgress>? progress = null);

    /// <summary>
    /// 卸载插件
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <param name="pluginId">插件ID</param>
    /// <returns>是否成功</returns>
    Task<bool> UninstallPluginAsync(string serverId, string pluginId);

    /// <summary>
    /// 启用或禁用插件
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <param name="pluginId">插件ID</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>是否成功</returns>
    Task<bool> SetPluginEnabledAsync(string serverId, string pluginId, bool enabled);

    /// <summary>
    /// 检查插件更新
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>有更新的插件列表</returns>
    Task<List<PluginUpdateInfo>> CheckUpdatesAsync(string serverId);

    /// <summary>
    /// 搜索插件
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <returns>匹配的插件列表</returns>
    Task<List<PluginInfo>> SearchPluginsAsync(string keyword);

    /// <summary>
    /// 按分类获取插件
    /// </summary>
    /// <param name="category">分类名称</param>
    /// <returns>插件列表</returns>
    Task<List<PluginInfo>> GetPluginsByCategoryAsync(string category);
}

/// <summary>
/// 插件更新信息
/// </summary>
public class PluginUpdateInfo
{
    /// <summary>
    /// 插件ID
    /// </summary>
    public required string PluginId { get; set; }

    /// <summary>
    /// 插件名称
    /// </summary>
    public required string PluginName { get; set; }

    /// <summary>
    /// 当前版本
    /// </summary>
    public required string CurrentVersion { get; set; }

    /// <summary>
    /// 最新版本
    /// </summary>
    public required string LatestVersion { get; set; }

    /// <summary>
    /// 更新说明
    /// </summary>
    public string? ReleaseNotes { get; set; }
}

