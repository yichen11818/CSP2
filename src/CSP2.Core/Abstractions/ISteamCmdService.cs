using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// SteamCMD服务接口
/// </summary>
public interface ISteamCmdService
{
    /// <summary>
    /// 检查SteamCMD是否已安装
    /// </summary>
    /// <returns>是否已安装</returns>
    Task<bool> IsSteamCmdInstalledAsync();

    /// <summary>
    /// 下载并安装SteamCMD
    /// </summary>
    /// <param name="installPath">安装路径</param>
    /// <param name="progress">进度报告</param>
    /// <returns>是否成功</returns>
    Task<bool> InstallSteamCmdAsync(string installPath, IProgress<DownloadProgress>? progress = null);

    /// <summary>
    /// 安装或更新CS2专用服务器
    /// </summary>
    /// <param name="serverPath">服务器安装路径</param>
    /// <param name="validate">是否验证文件完整性</param>
    /// <param name="progress">进度报告</param>
    /// <returns>是否成功</returns>
    Task<bool> InstallOrUpdateServerAsync(string serverPath, bool validate = false, 
        IProgress<InstallProgress>? progress = null);

    /// <summary>
    /// 验证服务器文件完整性
    /// </summary>
    /// <param name="serverPath">服务器路径</param>
    /// <param name="progress">进度报告</param>
    /// <returns>是否成功</returns>
    Task<bool> ValidateServerFilesAsync(string serverPath, IProgress<InstallProgress>? progress = null);

    /// <summary>
    /// 获取SteamCMD安装路径
    /// </summary>
    /// <returns>安装路径</returns>
    string GetSteamCmdPath();

    /// <summary>
    /// 卸载SteamCMD
    /// </summary>
    /// <returns>是否成功</returns>
    Task<bool> UninstallSteamCmdAsync();
}

