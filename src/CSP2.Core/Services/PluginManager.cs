using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// 插件管理器实现
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly IServerManager _serverManager;
    private readonly IPluginRepositoryService _repositoryService;
    private readonly ProviderRegistry _providerRegistry;
    private readonly ILogger<PluginManager> _logger;

    public PluginManager(
        IServerManager serverManager,
        IPluginRepositoryService repositoryService,
        ProviderRegistry providerRegistry,
        ILogger<PluginManager> logger)
    {
        _serverManager = serverManager;
        _repositoryService = repositoryService;
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<List<PluginInfo>> GetAvailablePluginsAsync(bool forceRefresh = false)
    {
        var manifest = await _repositoryService.GetManifestAsync(forceRefresh);
        return manifest.Plugins;
    }

    public async Task<List<InstalledPlugin>> GetInstalledPluginsAsync(string serverId)
    {
        var server = await _serverManager.GetServerByIdAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("服务器不存在: {Id}", serverId);
            return new List<InstalledPlugin>();
        }

        var allInstalledPlugins = new List<InstalledPlugin>();

        // 遍历所有已安装的框架，扫描插件
        foreach (var framework in server.Frameworks)
        {
            var frameworkProvider = _providerRegistry.GetFrameworkProvider(framework.Id);
            if (frameworkProvider == null)
            {
                _logger.LogWarning("找不到框架Provider: {Id}", framework.Id);
                continue;
            }

            try
            {
                var plugins = await frameworkProvider.ScanInstalledPluginsAsync(server.InstallPath);
                allInstalledPlugins.AddRange(plugins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描框架 {Framework} 的插件失败", framework.Id);
            }
        }

        return allInstalledPlugins;
    }

    public async Task<InstallResult> InstallPluginAsync(string serverId, string pluginId, 
        IProgress<InstallProgress>? progress = null)
    {
        var server = await _serverManager.GetServerByIdAsync(serverId);
        if (server == null)
        {
            return InstallResult.CreateFailure("服务器不存在");
        }

        // 获取插件信息
        var pluginInfo = await _repositoryService.GetPluginDetailsAsync(pluginId);
        if (pluginInfo == null)
        {
            return InstallResult.CreateFailure($"找不到插件: {pluginId}");
        }

        // 获取对应的框架Provider
        var frameworkProvider = _providerRegistry.GetFrameworkProvider(pluginInfo.Framework);
        if (frameworkProvider == null)
        {
            return InstallResult.CreateFailure($"不支持的框架: {pluginInfo.Framework}");
        }

        // 检查框架是否已安装
        var isFrameworkInstalled = await frameworkProvider.IsInstalledAsync(server.InstallPath);
        if (!isFrameworkInstalled)
        {
            return InstallResult.CreateFailure($"框架 {pluginInfo.Framework} 未安装，请先安装框架");
        }

        // 检查依赖
        foreach (var dependencyId in pluginInfo.Dependencies)
        {
            var installed = await GetInstalledPluginsAsync(serverId);
            if (!installed.Any(p => p.Id == dependencyId))
            {
                return InstallResult.CreateFailure($"缺少依赖插件: {dependencyId}");
            }
        }

        // 安装插件
        try
        {
            var result = await frameworkProvider.InstallPluginAsync(
                server.InstallPath, pluginInfo, progress);

            if (result.Success)
            {
                _logger.LogInformation("插件安装成功: {Plugin} -> {Server}", 
                    pluginInfo.Name, server.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装插件失败: {Plugin}", pluginInfo.Name);
            return InstallResult.CreateFailure($"安装失败: {ex.Message}", ex);
        }
    }

    public async Task<InstallResult> UpdatePluginAsync(string serverId, string pluginId, 
        IProgress<InstallProgress>? progress = null)
    {
        // 更新就是重新安装最新版本
        return await InstallPluginAsync(serverId, pluginId, progress);
    }

    public async Task<bool> UninstallPluginAsync(string serverId, string pluginId)
    {
        var server = await _serverManager.GetServerByIdAsync(serverId);
        if (server == null)
        {
            return false;
        }

        var installedPlugins = await GetInstalledPluginsAsync(serverId);
        var plugin = installedPlugins.FirstOrDefault(p => p.Id == pluginId);
        
        if (plugin == null)
        {
            _logger.LogWarning("插件未安装: {Id}", pluginId);
            return false;
        }

        var frameworkProvider = _providerRegistry.GetFrameworkProvider(plugin.Framework);
        if (frameworkProvider == null)
        {
            return false;
        }

        try
        {
            var result = await frameworkProvider.UninstallPluginAsync(server.InstallPath, plugin);
            
            if (result)
            {
                _logger.LogInformation("插件已卸载: {Plugin}", plugin.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载插件失败: {Plugin}", plugin.Name);
            return false;
        }
    }

    public async Task<bool> SetPluginEnabledAsync(string serverId, string pluginId, bool enabled)
    {
        var server = await _serverManager.GetServerByIdAsync(serverId);
        if (server == null)
        {
            return false;
        }

        var installedPlugins = await GetInstalledPluginsAsync(serverId);
        var plugin = installedPlugins.FirstOrDefault(p => p.Id == pluginId);
        
        if (plugin == null)
        {
            return false;
        }

        var frameworkProvider = _providerRegistry.GetFrameworkProvider(plugin.Framework);
        if (frameworkProvider == null)
        {
            return false;
        }

        try
        {
            var result = await frameworkProvider.SetPluginEnabledAsync(
                server.InstallPath, plugin, enabled);
            
            if (result)
            {
                _logger.LogInformation("插件状态已更改: {Plugin} -> {Status}", 
                    plugin.Name, enabled ? "启用" : "禁用");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更改插件状态失败: {Plugin}", plugin.Name);
            return false;
        }
    }

    public async Task<List<PluginUpdateInfo>> CheckUpdatesAsync(string serverId)
    {
        var installedPlugins = await GetInstalledPluginsAsync(serverId);
        var updates = await _repositoryService.CheckUpdatesAsync(installedPlugins);

        var updateInfoList = new List<PluginUpdateInfo>();

        foreach (var (pluginId, latestVersion) in updates)
        {
            var plugin = installedPlugins.First(p => p.Id == pluginId);
            updateInfoList.Add(new PluginUpdateInfo
            {
                PluginId = pluginId,
                PluginName = plugin.Name,
                CurrentVersion = plugin.Version,
                LatestVersion = latestVersion
            });
        }

        return updateInfoList;
    }

    public async Task<List<PluginInfo>> SearchPluginsAsync(string keyword)
    {
        return await _repositoryService.SearchPluginsAsync(keyword);
    }

    public async Task<List<PluginInfo>> GetPluginsByCategoryAsync(string category)
    {
        return await _repositoryService.GetPluginsByCategoryAsync(category);
    }
}


