using CSP2.Core.Abstractions;
using CSP2.Core.Logging;
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
        DebugLogger.Info("GetInstalledPlugins", $"开始扫描服务器 {serverId} 的已安装插件");
        
        var server = await _serverManager.GetServerByIdAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("服务器不存在: {Id}", serverId);
            DebugLogger.Warning("GetInstalledPlugins", $"服务器不存在: {serverId}");
            return new List<InstalledPlugin>();
        }

        DebugLogger.Info("GetInstalledPlugins", $"服务器路径: {server.InstallPath}");
        DebugLogger.Info("GetInstalledPlugins", $"已安装框架数: {server.Frameworks.Count}");

        var allInstalledPlugins = new List<InstalledPlugin>();

        try
        {
            // 获取所有可用插件（从 manifest）
            var availablePlugins = await GetAvailablePluginsAsync(forceRefresh: false);
            
            _logger.LogDebug("开始扫描已安装插件，共 {Count} 个可用插件", availablePlugins.Count);
            DebugLogger.Info("GetInstalledPlugins", $"manifest 中共有 {availablePlugins.Count} 个可用插件");

            // 对每个插件，根据其 installation.mappings 检查是否已安装
            foreach (var pluginInfo in availablePlugins)
            {
                // 获取框架Provider并检查框架是否实际已安装
                var frameworkProvider = _providerRegistry.GetFrameworkProvider(pluginInfo.Framework);
                if (frameworkProvider == null)
                {
                    DebugLogger.Debug("GetInstalledPlugins", 
                        $"跳过插件 {pluginInfo.Name}：找不到框架 {pluginInfo.Framework} 的 Provider");
                    continue;
                }

                // 实际检查框架是否已安装（不依赖 server.Frameworks 配置）
                var frameworkInstalled = await frameworkProvider.IsInstalledAsync(server.InstallPath);
                if (!frameworkInstalled)
                {
                    DebugLogger.Debug("GetInstalledPlugins", 
                        $"跳过插件 {pluginInfo.Name}：框架 {pluginInfo.Framework} 未安装");
                    continue; // 框架未安装，跳过此插件
                }

                DebugLogger.Debug("GetInstalledPlugins", 
                    $"检查插件 {pluginInfo.Name} (框架: {pluginInfo.Framework})");

                // 检查插件是否已安装
                if (await IsPluginInstalledAsync(server.InstallPath, pluginInfo))
                {
                    var installedPlugin = new InstalledPlugin
                    {
                        Id = pluginInfo.Id,
                        Name = pluginInfo.Name,
                        Version = pluginInfo.Version,
                        Framework = pluginInfo.Framework,
                        Enabled = true, // TODO: 可以根据实际情况判断
                        InstallPath = GetPluginInstallPath(server.InstallPath, pluginInfo),
                        InstalledAt = GetInstallDate(server.InstallPath, pluginInfo)
                    };

                    allInstalledPlugins.Add(installedPlugin);
                    
                    _logger.LogDebug("检测到已安装插件: {Name} v{Version}", 
                        pluginInfo.Name, pluginInfo.Version);
                    DebugLogger.Info("GetInstalledPlugins", 
                        $"✓ 检测到已安装插件: {pluginInfo.Name} v{pluginInfo.Version}");
                }
            }

            _logger.LogInformation("扫描完成，找到 {Count} 个已安装插件", allInstalledPlugins.Count);
            DebugLogger.Info("GetInstalledPlugins", 
                $"✓ 扫描完成，共找到 {allInstalledPlugins.Count} 个已安装插件");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描已安装插件失败");
        }

        return allInstalledPlugins;
    }

    /// <summary>
    /// 检查插件是否已安装（根据 installation.mappings 中的 target 路径）
    /// </summary>
    private async Task<bool> IsPluginInstalledAsync(string serverPath, PluginInfo pluginInfo)
    {
        if (pluginInfo.Installation?.Mappings == null || pluginInfo.Installation.Mappings.Length == 0)
        {
            DebugLogger.Debug("PluginScan", $"插件 {pluginInfo.Name} 没有 mappings 配置");
            return false;
        }

        DebugLogger.Debug("PluginScan", 
            $"扫描插件 {pluginInfo.Name}，共 {pluginInfo.Installation.Mappings.Length} 个映射路径");

        // 检查所有 mapping 的 target 路径是否存在文件
        foreach (var mapping in pluginInfo.Installation.Mappings)
        {
            var targetPath = Path.Combine(serverPath, mapping.Target);
            
            DebugLogger.Debug("PluginScan", $"  检查路径: {targetPath}");
            
            // 检查目录是否存在且包含文件
            if (Directory.Exists(targetPath))
            {
                var hasFiles = Directory.EnumerateFileSystemEntries(targetPath, "*", SearchOption.AllDirectories).Any();
                if (hasFiles)
                {
                    var fileCount = Directory.EnumerateFileSystemEntries(targetPath, "*", SearchOption.AllDirectories).Count();
                    DebugLogger.Info("PluginScan", 
                        $"✓ 插件 {pluginInfo.Name} 已安装（在 {mapping.Target}，包含 {fileCount} 个文件）");
                    return true; // 找到至少一个 mapping 有文件，说明已安装
                }
                else
                {
                    DebugLogger.Debug("PluginScan", $"  路径存在但为空");
                }
            }
            else
            {
                DebugLogger.Debug("PluginScan", $"  路径不存在");
            }
        }

        return false;
    }

    /// <summary>
    /// 获取插件的安装路径（返回第一个 mapping 的 target）
    /// </summary>
    private string GetPluginInstallPath(string serverPath, PluginInfo pluginInfo)
    {
        if (pluginInfo.Installation?.Mappings != null && pluginInfo.Installation.Mappings.Length > 0)
        {
            return Path.Combine(serverPath, pluginInfo.Installation.Mappings[0].Target);
        }

        // 回退到默认路径
        return Path.Combine(serverPath, "game", "csgo", "addons", pluginInfo.Framework, "plugins", pluginInfo.Id);
    }

    /// <summary>
    /// 获取插件的安装日期（从第一个 mapping 的 target 路径的创建时间）
    /// </summary>
    private DateTime GetInstallDate(string serverPath, PluginInfo pluginInfo)
    {
        var installPath = GetPluginInstallPath(serverPath, pluginInfo);
        
        if (Directory.Exists(installPath))
        {
            return Directory.GetCreationTime(installPath);
        }

        return DateTime.Now;
    }

    public async Task<InstallResult> InstallPluginAsync(string serverId, string pluginId, 
        IProgress<InstallProgress>? progress = null)
    {
        // 使用一个集合跟踪安装链，避免循环依赖
        var installationChain = new HashSet<string>();
        var installedDependencies = new Dictionary<string, string>();
        return await InstallPluginWithDependenciesAsync(serverId, pluginId, progress, installationChain, installedDependencies);
    }

    /// <summary>
    /// 递归安装插件及其依赖项
    /// </summary>
    private async Task<InstallResult> InstallPluginWithDependenciesAsync(
        string serverId, 
        string pluginId, 
        IProgress<InstallProgress>? progress,
        HashSet<string> installationChain,
        Dictionary<string, string> installedDependencies)
    {
        var server = await _serverManager.GetServerByIdAsync(serverId);
        if (server == null)
        {
            return InstallResult.CreateFailure("服务器不存在");
        }

        // 检查循环依赖
        if (installationChain.Contains(pluginId))
        {
            var chain = string.Join(" -> ", installationChain) + $" -> {pluginId}";
            var errorMsg = $"检测到循环依赖: {chain}";
            DebugLogger.Error("InstallPluginAsync", $"❌ {errorMsg}");
            return InstallResult.CreateFailure(errorMsg);
        }

        // 获取插件信息
        var pluginInfo = await _repositoryService.GetPluginDetailsAsync(pluginId);
        if (pluginInfo == null)
        {
            return InstallResult.CreateFailure($"找不到插件: {pluginId}");
        }

        // 添加到安装链
        installationChain.Add(pluginId);

        // 获取对应的框架Provider
        var frameworkProvider = _providerRegistry.GetFrameworkProvider(pluginInfo.Framework);
        if (frameworkProvider == null)
        {
            return InstallResult.CreateFailure($"不支持的框架: {pluginInfo.Framework}");
        }

        // 检查框架是否已安装
        DebugLogger.Info("InstallPluginAsync", $"检查框架 {pluginInfo.Framework} 是否已安装...");
        DebugLogger.Debug("InstallPluginAsync", $"服务器路径: {server.InstallPath}");
        
        var isFrameworkInstalled = await frameworkProvider.IsInstalledAsync(server.InstallPath);
        if (!isFrameworkInstalled)
        {
            var errorMsg = $"框架 {pluginInfo.Framework} 未安装，请先安装框架";
            DebugLogger.Error("InstallPluginAsync", $"❌ {errorMsg}");
            DebugLogger.Info("InstallPluginAsync", "💡 提示：请先到「插件框架」页面安装 CounterStrikeSharp 框架");
            return InstallResult.CreateFailure(errorMsg);
        }
        
        DebugLogger.Info("InstallPluginAsync", $"✓ 框架 {pluginInfo.Framework} 已安装");

        // 检查并安装依赖
        if (pluginInfo.Dependencies.Length > 0)
        {
            DebugLogger.Info("InstallPluginAsync", 
                $"检查插件依赖（需要 {pluginInfo.Dependencies.Length} 个依赖插件）...");
            
            var installed = await GetInstalledPluginsAsync(serverId);
            var missingDependencies = new List<string>();

            // 先检查哪些依赖缺失
            foreach (var dependencyId in pluginInfo.Dependencies)
            {
                if (!installed.Any(p => p.Id == dependencyId))
                {
                    missingDependencies.Add(dependencyId);
                }
                else
                {
                    DebugLogger.Debug("InstallPluginAsync", $"  ✓ 依赖插件 {dependencyId} 已安装");
                }
            }

            // 如果有缺失的依赖，尝试自动安装
            if (missingDependencies.Count > 0)
            {
                DebugLogger.Info("InstallPluginAsync", 
                    $"发现 {missingDependencies.Count} 个缺失的依赖，尝试自动安装...");

                foreach (var dependencyId in missingDependencies)
                {
                    DebugLogger.Info("InstallPluginAsync", $"正在查找依赖插件: {dependencyId}");
                    
                    // 先尝试从仓库获取依赖插件信息
                    var dependencyInfo = await _repositoryService.GetPluginDetailsAsync(dependencyId);
                    
                    if (dependencyInfo == null)
                    {
                        var errorMsg = $"无法找到依赖插件: {dependencyId}，请确认该插件在插件仓库中存在";
                        DebugLogger.Error("InstallPluginAsync", $"❌ {errorMsg}");
                        return InstallResult.CreateFailure(errorMsg);
                    }

                    DebugLogger.Info("InstallPluginAsync", 
                        $"✓ 找到依赖插件: {dependencyInfo.Name} v{dependencyInfo.Version}");

                    // 报告进度
                    progress?.Report(new InstallProgress
                    {
                        Percentage = 0,
                        CurrentStep = $"安装依赖插件: {dependencyInfo.Name}",
                        Message = $"正在安装 {pluginInfo.Name} 所需的依赖..."
                    });

                    // 递归安装依赖（包括依赖的依赖）
                    DebugLogger.Info("InstallPluginAsync", $"开始安装依赖插件: {dependencyInfo.Name}");
                    
                    var dependencyResult = await InstallPluginWithDependenciesAsync(
                        serverId, 
                        dependencyId, 
                        progress, 
                        new HashSet<string>(installationChain), // 传递当前安装链的副本
                        installedDependencies); // 共享依赖记录字典
                    
                    if (!dependencyResult.Success)
                    {
                        var errorMsg = $"安装依赖插件 {dependencyInfo.Name} 失败: {dependencyResult.ErrorMessage}";
                        DebugLogger.Error("InstallPluginAsync", $"❌ {errorMsg}");
                        return InstallResult.CreateFailure(errorMsg);
                    }

                    // 记录成功安装的依赖
                    if (!installedDependencies.ContainsKey(dependencyId))
                    {
                        installedDependencies[dependencyId] = dependencyInfo.Name;
                    }

                    DebugLogger.Info("InstallPluginAsync", 
                        $"✓ 依赖插件 {dependencyInfo.Name} 安装成功");
                }

                DebugLogger.Info("InstallPluginAsync", 
                    $"✓ 所有 {missingDependencies.Count} 个依赖插件已成功安装");
            }
            else
            {
                DebugLogger.Info("InstallPluginAsync", "✓ 所有依赖插件已安装");
            }
        }

        // 安装主插件
        try
        {
            DebugLogger.Info("InstallPluginAsync", $"开始安装插件: {pluginInfo.Name} v{pluginInfo.Version}");
            
            progress?.Report(new InstallProgress
            {
                Percentage = 0,
                CurrentStep = $"安装插件: {pluginInfo.Name}",
                Message = "正在下载和安装插件..."
            });

            var result = await frameworkProvider.InstallPluginAsync(
                server.InstallPath, pluginInfo, progress);

            if (result.Success)
            {
                _logger.LogInformation("插件安装成功: {Plugin} -> {Server}", 
                    pluginInfo.Name, server.Name);
                DebugLogger.Info("InstallPluginAsync", $"✓ 插件 {pluginInfo.Name} 安装成功");
                
                // 将安装的依赖信息附加到结果中
                result.InstalledDependencies = installedDependencies;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装插件失败: {Plugin}", pluginInfo.Name);
            DebugLogger.Error("InstallPluginAsync", $"❌ 安装插件失败: {ex.Message}", ex);
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


