using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Logging;
using CSP2.Core.Models;
using CSP2.Core.Services;
using CSP2.Desktop.Services;
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
    private readonly JsonLocalizationService _localizationService;

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

    public ServerManagementViewModel(
        IServerManager serverManager,
        ISteamCmdService steamCmdService,
        CS2PathDetector pathDetector,
        ILogger<ServerManagementViewModel> logger,
        JsonLocalizationService localizationService)
    {
        _serverManager = serverManager;
        _steamCmdService = steamCmdService;
        _pathDetector = pathDetector;
        _logger = logger;
        _localizationService = localizationService;
        
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
            
            // 通知命令更新（解决按钮初始无法点击的问题）
            InstallServerCommand.NotifyCanExecuteChanged();
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
                var dlg = new Views.Dialogs.AddServerDialog(_pathDetector)
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
            
            DebugLogger.Debug("AddServerAsync", $"【DEBUG】服务器配置: Name={name}, Port={config.Port}, Path={installPath}");
            _logger.LogDebug("【DEBUG】ViewModel: 准备添加服务器");
            
            _logger.LogInformation("【DEBUG】正在添加服务器: {ServerName}, 路径: {InstallPath}, 端口: {Port}", 
                name, installPath, config.Port);
            
            DebugLogger.Info("AddServerAsync", "【DEBUG】调用 ServerManager.AddServerAsync");
            var server = await _serverManager.AddServerAsync(name, installPath, config);
            DebugLogger.Info("AddServerAsync", $"【DEBUG】AddServerAsync 返回，Server ID: {server.Id}");
            
            // 手动添加的服务器，不由CSP2管理
            _logger.LogDebug("【DEBUG】设置服务器来源为 Manual");
            server.InstallSource = ServerInstallSource.Manual;
            server.IsManagedByCSP2 = false;
            
            DebugLogger.Info("AddServerAsync", "【DEBUG】调用 UpdateServerAsync 更新服务器信息");
            var updateResult = await _serverManager.UpdateServerAsync(server);
            DebugLogger.Info("AddServerAsync", $"【DEBUG】UpdateServerAsync 返回: {updateResult}");
            
            _logger.LogDebug("【DEBUG】添加到 UI 的 Servers 集合，当前大小: {Count}", Servers.Count);
            Servers.Add(server);
            _logger.LogDebug("【DEBUG】添加后 Servers 集合大小: {Count}", Servers.Count);
            
            _logger.LogInformation("【DEBUG】成功添加服务器: ID={ServerId}, 名称={ServerName}", 
                server.Id, server.Name);
            DebugLogger.Info("AddServerAsync", $"【DEBUG】服务器添加成功: {server.Id}");
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
        // 允许在停止或崩溃状态下启动服务器
        return server != null && (server.Status == ServerStatus.Stopped || server.Status == ServerStatus.Crashed);
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
        // 允许删除除了Running状态之外的所有服务器
        return server != null && server.Status != ServerStatus.Running;
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
        // 允许卸载除了Running状态之外的所有服务器，但不能卸载Steam安装的
        return server != null && 
               server.Status != ServerStatus.Running &&
               server.InstallSource != ServerInstallSource.ExistingSteam;
    }

    /// <summary>
    /// 刷新列表命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        DebugLogger.Info("RefreshAsync", "开始刷新服务器列表和状态");
        
        // 先检查并更新所有服务器状态
        try
        {
            await _serverManager.CheckAndUpdateServerStatusesAsync();
            DebugLogger.Info("RefreshAsync", "服务器状态检查完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查服务器状态失败");
            DebugLogger.Error("RefreshAsync", "状态检查失败", ex);
        }
        
        // 然后重新加载服务器列表
        await LoadServersAsync();
    }
    
    /// <summary>
    /// 刷新选中服务器状态命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedServer))]
    private async Task RefreshServerStatusAsync()
    {
        if (SelectedServer == null) return;
        
        DebugLogger.Info("RefreshServerStatus", $"刷新服务器状态: {SelectedServer.Name}");
        
        try
        {
            var newStatus = await _serverManager.RefreshServerStatusAsync(SelectedServer.Id);
            _logger.LogInformation("服务器状态已刷新: {Name} -> {Status}", SelectedServer.Name, newStatus);
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"状态已更新: {newStatus}";
                HasError = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新服务器状态失败: {Name}", SelectedServer.Name);
            DebugLogger.Error("RefreshServerStatus", "刷新失败", ex);
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"刷新状态失败: {ex.Message}";
                HasError = true;
            });
        }
    }
    
    private bool HasSelectedServer() => SelectedServer != null;

    /// <summary>
    /// 使用安装向导安装服务器命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstallServer))]
    private void InstallServer()
    {
        DebugLogger.Info("InstallServer", "导航到安装页面");
        _logger.LogInformation("开始安装服务器流程");
        
        try
        {
            // 获取MainWindow的ViewModel并导航到安装页面
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.NavigateToServerInstall(
                    onComplete: server =>
                    {
                        // 安装完成后添加到列表
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            Servers.Add(server);
                            ShowSuccess($"服务器 '{server.Name}' 安装成功！");
                        });
                    },
                    onCancel: () =>
                    {
                        DebugLogger.Info("InstallServer", "用户取消安装");
                    });
            }
            else
            {
                _logger.LogError("无法获取MainWindowViewModel");
                ShowError("无法打开安装页面");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开安装页面失败");
            DebugLogger.Error("InstallServer", "打开安装页面异常", ex);
            ShowError($"打开安装页面失败：{ex.Message}");
        }
    }

    private bool CanInstallServer() => !IsLoading;

    /// <summary>
    /// 查看服务器日志命令 - 导航到日志控制台页面
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanViewServerLog))]
    private void ViewServerLog(Server? server)
    {
        if (server == null) return;

        try
        {
            _logger.LogInformation("导航到服务器日志控制台: {ServerName}", server.Name);
            DebugLogger.Info("ViewServerLog", $"导航到服务器 {server.Name} 的日志控制台");
            
            // 获取MainWindow的ViewModel并导航到日志页面
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
            {
                mainViewModel.NavigateToLogConsole(server.Id);
            }
            else
            {
                _logger.LogError("无法获取MainWindowViewModel");
                ShowError("无法打开日志控制台");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到日志控制台失败: {ServerName}", server.Name);
            DebugLogger.Error("ViewServerLog", "导航失败", ex);
            ShowError($"打开日志控制台失败：{ex.Message}");
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
            DebugLogger.Info("EditServerSettingsAsync", $"打开配置对话框: {server.Name}");

            bool? result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 打开简化的服务器配置对话框
                var dialog = new Views.Dialogs.SimpleServerConfigDialog(server.Config)
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                    Title = $"配置服务器 - {server.Name}"
                };
                
                // 应用配置结果
                if (dialog.ShowDialog() == true)
                {
                    server.Config = dialog.ServerConfig;
                }
                
                return dialog.DialogResult;
            });

            // 用户点击确定后保存配置
            if (result == true)
            {
                _logger.LogInformation("用户确认保存配置: {ServerName}", server.Name);
                DebugLogger.Info("EditServerSettingsAsync", "用户确认保存配置，更新服务器");
                
                // 更新服务器配置
                await _serverManager.UpdateServerAsync(server);
                
                ShowSuccess($"服务器 {server.Name} 配置已保存");
                _logger.LogInformation("服务器配置已保存: {ServerName}", server.Name);
                DebugLogger.Info("EditServerSettingsAsync", $"配置保存成功: {server.Name}");
            }
            else
            {
                _logger.LogInformation("用户取消配置编辑");
                DebugLogger.Debug("EditServerSettingsAsync", "用户取消编辑");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑服务器设置失败: {ServerName}", server.Name);
            DebugLogger.Error("EditServerSettingsAsync", $"编辑失败: {ex.Message}", ex);
            ShowError($"编辑设置失败: {ex.Message}");
        }
    }

    private bool CanEditServerSettings(Server? server)
    {
        // 允许在停止或崩溃状态下编辑设置
        return server != null && (server.Status == ServerStatus.Stopped || server.Status == ServerStatus.Crashed);
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

