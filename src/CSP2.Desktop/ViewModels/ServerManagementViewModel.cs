using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using System.Collections.ObjectModel;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 服务器管理页面ViewModel
/// </summary>
public partial class ServerManagementViewModel : ObservableObject
{
    private readonly IServerManager _serverManager;

    [ObservableProperty]
    private ObservableCollection<Server> _servers = new();

    [ObservableProperty]
    private Server? _selectedServer;

    [ObservableProperty]
    private bool _isLoading;

    public ServerManagementViewModel(IServerManager serverManager)
    {
        _serverManager = serverManager;
        
        // 加载服务器列表
        _ = LoadServersAsync();
    }

    /// <summary>
    /// 加载服务器列表
    /// </summary>
    private async Task LoadServersAsync()
    {
        IsLoading = true;
        try
        {
            var servers = await _serverManager.GetServersAsync();
            Servers.Clear();
            foreach (var server in servers)
            {
                Servers.Add(server);
            }
        }
        catch (Exception ex)
        {
            // TODO: 显示错误消息
            System.Diagnostics.Debug.WriteLine($"加载服务器列表失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 添加服务器命令
    /// </summary>
    [RelayCommand]
    private async Task AddServerAsync()
    {
        // TODO: 显示添加服务器对话框
        // 暂时添加一个测试服务器
        try
        {
            var config = new ServerConfig
            {
                Port = 27015,
                Map = "de_dust2",
                MaxPlayers = 10,
                TickRate = 128
            };

            var name = $"测试服务器 {Servers.Count + 1}";
            var installPath = $@"C:\CS2Server\Server{Servers.Count + 1}";
            
            var server = await _serverManager.AddServerAsync(name, installPath, config);
            Servers.Add(server);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"添加服务器失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private async Task StartServerAsync()
    {
        if (SelectedServer == null) return;

        try
        {
            await _serverManager.StartServerAsync(SelectedServer.Id);
            // 服务器状态会通过事件更新
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"启动服务器失败: {ex.Message}");
        }
    }

    private bool CanStartServer()
    {
        return SelectedServer != null && SelectedServer.Status == ServerStatus.Stopped;
    }

    /// <summary>
    /// 停止服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopServer))]
    private async Task StopServerAsync()
    {
        if (SelectedServer == null) return;

        try
        {
            await _serverManager.StopServerAsync(SelectedServer.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"停止服务器失败: {ex.Message}");
        }
    }

    private bool CanStopServer()
    {
        return SelectedServer != null && SelectedServer.Status == ServerStatus.Running;
    }

    /// <summary>
    /// 重启服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRestartServer))]
    private async Task RestartServerAsync()
    {
        if (SelectedServer == null) return;

        try
        {
            await _serverManager.RestartServerAsync(SelectedServer.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"重启服务器失败: {ex.Message}");
        }
    }

    private bool CanRestartServer()
    {
        return SelectedServer != null && SelectedServer.Status == ServerStatus.Running;
    }

    /// <summary>
    /// 删除服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteServer))]
    private async Task DeleteServerAsync()
    {
        if (SelectedServer == null) return;

        try
        {
            // TODO: 显示确认对话框
            await _serverManager.DeleteServerAsync(SelectedServer.Id);
            Servers.Remove(SelectedServer);
            SelectedServer = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"删除服务器失败: {ex.Message}");
        }
    }

    private bool CanDeleteServer()
    {
        return SelectedServer != null && SelectedServer.Status == ServerStatus.Stopped;
    }

    /// <summary>
    /// 刷新列表命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadServersAsync();
    }
}

