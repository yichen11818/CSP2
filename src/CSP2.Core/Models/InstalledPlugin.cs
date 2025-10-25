namespace CSP2.Core.Models;

/// <summary>
/// 已安装的插件
/// </summary>
public class InstalledPlugin
{
    /// <summary>
    /// 插件ID
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 插件名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 已安装版本
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// 所属框架
    /// </summary>
    public required string Framework { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 安装路径
    /// </summary>
    public required string InstallPath { get; set; }

    /// <summary>
    /// 安装时间
    /// </summary>
    public DateTime InstalledAt { get; set; } = DateTime.Now;
}

