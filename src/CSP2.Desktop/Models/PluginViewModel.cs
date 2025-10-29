using CommunityToolkit.Mvvm.ComponentModel;
using CSP2.Core.Models;

namespace CSP2.Desktop.Models;

/// <summary>
/// 插件视图模型 - 包装 PluginInfo 并添加UI状态
/// </summary>
public partial class PluginViewModel : ObservableObject
{
    /// <summary>
    /// 插件信息
    /// </summary>
    public PluginInfo PluginInfo { get; }

    /// <summary>
    /// 是否已安装
    /// </summary>
    [ObservableProperty]
    private bool _isInstalled;

    /// <summary>
    /// 已安装的版本（null表示未安装）
    /// </summary>
    [ObservableProperty]
    private string? _installedVersion;

    /// <summary>
    /// 是否有更新
    /// </summary>
    [ObservableProperty]
    private bool _hasUpdate;

    /// <summary>
    /// 是否正在安装
    /// </summary>
    [ObservableProperty]
    private bool _isInstalling;

    public PluginViewModel(PluginInfo pluginInfo)
    {
        PluginInfo = pluginInfo;
    }

    // 转发 PluginInfo 的常用属性
    public string Id => PluginInfo.Id;
    public string Name => PluginInfo.Name;
    public string? Slug => PluginInfo.Slug;
    public AuthorInfo? Author => PluginInfo.Author;
    public string Description => PluginInfo.Description;
    public string? DescriptionZh => PluginInfo.DescriptionZh;
    public string Framework => PluginInfo.Framework;
    public string? FrameworkVersion => PluginInfo.FrameworkVersion;
    public string[] Dependencies => PluginInfo.Dependencies;
    public string Category => PluginInfo.Category;
    public string[] Tags => PluginInfo.Tags;
    public string Version => PluginInfo.Version;
    public string? Changelog => PluginInfo.Changelog;
    public DownloadInfo? Download => PluginInfo.Download;
    public string DownloadUrl => PluginInfo.DownloadUrl;
    public long DownloadSize => PluginInfo.DownloadSize;
    public RepositoryInfo? Repository => PluginInfo.Repository;
    public InstallationInfo? Installation => PluginInfo.Installation;
    public ConfigurationInfo? Configuration => PluginInfo.Configuration;
    public LinksInfo? Links => PluginInfo.Links;
    public MediaInfo? Media => PluginInfo.Media;
    public bool Verified => PluginInfo.Verified;
    public bool Featured => PluginInfo.Featured;
    public bool OfficialSupport => PluginInfo.OfficialSupport;
    public DownloadsInfo? Downloads => PluginInfo.Downloads;
    public RatingInfo? Rating => PluginInfo.Rating;
    public CompatibilityInfo? Compatibility => PluginInfo.Compatibility;
    public MetadataInfo? Metadata => PluginInfo.Metadata;

    /// <summary>
    /// 安装状态文本
    /// </summary>
    public string InstallStatusText
    {
        get
        {
            if (IsInstalling)
                return "安装中...";
            if (HasUpdate)
                return $"已安装 (v{InstalledVersion}) - 有更新";
            if (IsInstalled)
                return $"已安装 (v{InstalledVersion})";
            return "未安装";
        }
    }
}

