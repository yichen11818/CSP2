using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 主窗口ViewModel
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _statusText = "就绪 - CSP2 v0.1.0";

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _selectedMenuItem = "服务器";

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // 初始化 - 默认显示服务器管理页面
        NavigateToServerManagement();
    }

    [RelayCommand]
    private void NavigateToServerManagement()
    {
        SelectedMenuItem = "服务器";
        CurrentPage = _serviceProvider.GetRequiredService<Views.Pages.ServerManagementPage>();
        StatusText = "服务器管理";
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
}

