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
    /// 添加服务器命令
    /// </summary>
    [RelayCommand]
    private async Task AddServerAsync()
    {
        // TODO: 显示添加服务器对话框
        // 暂时添加一个测试服务器
        _logger.LogInformation("用户触发添加服务器操作");
        DebugLogger.Info("AddServerAsync", "开始添加服务器流程");
        
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
            var installPath = $@"C:\CS2Server\Server{Servers.Count + 1}";
            
            DebugLogger.Debug("AddServerAsync", $"服务器配置: Name={name}, Port={config.Port}, Path={installPath}");
            
            _logger.LogInformation("正在添加服务器: {ServerName}, 路径: {InstallPath}, 端口: {Port}", 
                name, installPath, config.Port);
            
            var server = await _serverManager.AddServerAsync(name, installPath, config);
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
            ShowError($"添加服务器失败\n\n{ex.Message}\n\n" +
                "提示：请参考文档 docs/04-CS2服务器配置指南.md 了解如何正确安装CS2服务器。");
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
        DebugLogger.Info("InstallServerAsync", "开始安装服务器流程");
        _logger.LogInformation("开始安装服务器流程");
        
        try
        {
            IsInstallingServer = true;
            ServerInstallProgress = 0;
            ServerInstallMessage = "正在检测现有CS2安装...";

            // 第一步：检测现有的CS2安装
            DebugLogger.Info("InstallServerAsync", "开始检测现有CS2安装");
            var detectedInstallations = await _pathDetector.DetectAllInstallationsAsync();
            var validInstallations = detectedInstallations.Where(i => i.IsValid).ToList();

            DebugLogger.Info("InstallServerAsync", $"检测到 {validInstallations.Count} 个有效的CS2安装");
            _logger.LogInformation("检测到 {Count} 个有效的CS2安装", validInstallations.Count);

            string? selectedPath = null;
            bool useSteamCmd = false;

            // 第二步：询问用户选择
            if (validInstallations.Count > 0)
            {
                DebugLogger.Info("InstallServerAsync", "有可用的CS2安装，询问用户选择");
                
                // 构建选择消息
                var messageBuilder = new System.Text.StringBuilder();
                messageBuilder.AppendLine("检测到以下CS2安装：\n");
                
                for (int i = 0; i < validInstallations.Count; i++)
                {
                    var install = validInstallations[i];
                    messageBuilder.AppendLine($"[{i + 1}] {install.Source}");
                    messageBuilder.AppendLine($"    路径: {install.InstallPath}");
                    if (install.InstallSize.HasValue)
                    {
                        messageBuilder.AppendLine($"    大小: {CS2PathDetector.FormatFileSize(install.InstallSize.Value)}");
                    }
                    messageBuilder.AppendLine();
                }

                messageBuilder.AppendLine("您想要：");
                messageBuilder.AppendLine("• 点击【是】- 使用检测到的第一个安装");
                messageBuilder.AppendLine("• 点击【否】- 使用SteamCMD下载新的服务器文件");

                var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    System.Windows.MessageBox.Show(
                        messageBuilder.ToString(),
                        "发现现有CS2安装",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question));

                if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    DebugLogger.Info("InstallServerAsync", "用户取消操作");
                    return;
                }
                else if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // 使用检测到的安装
                    selectedPath = validInstallations[0].InstallPath;
                    DebugLogger.Info("InstallServerAsync", $"用户选择使用现有安装: {selectedPath}");
                    _logger.LogInformation("用户选择使用现有CS2安装: {Path}", selectedPath);
                }
                else
                {
                    // 使用SteamCMD
                    useSteamCmd = true;
                    DebugLogger.Info("InstallServerAsync", "用户选择使用SteamCMD下载");
                    _logger.LogInformation("用户选择使用SteamCMD下载新的服务器文件");
                }
            }
            else
            {
                // 没有检测到安装，询问是否使用SteamCMD
                DebugLogger.Warning("InstallServerAsync", "未检测到CS2安装");
                _logger.LogWarning("未检测到现有的CS2安装");
                
                var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    System.Windows.MessageBox.Show(
                        "未检测到现有的CS2安装。\n\n" +
                        "是否使用SteamCMD下载CS2服务器文件？\n\n" +
                        "注意：服务器文件大约30GB，下载可能需要较长时间。",
                        "未找到CS2安装",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question));

                if (result == System.Windows.MessageBoxResult.No)
                {
                    DebugLogger.Info("InstallServerAsync", "用户取消操作");
                    ShowError("未选择安装源，操作已取消。");
                    return;
                }
                
                useSteamCmd = true;
            }

            // 第三步：执行安装
            var serverName = $"CS2 服务器 {Servers.Count + 1}";

            if (useSteamCmd)
            {
                // 使用SteamCMD下载
                await InstallServerWithSteamCmdAsync(serverName);
            }
            else if (!string.IsNullOrEmpty(selectedPath))
            {
                // 使用现有安装
                await AddExistingServerAsync(serverName, selectedPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装服务器失败");
            DebugLogger.Error("InstallServerAsync", "安装服务器异常", ex);
            ShowError($"安装服务器失败：{ex.Message}");
        }
        finally
        {
            IsInstallingServer = false;
            DebugLogger.Debug("InstallServerAsync", "安装流程结束");
        }
    }

    /// <summary>
    /// 使用SteamCMD下载服务器
    /// </summary>
    private async Task InstallServerWithSteamCmdAsync(string serverName)
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
        var serverPath = $@"C:\CS2Servers\Server{Servers.Count + 1}";
        
        _logger.LogInformation("开始下载 CS2 服务器文件到: {Path}", serverPath);
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

        var success = await _steamCmdService.InstallOrUpdateServerAsync(serverPath, false, installProgress);

        if (success)
        {
            _logger.LogInformation("CS2 服务器下载成功，正在添加到列表");
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

            await Task.Delay(3000);
        }
        else
        {
            DebugLogger.Error("InstallServerWithSteamCmdAsync", "CS2 服务器下载失败");
            ShowError("CS2 服务器下载失败，请查看日志了解详情。");
        }
    }

    /// <summary>
    /// 添加现有的CS2安装作为服务器
    /// </summary>
    private async Task AddExistingServerAsync(string serverName, string existingPath)
    {
        DebugLogger.Info("AddExistingServerAsync", $"添加现有安装: {existingPath}");
        _logger.LogInformation("使用现有CS2安装: {Path}", existingPath);

        ServerInstallProgress = 50;
        ServerInstallMessage = "正在验证CS2安装...";

        try
        {
            var config = new ServerConfig
            {
                Port = 27015 + Servers.Count,
                Map = "de_dust2",
                MaxPlayers = 10,
                TickRate = 128,
                ServerName = serverName
            };

            ServerInstallProgress = 80;
            ServerInstallMessage = "正在添加服务器到列表...";

            var server = await _serverManager.AddServerAsync(serverName, existingPath, config);
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

