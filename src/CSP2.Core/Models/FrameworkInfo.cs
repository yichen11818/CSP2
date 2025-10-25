namespace CSP2.Core.Models;

/// <summary>
/// 插件框架信息
/// </summary>
public class FrameworkInfo
{
    /// <summary>
    /// 框架唯一标识（小写）
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 框架显示名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 短名称
    /// </summary>
    public required string ShortName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 依赖的其他框架ID列表
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 安装路径（相对于服务器根目录）
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>
    /// 插件路径（相对于服务器根目录）
    /// </summary>
    public string PluginPath { get; set; } = string.Empty;

    /// <summary>
    /// 配置路径（相对于服务器根目录）
    /// </summary>
    public string ConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// 支持的平台（windows, linux）
    /// </summary>
    public string[] SupportedPlatforms { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 仓库地址
    /// </summary>
    public string RepositoryUrl { get; set; } = string.Empty;

    /// <summary>
    /// 文档地址
    /// </summary>
    public string DocumentationUrl { get; set; } = string.Empty;

    /// <summary>
    /// 已安装版本
    /// </summary>
    public string? InstalledVersion { get; set; }

    /// <summary>
    /// 最新版本
    /// </summary>
    public string? LatestVersion { get; set; }
}

