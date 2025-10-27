using System.Text.Json.Serialization;

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
    /// Slug（URL友好的标识符）
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// 作者信息
    /// </summary>
    public AuthorInfo? Author { get; set; }

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
    /// 框架版本要求
    /// </summary>
    public string? FrameworkVersion { get; set; }

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
    /// 更新日志链接
    /// </summary>
    public string? Changelog { get; set; }

    /// <summary>
    /// 下载信息
    /// </summary>
    public DownloadInfo? Download { get; set; }

    /// <summary>
    /// 下载地址（向后兼容）
    /// </summary>
    [JsonIgnore]
    public string DownloadUrl => Download?.Url ?? string.Empty;

    /// <summary>
    /// 下载文件大小（字节，向后兼容）
    /// </summary>
    [JsonIgnore]
    public long DownloadSize => Download?.Size ?? 0;

    /// <summary>
    /// 仓库信息
    /// </summary>
    public RepositoryInfo? Repository { get; set; }

    /// <summary>
    /// 安装信息
    /// </summary>
    public InstallationInfo? Installation { get; set; }

    /// <summary>
    /// 配置信息
    /// </summary>
    public ConfigurationInfo? Configuration { get; set; }

    /// <summary>
    /// 链接信息
    /// </summary>
    public LinksInfo? Links { get; set; }

    /// <summary>
    /// 媒体信息
    /// </summary>
    public MediaInfo? Media { get; set; }

    /// <summary>
    /// 是否经过验证
    /// </summary>
    public bool Verified { get; set; }

    /// <summary>
    /// 是否推荐
    /// </summary>
    public bool Featured { get; set; }

    /// <summary>
    /// 是否官方支持
    /// </summary>
    public bool OfficialSupport { get; set; }

    /// <summary>
    /// 下载统计信息
    /// </summary>
    public DownloadsInfo? Downloads { get; set; }

    /// <summary>
    /// 评分信息
    /// </summary>
    public RatingInfo? Rating { get; set; }

    /// <summary>
    /// 兼容性信息
    /// </summary>
    public CompatibilityInfo? Compatibility { get; set; }

    /// <summary>
    /// 元数据信息
    /// </summary>
    public MetadataInfo? Metadata { get; set; }
}

/// <summary>
/// 作者信息
/// </summary>
public class AuthorInfo
{
    /// <summary>
    /// 作者名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// GitHub用户名
    /// </summary>
    public string? Github { get; set; }

    /// <summary>
    /// 电子邮件
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
/// 下载信息
/// </summary>
public class DownloadInfo
{
    /// <summary>
    /// 下载URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 文件哈希值
    /// </summary>
    public string? Hash { get; set; }
}

/// <summary>
/// 仓库信息
/// </summary>
public class RepositoryInfo
{
    /// <summary>
    /// 类型（github, gitlab等）
    /// </summary>
    public string Type { get; set; } = "github";

    /// <summary>
    /// 所有者
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    public string? Repo { get; set; }

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
    /// 安装类型（extract, copy等）
    /// </summary>
    public string Type { get; set; } = "extract";

    /// <summary>
    /// 目标路径（相对于服务器根目录）- 用于简单安装
    /// </summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>
    /// 需要复制的文件列表 - 用于简单安装
    /// </summary>
    public string[]? Files { get; set; }

    /// <summary>
    /// 文件映射规则 - 用于复杂安装（优先级高于 TargetPath + Files）
    /// </summary>
    public FileMapping[]? Mappings { get; set; }

    /// <summary>
    /// 是否需要重启服务器
    /// </summary>
    public bool RequiresRestart { get; set; } = true;
}

/// <summary>
/// 文件映射规则
/// </summary>
public class FileMapping
{
    /// <summary>
    /// 源路径模式（ZIP内的路径，支持通配符 * 和 **）
    /// 例如: "gamedata/*", "MyPlugin/*.dll", "configs/**/*"
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// 目标路径（相对于服务器根目录）
    /// 例如: "game/csgo/addons/counterstrikesharp/gamedata"
    /// </summary>
    public required string Target { get; set; }

    /// <summary>
    /// 是否递归复制子目录（默认 true）
    /// </summary>
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// 排除的文件模式（可选）
    /// </summary>
    public string[]? Exclude { get; set; }
}

/// <summary>
/// 配置信息
/// </summary>
public class ConfigurationInfo
{
    /// <summary>
    /// 是否需要配置
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 配置文件列表
    /// </summary>
    public string[]? Files { get; set; }

    /// <summary>
    /// 文档链接
    /// </summary>
    public string? Documentation { get; set; }
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

    /// <summary>
    /// Discord社区
    /// </summary>
    public string? Discord { get; set; }
}

/// <summary>
/// 媒体信息
/// </summary>
public class MediaInfo
{
    /// <summary>
    /// 截图列表
    /// </summary>
    public string[]? Screenshots { get; set; }

    /// <summary>
    /// 视频链接
    /// </summary>
    public string? Video { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }
}

/// <summary>
/// 下载统计信息
/// </summary>
public class DownloadsInfo
{
    /// <summary>
    /// 总下载次数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 最近一个月下载次数
    /// </summary>
    public int LastMonth { get; set; }
}

/// <summary>
/// 评分信息
/// </summary>
public class RatingInfo
{
    /// <summary>
    /// 平均评分（0-5）
    /// </summary>
    public float Average { get; set; }

    /// <summary>
    /// 评分数量
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// 兼容性信息
/// </summary>
public class CompatibilityInfo
{
    /// <summary>
    /// CS2版本要求
    /// </summary>
    public string? Cs2Version { get; set; }

    /// <summary>
    /// 支持的平台列表
    /// </summary>
    public string[]? Platforms { get; set; }
}

/// <summary>
/// 元数据信息
/// </summary>
public class MetadataInfo
{
    /// <summary>
    /// 添加时间
    /// </summary>
    public DateTime? AddedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime? LastChecked { get; set; }
}

/// <summary>
/// 源代码信息（已弃用，保留用于向后兼容）
/// </summary>
[Obsolete("Use RepositoryInfo instead")]
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
