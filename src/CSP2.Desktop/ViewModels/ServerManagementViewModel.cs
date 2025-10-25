using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 服务器管理页面ViewModel
/// </summary>
public partial class ServerManagementViewModel : ObservableObject
{
    private readonly IServerManager _serverManager;
    private readonly ISteamCmdService _steamCmdService;
    private readonly ILogger<ServerManagementViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<Server> _servers = new();

    [ObservableProperty]
    private Server? _selectedServer;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isInstallingServer = false;

    [ObservableProperty]
    private double _serverInstallProgress = 0;

    [ObservableProperty]
    private string _serverInstallMessage = string.Empty;

    public ServerManagementViewModel(
        IServerManager serverManager,
        ISteamCmdService steamCmdService,
        ILogger<ServerManagementViewModel> logger)
    {
        _serverManager = serverManager;
        _steamCmdService = steamCmdService;
        _logger = logger;
        
        _logger.LogInformation("ServerManagementViewModel 初始化");
        
        // 订阅服务器状态变化事件
        _serverManager.StatusChanged += OnServerStatusChanged;
        
        // 加载服务器列表
        _ = LoadServersAsync();
    }

    /// <summary>
    /// 服务器状态变化处理
    /// </summary>
    private void OnServerStatusChanged(object? sender, ServerStatusChangedEventArgs e)
    {
        // 在UI线程更新服务器状态
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var server = Servers.FirstOrDefault(s => s.Id == e.ServerId);
            if (server != null)
            {
                server.Status = e.NewStatus;
                
                // 强制刷新集合（触发UI更新）
                var index = Servers.IndexOf(server);
                if (index >= 0)
                {
                    Servers.RemoveAt(index);
                    Servers.Insert(index, server);
                }
                
                // 如果是当前选中的服务器，也更新SelectedServer
                if (SelectedServer?.Id == e.ServerId)
                {
                    SelectedServer = server;
                }
                
                // 通知命令刷新（更新按钮启用状态）
                StartServerCommand.NotifyCanExecuteChanged();
                StopServerCommand.NotifyCanExecuteChanged();
                RestartServerCommand.NotifyCanExecuteChanged();
                DeleteServerCommand.NotifyCanExecuteChanged();
            }
        });
    }

    /// <summary>
    /// 加载服务器列表
    /// </summary>
    private async Task LoadServersAsync()
    {
        IsLoading = true;
        _logger.LogInformation("开始加载服务器列表");
        try
        {
            var servers = await _serverManager.GetServersAsync();
            Servers.Clear();
            foreach (var server in servers)
            {
                Servers.Add(server);
            }
            _logger.LogInformation("成功加载 {Count} 个服务器", Servers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载服务器列表失败");
            ShowError($"加载服务器列表失败: {ex.Message}");
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
        _logger.LogInformation("用户触发添加服务器操作");
        try
        {
            var config = new ServerConfig
            {
                Port = 27015 + Servers.Count,
                Map = "de_dust2",
                MaxPlayers = 10,
                TickRate = 128
            };

            var name = $"测试服务器 {Servers.Count + 1}";
            
            // 提示：这里使用测试路径，实际应该让用户选择路径
            // 用户需要：
            // 1. 已有CS2安装 - 通过Steam安装的CS2路径
            // 2. 或使用SteamCMD下载的独立服务器
            var installPath = $@"C:\CS2Server\Server{Servers.Count + 1}";
            
            _logger.LogInformation("正在添加服务器: {ServerName}, 路径: {InstallPath}, 端口: {Port}", 
                name, installPath, config.Port);
            
            var server = await _serverManager.AddServerAsync(name, installPath, config);
            Servers.Add(server);
            
            _logger.LogInformation("成功添加服务器: ID={ServerId}, 名称={ServerName}", 
                server.Id, server.Name);
            ShowSuccess($"已添加服务器: {name}");
        }
        catch (InvalidOperationException ex)
        {
            // 服务器验证失败的特定错误
            _logger.LogError(ex, "服务器路径验证失败");
            ShowError($"添加服务器失败\n\n{ex.Message}\n\n" +
                "提示：请参考文档 docs/04-CS2服务器配置指南.md 了解如何正确安装CS2服务器。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加服务器失败");
            ShowError($"添加服务器失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private async Task StartServerAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("启动服务器: {ServerName} (ID: {ServerId})", server.Name, server.Id);
            await _serverManager.StartServerAsync(server.Id);
            ShowSuccess($"服务器 {server.Name} 正在启动...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动服务器失败: {ServerName}", server.Name);
            ShowError($"启动服务器失败: {ex.Message}");
        }
    }

    private bool CanStartServer(Server? server)
    {
        return server != null && server.Status == ServerStatus.Stopped;
    }

    /// <summary>
    /// 停止服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopServer))]
    private async Task StopServerAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("停止服务器: {ServerName} (ID: {ServerId})", server.Name, server.Id);
            await _serverManager.StopServerAsync(server.Id);
            ShowSuccess($"服务器 {server.Name} 正在停止...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止服务器失败: {ServerName}", server.Name);
            ShowError($"停止服务器失败: {ex.Message}");
        }
    }

    private bool CanStopServer(Server? server)
    {
        return server != null && server.Status == ServerStatus.Running;
    }

    /// <summary>
    /// 重启服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRestartServer))]
    private async Task RestartServerAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("重启服务器: {ServerName} (ID: {ServerId})", server.Name, server.Id);
            await _serverManager.RestartServerAsync(server.Id);
            ShowSuccess($"服务器 {server.Name} 正在重启...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启服务器失败: {ServerName}", server.Name);
            ShowError($"重启服务器失败: {ex.Message}");
        }
    }

    private bool CanRestartServer(Server? server)
    {
        return server != null && server.Status == ServerStatus.Running;
    }

    /// <summary>
    /// 删除服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteServer))]
    private async Task DeleteServerAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("删除服务器: {ServerName} (ID: {ServerId})", server.Name, server.Id);
            // TODO: 显示确认对话框
            await _serverManager.DeleteServerAsync(server.Id);
            Servers.Remove(server);
            if (SelectedServer?.Id == server.Id)
            {
                SelectedServer = null;
            }
            ShowSuccess($"已删除服务器: {server.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除服务器失败: {ServerName}", server.Name);
            ShowError($"删除服务器失败: {ex.Message}");
        }
    }

    private bool CanDeleteServer(Server? server)
    {
        return server != null && server.Status == ServerStatus.Stopped;
    }

    /// <summary>
    /// 刷新列表命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadServersAsync();
    }

    /// <summary>
    /// 使用 SteamCMD 安装服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstallServer))]
    private async Task InstallServerAsync()
    {
        try
        {
            // 检查 SteamCMD 是否已安装
            if (!await _steamCmdService.IsSteamCmdInstalledAsync())
            {
                var result = System.Windows.MessageBox.Show(
                    "SteamCMD 未安装。是否现在安装？\n\n" +
                    "SteamCMD 是下载和管理 CS2 服务器文件所必需的工具。",
                    "需要 SteamCMD",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.No)
                {
                    return;
                }

                // 安装 SteamCMD
                _logger.LogInformation("开始安装 SteamCMD");
                IsInstallingServer = true;
                ServerInstallProgress = 0;
                ServerInstallMessage = "正在安装 SteamCMD...";

                var downloadProgress = new Progress<DownloadProgress>(p =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ServerInstallProgress = p.Percentage * 0.3; // SteamCMD 占30%
                        ServerInstallMessage = p.Message;
                    });
                });

                var steamCmdPath = _steamCmdService.GetSteamCmdPath();
                if (!await _steamCmdService.InstallSteamCmdAsync(steamCmdPath, downloadProgress))
                {
                    ShowError("SteamCMD 安装失败，请查看日志了解详情。");
                    return;
                }
            }

            // 请求用户输入服务器信息
            // TODO: 创建一个对话框让用户输入服务器名称和安装路径
            // 暂时使用固定路径
            var serverName = $"CS2 服务器 {Servers.Count + 1}";
            var serverPath = $@"C:\CS2Servers\Server{Servers.Count + 1}";
            
            _logger.LogInformation("开始安装 CS2 服务器到: {Path}", serverPath);
            IsInstallingServer = true;
            ServerInstallProgress = 30;
            ServerInstallMessage = "正在下载 CS2 服务器文件...";

            var installProgress = new Progress<InstallProgress>(p =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ServerInstallProgress = 30 + (p.Percentage * 0.7); // 服务器安装占70%
                    ServerInstallMessage = p.Message;
                });
            });

            var success = await _steamCmdService.InstallOrUpdateServerAsync(serverPath, false, installProgress);

            if (success)
            {
                _logger.LogInformation("CS2 服务器安装成功，正在添加到列表");
                ServerInstallMessage = "正在添加服务器到列表...";

                var config = new ServerConfig
                {
                    Port = 27015 + Servers.Count,
                    Map = "de_dust2",
                    MaxPlayers = 10,
                    TickRate = 128,
                    ServerName = serverName
                };

                var server = await _serverManager.AddServerAsync(serverName, serverPath, config);
                Servers.Add(server);

                ServerInstallProgress = 100;
                ServerInstallMessage = "✅ 服务器安装完成！";
                ShowSuccess($"服务器 '{serverName}' 安装成功！");

                // 3秒后清除进度
                await Task.Delay(3000);
                IsInstallingServer = false;
            }
            else
            {
                ShowError("CS2 服务器安装失败，请查看日志了解详情。");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装服务器失败");
            ShowError($"安装服务器失败：{ex.Message}");
        }
        finally
        {
            IsInstallingServer = false;
        }
    }

    private bool CanInstallServer() => !IsInstallingServer && !IsLoading;

    /// <summary>
    /// 显示成功消息
    /// </summary>
    private void ShowSuccess(string message)
    {
        HasError = false;
        StatusMessage = $"✅ {message}";
        
        // 3秒后自动清除消息
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            StatusMessage = string.Empty;
        });
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    private void ShowError(string message)
    {
        HasError = true;
        StatusMessage = $"❌ {message}";
        
        // 5秒后自动清除消息
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            StatusMessage = string.Empty;
            HasError = false;
        });
    }
}

