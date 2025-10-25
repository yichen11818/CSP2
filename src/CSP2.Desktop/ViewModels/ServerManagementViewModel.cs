using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using CSP2.Core.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 服务器管理页面ViewModel
/// </summary>
public partial class ServerManagementViewModel : ObservableObject
{
    private readonly IServerManager _serverManager;
    private readonly ISteamCmdService _steamCmdService;
    private readonly CS2PathDetector _pathDetector;
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
        CS2PathDetector pathDetector,
        ILogger<ServerManagementViewModel> logger)
    {
        _serverManager = serverManager;
        _steamCmdService = steamCmdService;
        _pathDetector = pathDetector;
        _logger = logger;
        
        _logger.LogInformation("ServerManagementViewModel 初始化");
        DebugLogger.Debug("ServerManagementViewModel", "构造函数开始执行");
        
        // 订阅服务器状态变化事件
        _serverManager.StatusChanged += OnServerStatusChanged;
        DebugLogger.Debug("ServerManagementViewModel", "已订阅服务器状态变化事件");
        
        // 加载服务器列表
        _ = LoadServersAsync();
        DebugLogger.Debug("ServerManagementViewModel", "开始异步加载服务器列表");
    }

    /// <summary>
    /// 服务器状态变化处理
    /// </summary>
    private void OnServerStatusChanged(object? sender, ServerStatusChangedEventArgs e)
    {
        // 在UI线程更新服务器状态（使用BeginInvoke避免阻塞事件源）
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
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
        DebugLogger.Debug("LoadServersAsync", "IsLoading = true");
        
        try
        {
            DebugLogger.Debug("LoadServersAsync", "调用 ServerManager.GetServersAsync()");
            var servers = await _serverManager.GetServersAsync();
            DebugLogger.Debug("LoadServersAsync", $"获取到 {servers.Count} 个服务器");
            
            Servers.Clear();
            foreach (var server in servers)
            {
                Servers.Add(server);
                DebugLogger.Debug("LoadServersAsync", $"添加服务器: {server.Name} (ID: {server.Id})");
            }
            _logger.LogInformation("成功加载 {Count} 个服务器", Servers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载服务器列表失败");
            DebugLogger.Error("LoadServersAsync", $"加载失败: {ex.Message}", ex);
            ShowError($"加载服务器列表失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            DebugLogger.Debug("LoadServersAsync", "IsLoading = false");
        }
    }

    /// <summary>
    /// 添加服务器命令（快速添加现有服务器）
    /// </summary>
    [RelayCommand]
    private async Task AddServerAsync()
    {
        _logger.LogInformation("用户触发添加服务器操作");
        DebugLogger.Info("AddServerAsync", "开始添加服务器流程");
        
        try
        {
            // 显示添加服务器对话框
            var dialog = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dlg = new Views.Dialogs.AddServerDialog
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                return dlg;
            });

            var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                dialog.ShowDialog());

            if (result != true)
            {
                DebugLogger.Info("AddServerAsync", "用户取消添加服务器");
                return;
            }

            var name = dialog.ServerName;
            var installPath = dialog.InstallPath;
            var config = dialog.ServerConfig;
            
            DebugLogger.Debug("AddServerAsync", $"服务器配置: Name={name}, Port={config.Port}, Path={installPath}");
            
            _logger.LogInformation("正在添加服务器: {ServerName}, 路径: {InstallPath}, 端口: {Port}", 
                name, installPath, config.Port);
            
            var server = await _serverManager.AddServerAsync(name, installPath, config);
            
            // 手动添加的服务器，不由CSP2管理
            server.InstallSource = ServerInstallSource.Manual;
            server.IsManagedByCSP2 = false;
            await _serverManager.UpdateServerAsync(server);
            
            Servers.Add(server);
            
            _logger.LogInformation("成功添加服务器: ID={ServerId}, 名称={ServerName}", 
                server.Id, server.Name);
            DebugLogger.Info("AddServerAsync", $"服务器添加成功: {server.Id}");
            ShowSuccess($"已添加服务器: {name}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "服务器路径验证失败");
            DebugLogger.Error("AddServerAsync", "路径验证失败", ex);
            
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show(
                    $"添加服务器失败\n\n{ex.Message}\n\n" +
                    "提示：服务器路径必须包含有效的 CS2 服务器文件（game/bin/win64/cs2.exe）。\n" +
                    "你可以：\n" +
                    "1. 使用【安装服务器】功能自动下载服务器文件\n" +
                    "2. 选择已有的 CS2 游戏安装路径",
                    "路径验证失败",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加服务器失败");
            DebugLogger.Error("AddServerAsync", "添加服务器异常", ex);
            ShowError($"添加服务器失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private async Task StartServerAsync(Server? server)
    {
        if (server == null)
        {
            DebugLogger.Warning("StartServerAsync", "服务器参数为null");
            return;
        }

        try
        {
            _logger.LogInformation("启动服务器: {ServerName} (ID: {ServerId})", server.Name, server.Id);
            DebugLogger.Info("StartServerAsync", $"开始启动服务器: {server.Name}");
            await _serverManager.StartServerAsync(server.Id);
            DebugLogger.Info("StartServerAsync", $"服务器启动命令已发送: {server.Name}");
            ShowSuccess($"服务器 {server.Name} 正在启动...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动服务器失败: {ServerName}", server.Name);
            DebugLogger.Error("StartServerAsync", $"启动服务器失败: {server.Name}", ex);
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
    /// 删除服务器命令（仅删除配置）
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteServer))]
    private async Task DeleteServerAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("请求删除服务器: {ServerName} (ID: {ServerId})", server.Name, server.Id);
            
            // 根据服务器来源显示不同的警告
            string warningMessage;
            string subtitle;
            
            if (server.InstallSource == ServerInstallSource.ExistingSteam)
            {
                warningMessage = $"你确定要从CSP2中移除服务器 '{server.Name}' 吗？\n\n" +
                    $"路径: {server.InstallPath}\n\n" +
                    "⚠️ 警告：此服务器使用的是Steam安装的CS2文件！\n" +
                    "删除操作只会移除CSP2中的服务器配置，不会影响你的Steam游戏文件。\n\n" +
                    "如果你想完全删除服务器文件，请使用【卸载】功能（仅限通过CSP2安装的服务器）。";
                subtitle = "Steam游戏文件将被保留";
            }
            else if (server.InstallSource == ServerInstallSource.ExistingLocal)
            {
                warningMessage = $"你确定要从CSP2中移除服务器 '{server.Name}' 吗？\n\n" +
                    $"路径: {server.InstallPath}\n\n" +
                    "此操作只会从CSP2中删除服务器配置，不会删除服务器文件。\n" +
                    "服务器文件将保留在原位置。";
                subtitle = "服务器文件将被保留";
            }
            else if (server.InstallSource == ServerInstallSource.SteamCmd && server.IsManagedByCSP2)
            {
                warningMessage = $"你确定要从CSP2中移除服务器 '{server.Name}' 吗？\n\n" +
                    $"路径: {server.InstallPath}\n\n" +
                    "此操作只会从CSP2中删除服务器配置，不会删除服务器文件。\n\n" +
                    "💡 提示：如果你想同时删除服务器文件，请使用【卸载】按钮。";
                subtitle = "服务器文件将被保留";
            }
            else
            {
                warningMessage = $"你确定要删除服务器 '{server.Name}' 吗？\n\n" +
                    $"路径: {server.InstallPath}\n\n" +
                    "此操作只会从CSP2中删除服务器配置，不会删除服务器文件。";
                subtitle = "配置删除后无法恢复";
            }
            
            // 显示确认对话框
            var confirmed = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return Views.Dialogs.ConfirmDialog.Show(
                    System.Windows.Application.Current.MainWindow,
                    "确认删除服务器",
                    warningMessage,
                    subtitle,
                    "🗑️ 删除配置",
                    "🗑️",
                    true);
            });

            if (!confirmed)
            {
                DebugLogger.Info("DeleteServerAsync", "用户取消删除操作");
                return;
            }

            await _serverManager.DeleteServerAsync(server.Id);
            Servers.Remove(server);
            if (SelectedServer?.Id == server.Id)
            {
                SelectedServer = null;
            }
            
            _logger.LogInformation("成功删除服务器配置: {ServerName}", server.Name);
            ShowSuccess($"已从CSP2中移除服务器: {server.Name}");
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
    /// 卸载服务器命令（删除配置和文件）
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUninstallServer))]
    private async Task UninstallServerAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("请求卸载服务器: {ServerName} (ID: {ServerId})", server.Name, server.Id);
            
            // 检查服务器来源，防止卸载非CSP2管理的服务器
            if (server.InstallSource == ServerInstallSource.ExistingSteam)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"无法卸载服务器 '{server.Name}'！\n\n" +
                        "此服务器使用的是Steam安装的CS2文件，不应该被删除。\n\n" +
                        "如果你想从CSP2中移除此服务器，请使用【删除】功能。\n" +
                        "这只会删除CSP2中的配置，不会影响Steam游戏文件。",
                        "无法卸载",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                });
                return;
            }

            if (!server.IsManagedByCSP2 && server.InstallSource != ServerInstallSource.SteamCmd)
            {
                var allowUninstall = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    return System.Windows.MessageBox.Show(
                        $"警告：服务器 '{server.Name}' 不是通过CSP2安装的！\n\n" +
                        $"路径: {server.InstallPath}\n\n" +
                        "卸载操作将删除该路径下的所有文件！\n" +
                        "如果此路径包含其他重要数据，可能会造成数据丢失。\n\n" +
                        "你确定要继续吗？",
                        "危险操作",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);
                });

                if (allowUninstall != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }
            }
            
            // 显示最终确认对话框
            var confirmed = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return Views.Dialogs.ConfirmDialog.Show(
                    System.Windows.Application.Current.MainWindow,
                    "确认卸载服务器",
                    $"你确定要完全卸载服务器 '{server.Name}' 吗？\n\n" +
                    $"路径: {server.InstallPath}\n\n" +
                    "⚠️ 此操作将：\n" +
                    "• 删除服务器配置\n" +
                    "• 删除服务器文件（包括所有游戏数据、插件、配置等）\n\n" +
                    "此操作无法撤销！",
                    "所有数据将被永久删除",
                    "💣 完全卸载",
                    "💣",
                    true);
            });

            if (!confirmed)
            {
                DebugLogger.Info("UninstallServerAsync", "用户取消卸载操作");
                return;
            }

            await _serverManager.UninstallServerAsync(server.Id, deleteFiles: true);
            Servers.Remove(server);
            if (SelectedServer?.Id == server.Id)
            {
                SelectedServer = null;
            }
            
            _logger.LogInformation("成功卸载服务器: {ServerName}", server.Name);
            ShowSuccess($"已完全卸载服务器: {server.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载服务器失败: {ServerName}", server.Name);
            ShowError($"卸载服务器失败: {ex.Message}");
        }
    }

    private bool CanUninstallServer(Server? server)
    {
        // 只有停止状态且不是Steam安装的服务器才能卸载
        return server != null && 
               server.Status == ServerStatus.Stopped &&
               server.InstallSource != ServerInstallSource.ExistingSteam;
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
    /// 使用安装向导安装服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstallServer))]
    private async Task InstallServerAsync()
    {
        DebugLogger.Info("InstallServerAsync", "开始安装服务器流程");
        _logger.LogInformation("开始安装服务器流程");
        
        try
        {
            // 显示安装对话框
            var dialog = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dlg = new Views.Dialogs.ServerInstallDialog(_pathDetector, 
                    Microsoft.Extensions.Logging.LoggerFactory.Create(builder => {}).CreateLogger<Views.Dialogs.ServerInstallDialog>())
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                return dlg;
            });

            var dialogResult = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                dialog.ShowDialog());

            if (dialogResult != true || dialog.Result == null)
            {
                DebugLogger.Info("InstallServerAsync", "用户取消安装");
                return;
            }

            var result = dialog.Result;
            
            // 根据安装模式执行不同的操作
            if (result.Mode == Views.Dialogs.ServerInstallMode.SteamCmd)
            {
                await InstallServerWithSteamCmdAsync(result.ServerName, result.InstallPath, result.Config);
            }
            else
            {
                await AddExistingServerAsync(result.ServerName, result.InstallPath, result.Config, result.Mode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装服务器失败");
            DebugLogger.Error("InstallServerAsync", "安装服务器异常", ex);
            ShowError($"安装服务器失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 使用SteamCMD下载服务器
    /// </summary>
    private async Task InstallServerWithSteamCmdAsync(string serverName, string installPath, ServerConfig config)
    {
        DebugLogger.Info("InstallServerWithSteamCmdAsync", "开始使用SteamCMD安装服务器");
        
        // 检查 SteamCMD 是否已安装
        DebugLogger.Debug("InstallServerWithSteamCmdAsync", "检查 SteamCMD 是否已安装");
        if (!await _steamCmdService.IsSteamCmdInstalledAsync())
        {
            DebugLogger.Warning("InstallServerWithSteamCmdAsync", "SteamCMD 未安装，提示用户安装");
            
            var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                System.Windows.MessageBox.Show(
                    "SteamCMD 未安装。是否现在安装？\n\n" +
                    "SteamCMD 是下载和管理 CS2 服务器文件所必需的工具。",
                    "需要 SteamCMD",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question));

            if (result == System.Windows.MessageBoxResult.No)
            {
                DebugLogger.Info("InstallServerWithSteamCmdAsync", "用户取消安装 SteamCMD");
                ShowError("未安装SteamCMD，操作已取消。");
                return;
            }
            
            // 安装 SteamCMD
            _logger.LogInformation("开始安装 SteamCMD");
            ServerInstallProgress = 0;
            ServerInstallMessage = "正在安装 SteamCMD...";

            var downloadProgress = new Progress<DownloadProgress>(p =>
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    ServerInstallProgress = p.Percentage * 0.3;
                    ServerInstallMessage = p.Message;
                });
            });

            var steamCmdPath = _steamCmdService.GetSteamCmdPath();
            
            if (!await _steamCmdService.InstallSteamCmdAsync(steamCmdPath, downloadProgress))
            {
                DebugLogger.Error("InstallServerWithSteamCmdAsync", "SteamCMD 安装失败");
                ShowError("SteamCMD 安装失败，请查看日志了解详情。");
                return;
            }
        }

        // 下载CS2服务器文件
        IsInstallingServer = true;
        
        _logger.LogInformation("开始下载 CS2 服务器文件到: {Path}", installPath);
        ServerInstallProgress = 30;
        ServerInstallMessage = "正在下载 CS2 服务器文件（约30GB）...";

        var installProgress = new Progress<InstallProgress>(p =>
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                ServerInstallProgress = 30 + (p.Percentage * 0.7);
                ServerInstallMessage = p.Message;
            });
        });

        try
        {
            var success = await _steamCmdService.InstallOrUpdateServerAsync(installPath, false, installProgress);

            if (success)
            {
                _logger.LogInformation("CS2 服务器下载成功，正在添加到列表");
                ServerInstallMessage = "正在添加服务器到列表...";
                
                var server = await _serverManager.AddServerAsync(serverName, installPath, config);
                
                // 标记为通过SteamCMD安装且由CSP2管理
                server.InstallSource = ServerInstallSource.SteamCmd;
                server.IsManagedByCSP2 = true;
                await _serverManager.UpdateServerAsync(server);
                
                Servers.Add(server);

                ServerInstallProgress = 100;
                ServerInstallMessage = "✅ 服务器安装完成！";
                ShowSuccess($"服务器 '{serverName}' 安装成功！");

                await Task.Delay(3000);
            }
            else
            {
                DebugLogger.Error("InstallServerWithSteamCmdAsync", "CS2 服务器下载失败");
                ShowError("CS2 服务器下载失败，请查看日志了解详情。");
            }
        }
        finally
        {
            IsInstallingServer = false;
        }
    }

    /// <summary>
    /// 添加现有的CS2安装作为服务器
    /// </summary>
    private async Task AddExistingServerAsync(string serverName, string existingPath, ServerConfig config, Views.Dialogs.ServerInstallMode mode)
    {
        DebugLogger.Info("AddExistingServerAsync", $"添加现有安装: {existingPath}");
        _logger.LogInformation("使用现有CS2安装: {Path}", existingPath);

        IsInstallingServer = true;
        ServerInstallProgress = 50;
        ServerInstallMessage = "正在验证CS2安装...";

        try
        {
            ServerInstallProgress = 80;
            ServerInstallMessage = "正在添加服务器到列表...";

            var server = await _serverManager.AddServerAsync(serverName, existingPath, config);
            
            // 根据模式设置安装来源
            if (mode == Views.Dialogs.ServerInstallMode.ExistingSteam)
            {
                server.InstallSource = ServerInstallSource.ExistingSteam;
                server.IsManagedByCSP2 = false;  // Steam安装不由CSP2管理
            }
            else
            {
                server.InstallSource = ServerInstallSource.ExistingLocal;
                server.IsManagedByCSP2 = false;  // 现有本地安装不由CSP2管理
            }
            
            await _serverManager.UpdateServerAsync(server);
            Servers.Add(server);

            ServerInstallProgress = 100;
            ServerInstallMessage = "✅ 服务器添加完成！";
            DebugLogger.Info("AddExistingServerAsync", $"服务器添加成功: {server.Id}");
            ShowSuccess($"服务器 '{serverName}' 添加成功！");

            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加现有服务器失败");
            DebugLogger.Error("AddExistingServerAsync", "添加失败", ex);
            ShowError($"添加服务器失败：{ex.Message}");
        }
        finally
        {
            IsInstallingServer = false;
        }
    }

    private bool CanInstallServer() => !IsInstallingServer && !IsLoading;

    /// <summary>
    /// 查看服务器日志命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanViewServerLog))]
    private async Task ViewServerLogAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("查看服务器日志: {ServerName}", server.Name);
            
            var logFiles = await _serverManager.GetServerLogFilesAsync(server.Id);
            
            if (logFiles.Count == 0)
            {
                ShowError("未找到日志文件。\n\n服务器可能尚未运行过，或日志功能未启用。");
                return;
            }

            // 读取最新的日志文件
            var latestLogFile = logFiles.First();
            var logFileName = Path.GetFileName(latestLogFile);
            var logContent = await _serverManager.ReadLogFileAsync(latestLogFile, 500);

            // 显示日志内容
            var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var window = new System.Windows.Window
                {
                    Title = $"服务器日志 - {server.Name} ({logFileName})",
                    Width = 900,
                    Height = 600,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                };

                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = logContent,
                    IsReadOnly = true,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    Padding = new System.Windows.Thickness(10),
                    TextWrapping = System.Windows.TextWrapping.NoWrap
                };

                window.Content = textBox;
                window.ShowDialog();
                return true;
            });

            _logger.LogInformation("日志查看完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查看服务器日志失败: {ServerName}", server.Name);
            ShowError($"查看日志失败: {ex.Message}");
        }
    }

    private bool CanViewServerLog(Server? server)
    {
        return server != null;
    }

    /// <summary>
    /// 编辑服务器设置命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditServerSettings))]
    private async Task EditServerSettingsAsync(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("编辑服务器设置: {ServerName}", server.Name);
            
            // TODO: 实现完整的设置对话框
            // 暂时使用简单的消息框显示当前配置
            var configInfo = $"""
                服务器配置
                
                名称: {server.Name}
                路径: {server.InstallPath}
                
                网络设置:
                - IP地址: {server.Config.IpAddress}
                - 端口: {server.Config.Port}
                
                游戏设置:
                - 地图: {server.Config.Map}
                - 最大玩家: {server.Config.MaxPlayers}
                - Tick Rate: {server.Config.TickRate}
                - 游戏类型: {server.Config.GameType}
                - 游戏模式: {server.Config.GameMode}
                
                高级设置:
                - 服务器名称: {server.Config.ServerName ?? "未设置"}
                - RCON密码: {(string.IsNullOrEmpty(server.Config.RconPassword) ? "未设置" : "已设置")}
                - Steam令牌: {(string.IsNullOrEmpty(server.Config.SteamToken) ? "未设置" : "已设置")}
                - 控制台: {(server.Config.EnableConsole ? "启用" : "禁用")}
                - 日志: {(server.Config.EnableLogging ? "启用" : "禁用")}
                - 进程优先级: {server.Config.ProcessPriority}
                
                提示: 完整的设置编辑器正在开发中...
                """;

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                System.Windows.MessageBox.Show(
                    configInfo,
                    $"服务器设置 - {server.Name}",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            });

            _logger.LogInformation("设置查看完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑服务器设置失败: {ServerName}", server.Name);
            ShowError($"编辑设置失败: {ex.Message}");
        }
    }

    private bool CanEditServerSettings(Server? server)
    {
        return server != null && server.Status == ServerStatus.Stopped;
    }

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

