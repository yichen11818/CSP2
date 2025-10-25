using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Desktop.Services;
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
    private readonly JsonLocalizationService _localizationService;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _selectedMenuItem;

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

    public MainWindowViewModel(IServiceProvider serviceProvider, IDownloadManager downloadManager, JsonLocalizationService localizationService)
    {
        _serviceProvider = serviceProvider;
        _downloadManager = downloadManager;
        _localizationService = localizationService;
        
        // 初始化本地化字符串
        _statusText = _localizationService.GetString("Status.ReadyText");
        _selectedMenuItem = _localizationService.GetString("Nav.ServerManagement");
        
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
            StatusText = _localizationService.GetString("Download.Completed", e.Name);
        });
    }

    private void OnDownloadTaskFailed(object? sender, Core.Models.DownloadTask e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateDownloadStatus();
            StatusText = _localizationService.GetString("Download.Failed", e.Name);
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
        SelectedMenuItem = _localizationService.GetString("Nav.ServerManagement");
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.ServerManagementPage>();
        StatusText = _localizationService.GetString("Nav.ServerManagement");
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
            _localizationService,
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
        SelectedMenuItem = _localizationService.GetString("Nav.LogConsole");
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.LogConsolePage>();
        StatusText = _localizationService.GetString("Nav.LogConsole");
    }

    [RelayCommand]
    private void NavigateToPluginMarket()
    {
        SelectedMenuItem = _localizationService.GetString("Nav.PluginMarket");
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.PluginMarketPage>();
        StatusText = _localizationService.GetString("Nav.PluginMarket");
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        SelectedMenuItem = _localizationService.GetString("Nav.Settings");
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.SettingsPage>();
        StatusText = _localizationService.GetString("Nav.Settings");
    }

    [RelayCommand]
    private void NavigateToDebugConsole()
    {
        SelectedMenuItem = _localizationService.GetString("Nav.DebugConsole");
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.DebugConsolePage>();
        StatusText = _localizationService.GetString("Nav.DebugConsole");
        DebugLogger.Info("MainWindow", "已切换到Debug控制台");
    }

    [RelayCommand]
    private void NavigateToDownloadManager()
    {
        SelectedMenuItem = _localizationService.GetString("Nav.DownloadManager");
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.DownloadManagerPage>();
        StatusText = _localizationService.GetString("Nav.DownloadManager");
        DebugLogger.Info("MainWindow", "已切换到下载管理页面");
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void OpenDownloadManager()
    {
        // 悬浮球点击：导航到下载管理页面
        DebugLogger.Debug("OpenDownloadManager", "从悬浮球导航到下载管理页面");
        NavigateToDownloadManager();
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
            StatusText = _localizationService.GetString("Status.ReadyText");
        }
        else
        {
            StatusText = _localizationService.GetString("Msg.Downloading", ActiveDownloadCount);
        }
    }
}

