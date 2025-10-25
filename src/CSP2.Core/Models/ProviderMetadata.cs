namespace CSP2.Core.Models;

/// <summary>
/// Provider提供者元数据
/// </summary>
public class ProviderMetadata
{
    /// <summary>
    /// 提供者唯一标识（小写）
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// 提供者显示名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 优先级（数字越大优先级越高）
    /// </summary>
    public int Priority { get; set; } = 0;
}

