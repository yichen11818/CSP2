namespace CSP2.Core.Models;

/// <summary>
/// 插件信息
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// 插件唯一标识（小写）
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 插件显示名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// 描述（英文）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 描述（中文）
    /// </summary>
    public string? DescriptionZh { get; set; }

    /// <summary>
    /// 所属框架（counterstrikesharp, metamod等）
    /// </summary>
    public required string Framework { get; set; }

    /// <summary>
    /// 依赖的其他插件ID列表
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 分类（gameplay, admin, utility等）
    /// </summary>
    public string Category { get; set; } = "other";

    /// <summary>
    /// 标签
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 版本号
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// 下载地址
    /// </summary>
    public required string DownloadUrl { get; set; }

    /// <summary>
    /// 下载文件大小（字节）
    /// </summary>
    public long DownloadSize { get; set; }

    /// <summary>
    /// 源代码信息
    /// </summary>
    public SourceInfo? Source { get; set; }

    /// <summary>
    /// 安装信息
    /// </summary>
    public InstallationInfo? Installation { get; set; }

    /// <summary>
    /// 链接信息
    /// </summary>
    public LinksInfo? Links { get; set; }

    /// <summary>
    /// 是否经过验证
    /// </summary>
    public bool Verified { get; set; }

    /// <summary>
    /// 是否推荐
    /// </summary>
    public bool Featured { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int Downloads { get; set; }

    /// <summary>
    /// 评分（0-5）
    /// </summary>
    public float Rating { get; set; }
}

/// <summary>
/// 源代码信息
/// </summary>
public class SourceInfo
{
    /// <summary>
    /// 类型（github, gitlab等）
    /// </summary>
    public string Type { get; set; } = "github";

    /// <summary>
    /// 仓库地址
    /// </summary>
    public required string Repository { get; set; }

    /// <summary>
    /// 分支或标签
    /// </summary>
    public string? Branch { get; set; }
}

/// <summary>
/// 安装信息
/// </summary>
public class InstallationInfo
{
    /// <summary>
    /// 目标路径（相对于服务器根目录）
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要重启服务器
    /// </summary>
    public bool RequiresRestart { get; set; } = true;
}

/// <summary>
/// 链接信息
/// </summary>
public class LinksInfo
{
    /// <summary>
    /// 主页
    /// </summary>
    public string? Homepage { get; set; }

    /// <summary>
    /// 文档
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// 问题反馈
    /// </summary>
    public string? Issues { get; set; }
}

