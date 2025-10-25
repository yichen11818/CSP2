using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 主窗口ViewModel
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDownloadManager _downloadManager;

    [ObservableProperty]
    private string _statusText = "就绪 - CSP2 v0.1.0";

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _selectedMenuItem = "服务器";

    [ObservableProperty]
    private bool _isDebugMode;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private bool _hasActiveDownloads = false;

    [ObservableProperty]
    private int _activeDownloadCount = 0;

    [ObservableProperty]
    private double _downloadProgress = 0.0;

    public MainWindowViewModel(IServiceProvider serviceProvider, IDownloadManager downloadManager)
    {
        _serviceProvider = serviceProvider;
        _downloadManager = downloadManager;
        
        // 检查是否为Debug模式
        IsDebugMode = DebugLogger.IsDebugMode;
        
        // 订阅下载管理器事件
        _downloadManager.TaskAdded += OnDownloadTaskAdded;
        _downloadManager.TaskUpdated += OnDownloadTaskUpdated;
        _downloadManager.TaskCompleted += OnDownloadTaskCompleted;
        _downloadManager.TaskFailed += OnDownloadTaskFailed;
        
        // 初始化 - 默认显示服务器管理页面
        NavigateToServerManagement();
        
        if (IsDebugMode)
        {
            DebugLogger.Info("MainWindow", "主窗口ViewModel已初始化 (Debug模式)");
        }
    }

    private void OnDownloadTaskAdded(object? sender, Core.Models.DownloadTask e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateDownloadStatus();
        });
    }

    private void OnDownloadTaskUpdated(object? sender, Core.Models.DownloadTask e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateDownloadStatus();
        });
    }

    private void OnDownloadTaskCompleted(object? sender, Core.Models.DownloadTask e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateDownloadStatus();
            StatusText = $"下载完成: {e.Name}";
        });
    }

    private void OnDownloadTaskFailed(object? sender, Core.Models.DownloadTask e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateDownloadStatus();
            StatusText = $"下载失败: {e.Name}";
        });
    }

    private void UpdateDownloadStatus()
    {
        ActiveDownloadCount = _downloadManager.ActiveTaskCount;
        HasActiveDownloads = ActiveDownloadCount > 0;
    }

    [RelayCommand]
    private void NavigateToServerManagement()
    {
        SelectedMenuItem = "服务器";
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.ServerManagementPage>();
        StatusText = "服务器管理";
    }

    /// <summary>
    /// 导航到服务器安装页面
    /// </summary>
    public void NavigateToServerInstall(Action<Core.Models.Server>? onComplete = null, Action? onCancel = null)
    {
        // 直接从ServiceProvider获取Logger
        var logger = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServerInstallPageViewModel>>();
        
        // 创建安装页面ViewModel
        var viewModel = new ServerInstallPageViewModel(
            _serviceProvider.GetRequiredService<IServerManager>(),
            _serviceProvider.GetRequiredService<ISteamCmdService>(),
            _serviceProvider.GetRequiredService<Core.Services.CS2PathDetector>(),
            logger,
            onInstallComplete: server =>
            {
                onComplete?.Invoke(server);
                NavigateToServerManagement();
            },
            onCancel: () =>
            {
                onCancel?.Invoke();
                NavigateToServerManagement();
            });

        // 创建页面并设置DataContext
        var page = new Views.Pages.ServerInstallPage
        {
            DataContext = viewModel
        };

        CurrentPage = page;
        SelectedMenuItem = "";
        StatusText = "安装服务器";
    }

    [RelayCommand]
    private void NavigateToLogConsole()
    {
        SelectedMenuItem = "日志";
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.LogConsolePage>();
        StatusText = "日志控制台";
    }

    [RelayCommand]
    private void NavigateToPluginMarket()
    {
        SelectedMenuItem = "插件";
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.PluginMarketPage>();
        StatusText = "插件市场";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        SelectedMenuItem = "设置";
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.SettingsPage>();
        StatusText = "设置";
    }

    [RelayCommand]
    private void NavigateToDebugConsole()
    {
        SelectedMenuItem = "Debug";
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.DebugConsolePage>();
        StatusText = "Debug控制台";
        DebugLogger.Info("MainWindow", "已切换到Debug控制台");
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void OpenDownloadManager()
    {
        DebugLogger.Debug("OpenDownloadManager", "打开下载管理器窗口");
        
        try
        {
            var downloadWindow = new Views.DownloadManagerWindow();
            var viewModel = _serviceProvider.GetRequiredService<DownloadManagerViewModel>();
            downloadWindow.DataContext = viewModel;
            downloadWindow.Owner = Application.Current.MainWindow;
            downloadWindow.ShowDialog();
            DebugLogger.Debug("OpenDownloadManager", "下载管理器窗口已关闭");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("OpenDownloadManager", $"打开下载管理器失败: {ex.Message}", ex);
            StatusText = $"打开下载管理器失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 模拟开始下载（用于测试）
    /// </summary>
    public void StartMockDownload()
    {
        HasActiveDownloads = true;
        ActiveDownloadCount++;
        StatusText = $"正在下载 {ActiveDownloadCount} 个文件...";
    }

    /// <summary>
    /// 模拟停止下载（用于测试）
    /// </summary>
    public void StopMockDownload()
    {
        if (ActiveDownloadCount > 0)
        {
            ActiveDownloadCount--;
        }
        
        if (ActiveDownloadCount == 0)
        {
            HasActiveDownloads = false;
            StatusText = "就绪 - CSP2 v0.1.0";
        }
        else
        {
            StatusText = $"正在下载 {ActiveDownloadCount} 个文件...";
        }
    }
}

