using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 加载所有服务器配置
    /// </summary>
    /// <returns>服务器列表</returns>
    Task<List<Server>> LoadServersAsync();

    /// <summary>
    /// 保存所有服务器配置
    /// </summary>
    /// <param name="servers">服务器列表</param>
    /// <returns>是否成功</returns>
    Task<bool> SaveServersAsync(List<Server> servers);

    /// <summary>
    /// 加载应用设置
    /// </summary>
    /// <returns>应用设置</returns>
    Task<AppSettings> LoadAppSettingsAsync();

    /// <summary>
    /// 保存应用设置
    /// </summary>
    /// <param name="settings">应用设置</param>
    /// <returns>是否成功</returns>
    Task<bool> SaveAppSettingsAsync(AppSettings settings);

    /// <summary>
    /// 获取数据目录路径
    /// </summary>
    /// <returns>数据目录路径</returns>
    string GetDataDirectory();
}

/// <summary>
/// 应用设置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// UI设置
    /// </summary>
    public UiSettings Ui { get; set; } = new();

    /// <summary>
    /// SteamCMD设置
    /// </summary>
    public SteamCmdSettings SteamCmd { get; set; } = new();

    /// <summary>
    /// 插件仓库设置
    /// </summary>
    public RepositorySettings Repository { get; set; } = new();
}

/// <summary>
/// UI设置
/// </summary>
public class UiSettings
{
    /// <summary>
    /// 主题（dark, light）
    /// </summary>
    public string Theme { get; set; } = "dark";

    /// <summary>
    /// 语言（zh-CN, en-US）
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 是否自动检查更新
    /// </summary>
    public bool AutoCheckUpdates { get; set; } = true;

    /// <summary>
    /// 窗口宽度
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// 窗口高度
    /// </summary>
    public double WindowHeight { get; set; } = 800;
}

/// <summary>
/// SteamCMD设置
/// </summary>
public class SteamCmdSettings
{
    /// <summary>
    /// 安装路径
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>
    /// 是否自动下载
    /// </summary>
    public bool AutoDownload { get; set; } = true;
}

/// <summary>
/// 仓库设置
/// </summary>
public class RepositorySettings
{
    /// <summary>
    /// 仓库URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 缓存过期时间（秒）
    /// </summary>
    public int CacheExpiration { get; set; } = 3600;
}

