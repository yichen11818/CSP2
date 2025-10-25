using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 插件市场ViewModel
/// </summary>
public partial class PluginMarketViewModel : ObservableObject
{
    private readonly IPluginRepositoryService _pluginRepositoryService;
    private readonly IPluginManager _pluginManager;
    private readonly IServerManager _serverManager;
    private readonly ILogger<PluginMarketViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<PluginInfo> _plugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginInfo> _filteredPlugins = new();

    [ObservableProperty]
    private ObservableCollection<Server> _servers = new();

    [ObservableProperty]
    private Server? _selectedServer;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "全部";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<string> Categories { get; } = new()
    {
        "全部",
        "游戏玩法",
        "管理员工具",
        "实用工具",
        "娱乐",
        "统计",
        "其他"
    };

    public PluginMarketViewModel(
        IPluginRepositoryService pluginRepositoryService,
        IPluginManager pluginManager,
        IServerManager serverManager,
        ILogger<PluginMarketViewModel> logger)
    {
        _pluginRepositoryService = pluginRepositoryService;
        _pluginManager = pluginManager;
        _serverManager = serverManager;
        _logger = logger;

        _logger.LogInformation("PluginMarketViewModel 初始化");
        DebugLogger.Debug("PluginMarketViewModel", "构造函数开始执行");

        _ = LoadDataAsync();
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        _logger.LogInformation("开始加载插件市场数据");
        DebugLogger.Debug("LoadDataAsync", "IsLoading = true");

        try
        {
            // 加载服务器列表
            DebugLogger.Debug("LoadDataAsync", "加载服务器列表");
            var servers = await _serverManager.GetServersAsync();
            Servers.Clear();
            foreach (var server in servers)
            {
                Servers.Add(server);
            }
            SelectedServer = servers.FirstOrDefault();
            _logger.LogInformation("加载了 {Count} 个服务器", servers.Count);

            // 加载插件列表
            DebugLogger.Debug("LoadDataAsync", "加载插件列表");
            var manifest = await _pluginRepositoryService.GetManifestAsync();
            Plugins.Clear();
            foreach (var plugin in manifest.Plugins)
            {
                Plugins.Add(plugin);
            }
            _logger.LogInformation("加载了 {Count} 个插件", manifest.Plugins.Count);

            // 初始化过滤列表
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载插件市场数据失败");
            DebugLogger.Error("LoadDataAsync", $"加载数据失败: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
            DebugLogger.Debug("LoadDataAsync", "IsLoading = false");
        }
    }

    /// <summary>
    /// 应用过滤
    /// </summary>
    private void ApplyFilter()
    {
        var filtered = Plugins.AsEnumerable();

        // 按分类过滤
        if (SelectedCategory != "全部")
        {
            filtered = filtered.Where(p => 
                p.Category?.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase) == true);
        }

        // 按搜索文本过滤
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.DescriptionZh?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.Author?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true));
        }

        FilteredPlugins.Clear();
        foreach (var plugin in filtered)
        {
            FilteredPlugins.Add(plugin);
        }
    }

    /// <summary>
    /// 安装插件
    /// </summary>
    [RelayCommand]
    private async Task InstallPluginAsync(PluginInfo plugin)
    {
        if (SelectedServer == null)
        {
            _logger.LogWarning("安装插件失败: 未选择服务器");
            DebugLogger.Warning("InstallPluginAsync", "请先选择一个服务器");
            return;
        }

        _logger.LogInformation("开始安装插件: {PluginName} 到服务器: {ServerName}", 
            plugin.Name, SelectedServer.Name);
        DebugLogger.Info("InstallPluginAsync", $"安装插件: {plugin.Name} -> {SelectedServer.Name}");

        try
        {
            var progress = new Progress<ProgressInfo>(p =>
            {
                DebugLogger.Debug("InstallPluginAsync", $"安装进度: {p.Percentage}% - {p.Message}");
            });

            var result = await _pluginManager.InstallPluginAsync(
                SelectedServer.Id, 
                plugin.Id, 
                progress);

            if (result.Success)
            {
                _logger.LogInformation("插件 {PluginName} 安装成功", plugin.Name);
                DebugLogger.Info("InstallPluginAsync", $"插件 {plugin.Name} 安装成功");
            }
            else
            {
                _logger.LogError("插件 {PluginName} 安装失败: {Error}", plugin.Name, result.ErrorMessage);
                DebugLogger.Error("InstallPluginAsync", $"插件安装失败: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装插件 {PluginName} 时发生异常", plugin.Name);
            DebugLogger.Error("InstallPluginAsync", $"安装插件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 刷新插件列表
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// 搜索文本变化
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// 分类变化
    /// </summary>
    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilter();
    }
}

