using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
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
        IServerManager serverManager)
    {
        _pluginRepositoryService = pluginRepositoryService;
        _pluginManager = pluginManager;
        _serverManager = serverManager;

        _ = LoadDataAsync();
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            // 加载服务器列表
            var servers = await _serverManager.GetServersAsync();
            Servers.Clear();
            foreach (var server in servers)
            {
                Servers.Add(server);
            }
            SelectedServer = servers.FirstOrDefault();

            // 加载插件列表
            var manifest = await _pluginRepositoryService.GetManifestAsync();
            Plugins.Clear();
            foreach (var plugin in manifest.Plugins)
            {
                Plugins.Add(plugin);
            }

            // 初始化过滤列表
            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载数据失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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
            System.Diagnostics.Debug.WriteLine("请先选择一个服务器");
            return;
        }

        try
        {
            var progress = new Progress<ProgressInfo>(p =>
            {
                System.Diagnostics.Debug.WriteLine($"安装进度: {p.Percentage}% - {p.Message}");
            });

            var result = await _pluginManager.InstallPluginAsync(
                SelectedServer.Id, 
                plugin.Id, 
                progress);

            if (result.Success)
            {
                System.Diagnostics.Debug.WriteLine($"插件 {plugin.Name} 安装成功");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"插件安装失败: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"安装插件失败: {ex.Message}");
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

