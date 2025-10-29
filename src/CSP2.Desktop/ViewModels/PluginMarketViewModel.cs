using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Logging;
using CSP2.Core.Models;
using CSP2.Core.Services;
using CSP2.Desktop.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 插件市场ViewModel
/// </summary>
public partial class PluginMarketViewModel : ObservableObject
{
    private readonly IPluginRepositoryService _pluginRepositoryService;
    private readonly IPluginManager _pluginManager;
    private readonly IServerManager _serverManager;
    private readonly ProviderRegistry _providerRegistry;
    private readonly ILogger<PluginMarketViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<PluginInfo> _plugins = new();

    [ObservableProperty]
    private ObservableCollection<PluginViewModel> _filteredPlugins = new();
    
    // 已安装插件的缓存
    private List<InstalledPlugin> _installedPlugins = new();

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

    // 框架相关属性
    [ObservableProperty]
    private bool _cssInstalled;

    [ObservableProperty]
    private bool _metamodInstalled;

    [ObservableProperty]
    private string? _cssVersion;

    [ObservableProperty]
    private string? _metamodVersion;

    [ObservableProperty]
    private bool _isInstallingCss;

    [ObservableProperty]
    private bool _isInstallingMetamod;

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
        ProviderRegistry providerRegistry,
        ILogger<PluginMarketViewModel> logger)
    {
        _pluginRepositoryService = pluginRepositoryService;
        _pluginManager = pluginManager;
        _serverManager = serverManager;
        _providerRegistry = providerRegistry;
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
                DebugLogger.Debug("LoadDataAsync", $"  添加服务器: {server.Name} (ID: {server.Id})");
            }
            
            var firstServer = servers.FirstOrDefault();
            if (firstServer != null)
            {
                DebugLogger.Info("LoadDataAsync", $"设置默认选择服务器: {firstServer.Name} (ID: {firstServer.Id})");
                SelectedServer = firstServer;
            }
            else
            {
                DebugLogger.Warning("LoadDataAsync", "没有可用的服务器！");
            }
            
            _logger.LogInformation("加载了 {Count} 个服务器，当前选择: {SelectedServer}", 
                servers.Count, SelectedServer?.Name ?? "null");

            // 加载插件列表
            DebugLogger.Debug("LoadDataAsync", "开始调用 GetManifestAsync");
            var manifest = await _pluginRepositoryService.GetManifestAsync();
            
            DebugLogger.Info("LoadDataAsync", $"GetManifestAsync 返回，包含 {manifest?.Plugins?.Count ?? 0} 个插件");
            
            if (manifest == null)
            {
                DebugLogger.Error("LoadDataAsync", "manifest 为 null！");
                return;
            }
            
            if (manifest.Plugins == null)
            {
                DebugLogger.Error("LoadDataAsync", "manifest.Plugins 为 null！");
                manifest.Plugins = new List<PluginInfo>();
            }
            
            Plugins.Clear();
            DebugLogger.Debug("LoadDataAsync", $"开始添加 {manifest.Plugins.Count} 个插件到集合");
            
            foreach (var plugin in manifest.Plugins)
            {
                Plugins.Add(plugin);
                DebugLogger.Debug("LoadDataAsync", $"  添加插件: {plugin.Name} (ID: {plugin.Id})");
            }
            
            _logger.LogInformation("加载了 {Count} 个插件", manifest.Plugins.Count);
            DebugLogger.Info("LoadDataAsync", $"插件加载完成，Plugins.Count = {Plugins.Count}");

            // 初始化过滤列表
            DebugLogger.Debug("LoadDataAsync", "开始应用过滤");
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
        DebugLogger.Debug("ApplyFilter", $"开始过滤，总插件数: {Plugins.Count}");
        
        var filtered = Plugins.AsEnumerable();

        // 按分类过滤
        if (SelectedCategory != "全部")
        {
            var beforeCount = filtered.Count();
            filtered = filtered.Where(p => 
                p.Category?.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase) == true);
            var afterCount = filtered.Count();
            DebugLogger.Debug("ApplyFilter", $"分类过滤 '{SelectedCategory}': {beforeCount} -> {afterCount}");
        }

        // 按搜索文本过滤
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var beforeCount = filtered.Count();
            filtered = filtered.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (p.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.DescriptionZh?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.Author?.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true));
            var afterCount = filtered.Count();
            DebugLogger.Debug("ApplyFilter", $"搜索文本过滤 '{SearchText}': {beforeCount} -> {afterCount}");
        }

        FilteredPlugins.Clear();
        var filteredList = filtered.ToList();
        foreach (var plugin in filteredList)
        {
            // 创建PluginViewModel并设置安装状态
            var viewModel = new PluginViewModel(plugin);
            UpdatePluginInstallStatus(viewModel);
            FilteredPlugins.Add(viewModel);
        }
        
        DebugLogger.Info("ApplyFilter", $"过滤完成，显示 {FilteredPlugins.Count} 个插件");
        
        if (FilteredPlugins.Count > 0)
        {
            var pluginNames = string.Join(", ", FilteredPlugins.Take(5).Select(p => p.Name));
            DebugLogger.Debug("ApplyFilter", $"前5个插件: {pluginNames}...");
        }
        else
        {
            DebugLogger.Warning("ApplyFilter", "过滤后无插件显示！");
        }
    }

    /// <summary>
    /// 更新插件的安装状态
    /// </summary>
    private void UpdatePluginInstallStatus(PluginViewModel pluginViewModel)
    {
        var installedPlugin = _installedPlugins.FirstOrDefault(p => p.Id == pluginViewModel.Id);
        
        if (installedPlugin != null)
        {
            pluginViewModel.IsInstalled = true;
            pluginViewModel.InstalledVersion = installedPlugin.Version;
            
            // 检查是否有更新（简单的版本字符串比较）
            if (installedPlugin.Version != pluginViewModel.Version)
            {
                pluginViewModel.HasUpdate = true;
                DebugLogger.Debug("UpdatePluginInstallStatus", 
                    $"插件 {pluginViewModel.Name} 有更新: {installedPlugin.Version} -> {pluginViewModel.Version}");
            }
        }
        else
        {
            pluginViewModel.IsInstalled = false;
            pluginViewModel.InstalledVersion = null;
            pluginViewModel.HasUpdate = false;
        }
    }

    /// <summary>
    /// 刷新已安装插件列表
    /// </summary>
    private async Task RefreshInstalledPluginsAsync()
    {
        if (SelectedServer == null)
        {
            _installedPlugins.Clear();
            return;
        }

        try
        {
            _installedPlugins = await _pluginManager.GetInstalledPluginsAsync(SelectedServer.Id);
            DebugLogger.Info("RefreshInstalledPlugins", 
                $"已加载 {_installedPlugins.Count} 个已安装插件");
            
            if (_installedPlugins.Count > 0)
            {
                var pluginNames = string.Join(", ", _installedPlugins.Select(p => $"{p.Name} v{p.Version}"));
                DebugLogger.Debug("RefreshInstalledPlugins", $"已安装插件列表: {pluginNames}");
            }
            
            // 更新所有已显示插件的安装状态
            foreach (var plugin in FilteredPlugins)
            {
                UpdatePluginInstallStatus(plugin);
            }
            
            DebugLogger.Debug("RefreshInstalledPlugins", $"已更新 {FilteredPlugins.Count} 个插件的显示状态");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新已安装插件列表失败");
            DebugLogger.Error("RefreshInstalledPlugins", $"刷新失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 安装插件
    /// </summary>
    [RelayCommand]
    private async Task InstallPluginAsync(PluginViewModel plugin)
    {
        if (SelectedServer == null)
        {
            _logger.LogWarning("安装插件失败: 未选择服务器");
            DebugLogger.Warning("InstallPluginAsync", "请先选择一个服务器");
            MessageBox.Show("请先在左侧选择一个服务器", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _logger.LogInformation("开始安装插件: {PluginName} 到服务器: {ServerName}", 
            plugin.Name, SelectedServer.Name);
        DebugLogger.Info("InstallPluginAsync", $"安装插件: {plugin.Name} -> {SelectedServer.Name}");

        // 创建进度对话框
        var progressDialog = new Views.Dialogs.FrameworkInstallProgressDialog($"插件: {plugin.Name}");
        progressDialog.Owner = Application.Current.MainWindow;
        
        try
        {
            var progress = new Progress<InstallProgress>(p =>
            {
                DebugLogger.Debug("InstallPluginAsync", $"安装进度: {p.Percentage}% - {p.Message}");
                progressDialog.UpdateProgress(p);
            });

            // 显示进度对话框（非模态）
            progressDialog.Show();

            // 设置正在安装状态
            plugin.IsInstalling = true;
            
            var result = await _pluginManager.InstallPluginAsync(
                SelectedServer.Id, 
                plugin.Id, 
                progress);

            if (result.Success)
            {
                _logger.LogInformation("插件 {PluginName} 安装成功", plugin.Name);
                DebugLogger.Info("InstallPluginAsync", $"插件 {plugin.Name} 安装成功");
                
                // 构建成功消息
                var successMessage = $"插件 {plugin.Name} 安装成功！";
                if (result.InstalledDependencies.Count > 0)
                {
                    var dependencyNames = string.Join(", ", result.InstalledDependencies.Values);
                    successMessage = $"插件 {plugin.Name} 及其 {result.InstalledDependencies.Count} 个依赖安装成功！\n\n已安装依赖：{dependencyNames}";
                    DebugLogger.Info("InstallPluginAsync", $"同时安装了 {result.InstalledDependencies.Count} 个依赖插件");
                }
                
                progressDialog.ShowSuccess(successMessage);
                
                // 刷新已安装插件列表
                await RefreshInstalledPluginsAsync();
                
                // 延迟关闭对话框
                await Task.Delay(2000);
                progressDialog.Close();
                
                // 构建详细消息
                var detailMessage = $"插件 {plugin.Name} 安装成功！\n\n";
                if (result.InstalledDependencies.Count > 0)
                {
                    detailMessage += $"同时自动安装了以下 {result.InstalledDependencies.Count} 个依赖插件：\n";
                    foreach (var dep in result.InstalledDependencies)
                    {
                        detailMessage += $"  • {dep.Value}\n";
                    }
                    detailMessage += "\n";
                }
                detailMessage += (plugin.Installation?.RequiresRestart == true ? "请重启服务器以加载插件。" : "插件已就绪。");
                
                MessageBox.Show(detailMessage, "安装成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var errorMsg = result.ErrorMessage ?? "未知错误";
                _logger.LogError("插件 {PluginName} 安装失败: {Error}", plugin.Name, errorMsg);
                DebugLogger.Error("InstallPluginAsync", $"❌ 插件 {plugin.Name} 安装失败");
                DebugLogger.Error("InstallPluginAsync", $"   错误原因: {errorMsg}");
                
                progressDialog.ShowError(errorMsg);
                
                MessageBox.Show($"插件安装失败：\n\n{errorMsg}", 
                    "安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装插件 {PluginName} 时发生异常", plugin.Name);
            DebugLogger.Error("InstallPluginAsync", $"安装插件失败: {ex.Message}", ex);
            progressDialog.ShowError($"安装异常: {ex.Message}");
            
            MessageBox.Show($"安装插件时发生异常：\n\n{ex.Message}", 
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            plugin.IsInstalling = false;
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

    /// <summary>
    /// 服务器变化时，检查框架安装状态和已安装插件
    /// </summary>
    async partial void OnSelectedServerChanged(Server? value)
    {
        _logger.LogInformation("服务器选择已变更: {ServerName}", value?.Name ?? "null");
        DebugLogger.Info("OnSelectedServerChanged", $"新选择的服务器: {value?.Name ?? "null"} (ID: {value?.Id ?? "null"})");
        
        if (value != null)
        {
            await CheckFrameworksStatusAsync();
            await RefreshInstalledPluginsAsync();
        }
        else
        {
            // 清空框架状态
            CssInstalled = false;
            MetamodInstalled = false;
            CssVersion = null;
            MetamodVersion = null;
            _installedPlugins.Clear();
            
            // 更新所有插件的安装状态
            foreach (var plugin in FilteredPlugins)
            {
                UpdatePluginInstallStatus(plugin);
            }
        }
    }

    /// <summary>
    /// 检查框架安装状态
    /// </summary>
    private async Task CheckFrameworksStatusAsync()
    {
        if (SelectedServer == null)
            return;

        try
        {
            // 检查 Metamod
            var metamodProvider = _providerRegistry.GetFrameworkProvider("metamod");
            if (metamodProvider != null)
            {
                MetamodInstalled = await metamodProvider.IsInstalledAsync(SelectedServer.InstallPath);
                MetamodVersion = await metamodProvider.GetInstalledVersionAsync(SelectedServer.InstallPath);
                DebugLogger.Info("CheckFrameworks", $"Metamod 安装状态: {MetamodInstalled}, 版本: {MetamodVersion}");
            }

            // 检查 CounterStrikeSharp
            var cssProvider = _providerRegistry.GetFrameworkProvider("counterstrikesharp");
            if (cssProvider != null)
            {
                CssInstalled = await cssProvider.IsInstalledAsync(SelectedServer.InstallPath);
                CssVersion = await cssProvider.GetInstalledVersionAsync(SelectedServer.InstallPath);
                DebugLogger.Info("CheckFrameworks", $"CSS 安装状态: {CssInstalled}, 版本: {CssVersion}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查框架状态失败");
            DebugLogger.Error("CheckFrameworks", $"检查框架状态失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 安装 Metamod
    /// </summary>
    [RelayCommand]
    private async Task InstallMetamodAsync()
    {
        if (SelectedServer == null)
        {
            MessageBox.Show("请先选择一个服务器", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IsInstallingMetamod)
            return;

        IsInstallingMetamod = true;
        DebugLogger.Info("InstallMetamod", $"开始安装 Metamod 到服务器: {SelectedServer.Name}");

        // 创建进度对话框
        var progressDialog = new Views.Dialogs.FrameworkInstallProgressDialog("Metamod:Source");
        
        try
        {
            var provider = _providerRegistry.GetFrameworkProvider("metamod");
            if (provider == null)
            {
                DebugLogger.Error("InstallMetamod", "未找到 Metamod Provider");
                MessageBox.Show("未找到 Metamod 安装程序", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var progress = new Progress<InstallProgress>(p =>
            {
                DebugLogger.Debug("InstallMetamod", $"进度: {p.Percentage:F1}% - {p.Message}");
                progressDialog.UpdateProgress(p);
            });

            // 显示进度对话框（非模态）
            progressDialog.Owner = Application.Current.MainWindow;
            progressDialog.Show();

            var result = await provider.InstallAsync(SelectedServer.InstallPath, null, progress);

            if (result.Success)
            {
                _logger.LogInformation("Metamod 安装成功");
                DebugLogger.Info("InstallMetamod", "Metamod 安装成功");
                progressDialog.ShowSuccess("Metamod:Source 安装成功！");
                await CheckFrameworksStatusAsync();
                
                // 延迟关闭对话框
                await Task.Delay(1500);
                progressDialog.Close();
            }
            else
            {
                _logger.LogError("Metamod 安装失败: {Error}", result.ErrorMessage);
                DebugLogger.Error("InstallMetamod", $"安装失败: {result.ErrorMessage}");
                progressDialog.ShowError(result.ErrorMessage ?? "未知错误");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装 Metamod 时发生异常");
            DebugLogger.Error("InstallMetamod", $"安装异常: {ex.Message}", ex);
            progressDialog.ShowError($"安装异常: {ex.Message}");
        }
        finally
        {
            IsInstallingMetamod = false;
        }
    }

    /// <summary>
    /// 安装 CounterStrikeSharp
    /// </summary>
    [RelayCommand]
    private async Task InstallCssAsync()
    {
        if (SelectedServer == null)
        {
            MessageBox.Show("请先选择一个服务器", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (IsInstallingCss)
            return;

        IsInstallingCss = true;
        DebugLogger.Info("InstallCss", $"开始安装 CounterStrikeSharp 到服务器: {SelectedServer.Name}");

        // 创建进度对话框
        var progressDialog = new Views.Dialogs.FrameworkInstallProgressDialog("CounterStrikeSharp");
        progressDialog.Owner = Application.Current.MainWindow;
        progressDialog.Show();

        try
        {
            // 检查 Metamod 依赖
            if (!MetamodInstalled)
            {
                DebugLogger.Info("InstallCss", "CounterStrikeSharp 依赖 Metamod，先安装 Metamod");
                progressDialog.ShowInstallingDependency("Metamod:Source");
                
                var metamodProvider = _providerRegistry.GetFrameworkProvider("metamod");
                if (metamodProvider == null)
                {
                    var errorMsg = "无法找到 Metamod 安装程序，无法继续安装";
                    DebugLogger.Error("InstallCss", errorMsg);
                    progressDialog.ShowError(errorMsg);
                    return;
                }

                var metamodProgress = new Progress<InstallProgress>(p =>
                {
                    DebugLogger.Debug("InstallCss", $"[Metamod依赖] {p.Percentage:F1}% - {p.Message}");
                    // 依赖安装进度映射到 0-50%
                    var mappedProgress = new InstallProgress
                    {
                        Percentage = p.Percentage * 0.5,
                        CurrentStep = $"安装依赖: {p.CurrentStep}",
                        Message = p.Message,
                        CurrentStepIndex = p.CurrentStepIndex,
                        TotalSteps = p.TotalSteps
                    };
                    progressDialog.UpdateProgress(mappedProgress);
                });

                var metamodResult = await metamodProvider.InstallAsync(SelectedServer.InstallPath, null, metamodProgress);
                if (!metamodResult.Success)
                {
                    var errorMsg = $"Metamod 依赖安装失败: {metamodResult.ErrorMessage}";
                    _logger.LogError(errorMsg);
                    DebugLogger.Error("InstallCss", errorMsg);
                    progressDialog.ShowError(errorMsg);
                    return;
                }

                await CheckFrameworksStatusAsync();
                DebugLogger.Info("InstallCss", "✓ Metamod 依赖安装成功，继续安装 CSS");
            }

            var cssProvider = _providerRegistry.GetFrameworkProvider("counterstrikesharp");
            if (cssProvider == null)
            {
                var errorMsg = "未找到 CounterStrikeSharp 安装程序";
                DebugLogger.Error("InstallCss", errorMsg);
                progressDialog.ShowError(errorMsg);
                return;
            }

            var progress = new Progress<InstallProgress>(p =>
            {
                DebugLogger.Debug("InstallCss", $"进度: {p.Percentage:F1}% - {p.Message}");
                
                // 如果之前安装了 Metamod，则将 CSS 安装进度映射到 50-100%
                if (!MetamodInstalled)
                {
                    var mappedProgress = new InstallProgress
                    {
                        Percentage = 50 + (p.Percentage * 0.5),
                        CurrentStep = p.CurrentStep,
                        Message = p.Message,
                        CurrentStepIndex = p.CurrentStepIndex,
                        TotalSteps = p.TotalSteps
                    };
                    progressDialog.UpdateProgress(mappedProgress);
                }
                else
                {
                    progressDialog.UpdateProgress(p);
                }
            });

            var result = await cssProvider.InstallAsync(SelectedServer.InstallPath, null, progress);

            if (result.Success)
            {
                _logger.LogInformation("CounterStrikeSharp 安装成功");
                DebugLogger.Info("InstallCss", "CounterStrikeSharp 安装成功");
                progressDialog.ShowSuccess("CounterStrikeSharp 安装成功！");
                await CheckFrameworksStatusAsync();
                
                // 延迟关闭对话框
                await Task.Delay(1500);
                progressDialog.Close();
            }
            else
            {
                _logger.LogError("CounterStrikeSharp 安装失败: {Error}", result.ErrorMessage);
                DebugLogger.Error("InstallCss", $"安装失败: {result.ErrorMessage}");
                progressDialog.ShowError(result.ErrorMessage ?? "未知错误");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装 CounterStrikeSharp 时发生异常");
            DebugLogger.Error("InstallCss", $"安装异常: {ex.Message}", ex);
            progressDialog.ShowError($"安装异常: {ex.Message}");
        }
        finally
        {
            IsInstallingCss = false;
        }
    }
}

