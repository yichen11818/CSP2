using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 框架提供者接口 - 抽象不同插件框架的差异
/// </summary>
public interface IFrameworkProvider
{
    /// <summary>
    /// 提供者元数据
    /// </summary>
    ProviderMetadata Metadata { get; }

    /// <summary>
    /// 框架信息
    /// </summary>
    FrameworkInfo FrameworkInfo { get; }

    /// <summary>
    /// 检查框架是否已安装
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <returns>是否已安装</returns>
    Task<bool> IsInstalledAsync(string serverPath);

    /// <summary>
    /// 获取已安装的框架版本
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <returns>版本号，未安装返回null</returns>
    Task<string?> GetInstalledVersionAsync(string serverPath);

    /// <summary>
    /// 安装框架
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <param name="version">版本号，null表示安装最新版</param>
    /// <param name="progress">进度报告</param>
    /// <returns>安装结果</returns>
    Task<InstallResult> InstallAsync(string serverPath, string? version = null, 
        IProgress<InstallProgress>? progress = null);

    /// <summary>
    /// 更新框架
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <param name="progress">进度报告</param>
    /// <returns>安装结果</returns>
    Task<InstallResult> UpdateAsync(string serverPath, IProgress<InstallProgress>? progress = null);

    /// <summary>
    /// 卸载框架
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <returns>是否成功</returns>
    Task<bool> UninstallAsync(string serverPath);

    /// <summary>
    /// 扫描已安装的插件
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <returns>已安装的插件列表</returns>
    Task<List<InstalledPlugin>> ScanInstalledPluginsAsync(string serverPath);

    /// <summary>
    /// 安装插件
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <param name="pluginInfo">插件信息</param>
    /// <param name="progress">进度报告</param>
    /// <returns>安装结果</returns>
    Task<InstallResult> InstallPluginAsync(string serverPath, PluginInfo pluginInfo, 
        IProgress<InstallProgress>? progress = null);

    /// <summary>
    /// 卸载插件
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <param name="plugin">已安装的插件</param>
    /// <returns>是否成功</returns>
    Task<bool> UninstallPluginAsync(string serverPath, InstalledPlugin plugin);

    /// <summary>
    /// 启用或禁用插件
    /// </summary>
    /// <param name="serverPath">服务器根目录</param>
    /// <param name="plugin">已安装的插件</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>是否成功</returns>
    Task<bool> SetPluginEnabledAsync(string serverPath, InstalledPlugin plugin, bool enabled);

    /// <summary>
    /// 检查框架是否有更新
    /// </summary>
    /// <param name="currentVersion">当前版本</param>
    /// <returns>最新版本，null表示无更新</returns>
    Task<string?> CheckUpdateAsync(string currentVersion);
}

