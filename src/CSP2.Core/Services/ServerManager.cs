using System.Diagnostics;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using CSP2.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// 服务器管理器实现
/// </summary>
public class ServerManager : IServerManager
{
    private readonly IConfigurationService _configService;
    private readonly IPlatformProvider _platformProvider;
    private readonly ILogger<ServerManager> _logger;
    private readonly Dictionary<string, ServerProcess> _runningServers = new();
    private List<Server> _servers = new();
    private readonly System.Threading.Timer _statusCheckTimer;
    private readonly object _statusLock = new();

    public event EventHandler<LogReceivedEventArgs>? LogReceived;
    public event EventHandler<ServerStatusChangedEventArgs>? StatusChanged;

    public ServerManager(
        IConfigurationService configService,
        ProviderRegistry providerRegistry,
        ILogger<ServerManager> logger)
    {
        _configService = configService;
        _platformProvider = providerRegistry.GetBestPlatformProvider();
        _logger = logger;
        
        // 启动后台状态检查任务，每5秒检查一次
        _statusCheckTimer = new System.Threading.Timer(
            CheckServerStatusesCallback, 
            null, 
            TimeSpan.FromSeconds(5), 
            TimeSpan.FromSeconds(5));
        
        _logger.LogDebug("ServerManager initialized with status monitoring");
    }
    
    /// <summary>
    /// 后台定期检查所有服务器状态
    /// </summary>
    private void CheckServerStatusesCallback(object? state)
    {
        try
        {
            _ = CheckAndUpdateServerStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状态检查任务异常");
        }
    }
    
    /// <summary>
    /// 检查并更新所有服务器状态
    /// </summary>
    public async Task CheckAndUpdateServerStatusesAsync()
    {
        lock (_statusLock)
        {
            var serversToUpdate = new List<(string serverId, ServerStatus newStatus)>();
            
            // 检查所有在运行列表中的服务器
            foreach (var kvp in _runningServers.ToList())
            {
                var serverId = kvp.Key;
                var serverProcess = kvp.Value;
                
                try
                {
                    // 检查进程是否已退出
                    if (serverProcess.Process.HasExited)
                    {
                        _logger.LogWarning("检测到服务器进程已退出: {Name} (ExitCode: {ExitCode})", 
                            serverProcess.Server.Name, serverProcess.Process.ExitCode);
                        
                        // 根据退出代码判断状态
                        var newStatus = serverProcess.Process.ExitCode == 0 
                            ? ServerStatus.Stopped 
                            : ServerStatus.Crashed;
                        
                        serversToUpdate.Add((serverId, newStatus));
                        _runningServers.Remove(serverId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检查服务器状态失败: {Id}", serverId);
                    serversToUpdate.Add((serverId, ServerStatus.Crashed));
                    _runningServers.Remove(serverId);
                }
            }
            
            // 检查所有标记为Starting但超时的服务器
            foreach (var server in _servers.Where(s => s.Status == ServerStatus.Starting))
            {
                // 如果服务器在Starting状态超过30秒，但不在运行列表中，设为Crashed
                if (!_runningServers.ContainsKey(server.Id))
                {
                    if (server.LastStartedAt.HasValue && 
                        DateTime.Now - server.LastStartedAt.Value > TimeSpan.FromSeconds(30))
                    {
                        _logger.LogWarning("服务器启动超时: {Name}", server.Name);
                        serversToUpdate.Add((server.Id, ServerStatus.Crashed));
                    }
                }
            }
            
            // 更新状态
            foreach (var (serverId, newStatus) in serversToUpdate)
            {
                var server = _servers.FirstOrDefault(s => s.Id == serverId);
                if (server != null)
                {
                    ChangeServerStatus(server, newStatus);
                }
            }
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 手动刷新指定服务器的状态
    /// </summary>
    public async Task<ServerStatus> RefreshServerStatusAsync(string serverId)
    {
        var server = await GetServerByIdAsync(serverId);
        if (server == null)
        {
            throw new InvalidOperationException($"Server not found: {serverId}");
        }
        
        lock (_statusLock)
        {
            // 如果在运行列表中
            if (_runningServers.TryGetValue(serverId, out var serverProcess))
            {
                try
                {
                    if (serverProcess.Process.HasExited)
                    {
                        // 进程已退出
                        _runningServers.Remove(serverId);
                        var newStatus = serverProcess.Process.ExitCode == 0 
                            ? ServerStatus.Stopped 
                            : ServerStatus.Crashed;
                        ChangeServerStatus(server, newStatus);
                        _logger.LogInformation("刷新状态: {Name} -> {Status}", server.Name, newStatus);
                    }
                    else
                    {
                        // 进程仍在运行，确保状态正确
                        if (server.Status != ServerStatus.Running)
                        {
                            ChangeServerStatus(server, ServerStatus.Running);
                            _logger.LogInformation("修正状态: {Name} -> Running", server.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "刷新服务器状态失败: {Id}", serverId);
                    _runningServers.Remove(serverId);
                    ChangeServerStatus(server, ServerStatus.Crashed);
                }
            }
            else
            {
                // 不在运行列表中，应该是Stopped
                if (server.Status != ServerStatus.Stopped && server.Status != ServerStatus.Crashed)
                {
                    ChangeServerStatus(server, ServerStatus.Stopped);
                    _logger.LogInformation("修正状态: {Name} -> Stopped", server.Name);
                }
            }
        }
        
        return server.Status;
    }
    
    /// <summary>
    /// 应用启动时恢复服务器状态
    /// </summary>
    public async Task RestoreServerStatesAsync()
    {
        _logger.LogInformation("【DEBUG】开始恢复服务器状态...");
        _logger.LogDebug("【DEBUG】恢复前 _servers 缓存大小: {Count}", _servers.Count);
        
        // 🔧 修复：先从配置文件加载服务器列表
        if (_servers.Count == 0)
        {
            _logger.LogDebug("【DEBUG】缓存为空，先加载服务器列表");
            _servers = await _configService.LoadServersAsync();
            _logger.LogDebug("【DEBUG】加载完成，共 {Count} 个服务器", _servers.Count);
        }
        
        // 如果没有服务器，直接返回，不要保存空列表
        if (_servers.Count == 0)
        {
            _logger.LogInformation("【DEBUG】没有服务器需要恢复状态");
            return;
        }
        
        bool hasChanges = false;
        foreach (var server in _servers)
        {
            // 将所有非Stopped状态的服务器重置为Stopped
            // 因为应用重启后，之前的进程引用已失效
            if (server.Status != ServerStatus.Stopped && server.Status != ServerStatus.Crashed)
            {
                _logger.LogWarning("【DEBUG】恢复服务器状态: {Name} {OldStatus} -> Stopped", 
                    server.Name, server.Status);
                ChangeServerStatus(server, ServerStatus.Stopped);
                hasChanges = true;
            }
        }
        
        // 只有在状态有变化时才保存
        if (hasChanges)
        {
            _logger.LogDebug("【DEBUG】检测到状态变化，保存配置");
            await _configService.SaveServersAsync(_servers);
        }
        else
        {
            _logger.LogDebug("【DEBUG】所有服务器状态正常，无需保存");
        }
        
        _logger.LogInformation("【DEBUG】服务器状态恢复完成，共 {Count} 个服务器", _servers.Count);
    }

    public async Task<List<Server>> GetServersAsync()
    {
        _logger.LogDebug("【DEBUG】GetServersAsync: 当前 _servers 缓存大小: {Count}", _servers.Count);
        
        if (_servers.Count == 0)
        {
            _logger.LogDebug("【DEBUG】缓存为空，从配置服务加载服务器列表");
            _servers = await _configService.LoadServersAsync();
            _logger.LogDebug("【DEBUG】加载完成，_servers 大小: {Count}", _servers.Count);
        }
        else
        {
            _logger.LogDebug("【DEBUG】使用缓存的服务器列表");
        }
        
        return _servers;
    }

    public async Task<Server?> GetServerByIdAsync(string serverId)
    {
        var servers = await GetServersAsync();
        return servers.FirstOrDefault(s => s.Id == serverId);
    }

    public async Task<Server> AddServerAsync(string name, string installPath, ServerConfig? config = null)
    {
        _logger.LogDebug("【DEBUG】开始添加服务器: Name={Name}, Path={Path}", name, installPath);
        
        // 验证服务器路径
        if (!await ValidateServerInstallationAsync(installPath))
        {
            _logger.LogError("【DEBUG】服务器路径验证失败: {Path}", installPath);
            throw new InvalidOperationException($"Invalid server path or CS2 server files not found: {installPath}\n" +
                "Please ensure the path contains valid CS2 server files, or use InstallServerAsync to install the server.");
        }

        var server = new Server
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            InstallPath = installPath,
            Config = config ?? new ServerConfig(),
            Status = ServerStatus.Stopped,
            CreatedAt = DateTime.Now
        };

        _logger.LogDebug("【DEBUG】生成服务器ID: {Id}", server.Id);
        _logger.LogDebug("【DEBUG】添加前 _servers 列表大小: {Count}", _servers.Count);
        
        _servers.Add(server);
        
        _logger.LogDebug("【DEBUG】添加后 _servers 列表大小: {Count}", _servers.Count);
        _logger.LogDebug("【DEBUG】准备保存服务器配置...");
        
        var saveResult = await _configService.SaveServersAsync(_servers);
        
        _logger.LogDebug("【DEBUG】保存结果: {Result}", saveResult ? "成功" : "失败");
        _logger.LogDebug("【DEBUG】服务器配置已保存");

        _logger.LogInformation("【DEBUG】已添加服务器: {Name} ({Id})", name, server.Id);
        return server;
    }

    public async Task<Server> AddServerWithoutValidationAsync(string name, string installPath, ServerConfig? config = null)
    {
        _logger.LogDebug("开始添加服务器(跳过验证): Name={Name}, Path={Path}", name, installPath);

        var server = new Server
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            InstallPath = installPath,
            Config = config ?? new ServerConfig(),
            Status = ServerStatus.Stopped,
            CreatedAt = DateTime.Now
        };

        _logger.LogDebug("生成服务器ID: {Id}", server.Id);
        _servers.Add(server);
        await _configService.SaveServersAsync(_servers);
        _logger.LogDebug("服务器配置已保存");

        _logger.LogInformation("已添加服务器(跳过验证): {Name} ({Id})", name, server.Id);
        return server;
    }

    /// <summary>
    /// 验证服务器安装是否有效
    /// </summary>
    private async Task<bool> ValidateServerInstallationAsync(string installPath)
    {
        if (!Directory.Exists(installPath))
        {
            _logger.LogWarning("服务器路径不存在: {Path}", installPath);
            return false;
        }

        // 检查关键文件是否存在
        var executablePath = Path.Combine(installPath, "game", "bin", "win64", "cs2.exe");
        if (!File.Exists(executablePath))
        {
            _logger.LogWarning("找不到CS2可执行文件: {Path}", executablePath);
            return false;
        }

        // 检查game目录结构
        var gameDir = Path.Combine(installPath, "game");
        if (!Directory.Exists(gameDir))
        {
            _logger.LogWarning("找不到game目录: {Path}", gameDir);
            return false;
        }

        _logger.LogInformation("服务器安装验证通过: {Path}", installPath);
        return await Task.FromResult(true);
    }

    public async Task<bool> UpdateServerAsync(Server server)
    {
        _logger.LogDebug("【DEBUG】UpdateServerAsync: 更新服务器 {Id}", server.Id);
        
        var index = _servers.FindIndex(s => s.Id == server.Id);
        if (index == -1)
        {
            _logger.LogWarning("【DEBUG】服务器不在列表中: {Id}", server.Id);
            return false;
        }

        _logger.LogDebug("【DEBUG】找到服务器，索引: {Index}", index);
        _servers[index] = server;
        
        _logger.LogDebug("【DEBUG】准备保存更新后的服务器列表，共 {Count} 个", _servers.Count);
        var saveResult = await _configService.SaveServersAsync(_servers);
        _logger.LogDebug("【DEBUG】保存结果: {Result}", saveResult ? "成功" : "失败");

        _logger.LogInformation("【DEBUG】已更新服务器: {Name} ({Id})", server.Name, server.Id);
        return true;
    }

    public async Task<bool> DeleteServerAsync(string serverId)
    {
        // 如果服务器正在运行，先停止它
        if (_runningServers.ContainsKey(serverId))
        {
            await StopServerAsync(serverId, force: true);
        }

        var removed = _servers.RemoveAll(s => s.Id == serverId) > 0;
        if (removed)
        {
            await _configService.SaveServersAsync(_servers);
            _logger.LogInformation("已删除服务器配置: {Id}", serverId);
        }

        return removed;
    }

    public async Task<bool> UninstallServerAsync(string serverId, bool deleteFiles = true)
    {
        var server = await GetServerByIdAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("服务器不存在: {Id}", serverId);
            return false;
        }

        // 如果服务器正在运行，先停止它
        if (_runningServers.ContainsKey(serverId))
        {
            await StopServerAsync(serverId, force: true);
            // 等待进程完全停止
            await Task.Delay(2000);
        }

        // 删除服务器文件
        if (deleteFiles && Directory.Exists(server.InstallPath))
        {
            try
            {
                _logger.LogInformation("正在删除服务器文件: {Path}", server.InstallPath);
                Directory.Delete(server.InstallPath, true);
                _logger.LogInformation("服务器文件已删除: {Path}", server.InstallPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除服务器文件失败: {Path}", server.InstallPath);
                throw new InvalidOperationException($"Failed to delete server files: {ex.Message}", ex);
            }
        }

        // 删除配置
        var removed = _servers.RemoveAll(s => s.Id == serverId) > 0;
        if (removed)
        {
            await _configService.SaveServersAsync(_servers);
            _logger.LogInformation("已卸载服务器: {Id}", serverId);
        }

        return removed;
    }

    public async Task<bool> StartServerAsync(string serverId)
    {
        _logger.LogDebug("开始启动服务器: {ServerId}", serverId);
        
        var server = await GetServerByIdAsync(serverId);
        if (server == null)
        {
            _logger.LogWarning("服务器不存在: {Id}", serverId);
            return false;
        }

        if (_runningServers.ContainsKey(serverId))
        {
            _logger.LogWarning("服务器已在运行: {Name}", server.Name);
            return false;
        }

        try
        {
            ChangeServerStatus(server, ServerStatus.Starting);
            _logger.LogDebug("服务器状态: Starting");

            // 构建启动参数
            var executablePath = Path.Combine(server.InstallPath, "game", "bin", "win64", "cs2.exe");
            _logger.LogDebug("CS2可执行文件路径: {Path}", executablePath);
            
            if (!File.Exists(executablePath))
            {
                _logger.LogError("找不到CS2可执行文件: {Path}", executablePath);
                ChangeServerStatus(server, ServerStatus.Stopped);
                return false;
            }

            var arguments = LaunchArgumentsBuilder.BuildStartupArguments(server.Config);
            _logger.LogDebug("启动参数: {Args}", arguments);
            
            var workingDirectory = Path.Combine(server.InstallPath, "game", "bin", "win64");
            _logger.LogDebug("工作目录: {Dir}", workingDirectory);

            // 启动进程
            var process = await _platformProvider.StartServerProcessAsync(
                executablePath, arguments, workingDirectory);

            // 检查进程是否立即退出
            if (process.HasExited)
            {
                _logger.LogError("进程启动后立即退出，退出代码: {ExitCode}", process.ExitCode);
                ChangeServerStatus(server, ServerStatus.Crashed);
                return false;
            }

            // 创建服务器进程包装
            var serverProcess = new ServerProcess
            {
                Process = process,
                Server = server
            };

            _runningServers[serverId] = serverProcess;

            // 开始读取输出
            StartReadingOutput(serverProcess);

            server.LastStartedAt = DateTime.Now;
            await UpdateServerAsync(server);

            // 延迟一点再设置为运行状态，确保进程稳定
            await Task.Delay(500);
            
            // 再次检查进程是否还在运行
            if (process.HasExited)
            {
                _logger.LogError("进程启动后很快退出，退出代码: {ExitCode}", process.ExitCode);
                _runningServers.Remove(serverId);
                ChangeServerStatus(server, ServerStatus.Crashed);
                return false;
            }

            ChangeServerStatus(server, ServerStatus.Running);
            _logger.LogInformation("服务器已启动: {Name}", server.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动服务器失败: {Name}", server.Name);
            
            // 确保从运行列表中移除
            _runningServers.Remove(serverId);
            
            ChangeServerStatus(server, ServerStatus.Crashed);
            return false;
        }
    }

    public async Task<bool> StopServerAsync(string serverId, bool force = false)
    {
        var server = await GetServerByIdAsync(serverId);
        if (server == null)
        {
            return false;
        }

        // 即使不在运行列表中，也尝试更新状态
        if (!_runningServers.ContainsKey(serverId))
        {
            _logger.LogWarning("服务器不在运行列表中，直接设置为停止状态: {Name}", server.Name);
            ChangeServerStatus(server, ServerStatus.Stopped);
            return true;
        }

        try
        {
            ChangeServerStatus(server, ServerStatus.Stopping);

            var serverProcess = _runningServers[serverId];
            
            // 标记为用户主动停止
            serverProcess.IsManualStop = true;
            
            // 检查进程是否已经退出
            if (serverProcess.Process.HasExited)
            {
                _logger.LogInformation("进程已经退出，直接清理: {Name}", server.Name);
                _runningServers.Remove(serverId);
                ChangeServerStatus(server, ServerStatus.Stopped);
                return true;
            }

            await _platformProvider.StopServerProcessAsync(serverProcess.Process, force);

            _runningServers.Remove(serverId);
            ChangeServerStatus(server, ServerStatus.Stopped);

            _logger.LogInformation("服务器已停止: {Name}", server.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止服务器失败: {Name}", server.Name);
            
            // 即使出错也要移除并更新状态
            _runningServers.Remove(serverId);
            ChangeServerStatus(server, ServerStatus.Stopped);
            
            return false;
        }
    }

    public async Task<bool> RestartServerAsync(string serverId)
    {
        if (!await StopServerAsync(serverId))
        {
            return false;
        }

        await Task.Delay(2000); // 等待2秒
        return await StartServerAsync(serverId);
    }

    public async Task<bool> SendCommandAsync(string serverId, string command)
    {
        // 注意：CS2服务器不支持通过stdin发送命令（会导致CTextConsoleWin错误）
        // 要发送命令到服务器，请配置RCON密码并使用RCON工具
        
        _logger.LogWarning("通过stdin发送命令已禁用（避免控制台错误）。请使用RCON发送命令到服务器。");
        _logger.LogInformation("尝试发送的命令: {Command} 到服务器 {Id}", command, serverId);
        
        // 返回false表示不支持此操作
        await Task.CompletedTask;
        return false;
    }

    /// <summary>
    /// 获取服务器日志目录
    /// </summary>
    public string GetServerLogDirectory(string serverId)
    {
        var server = _servers.FirstOrDefault(s => s.Id == serverId);
        if (server == null)
        {
            throw new InvalidOperationException($"Server not found: {serverId}");
        }

        return Path.Combine(server.InstallPath, "game", "csgo", "logs");
    }

    /// <summary>
    /// 获取服务器日志文件列表
    /// </summary>
    public async Task<List<string>> GetServerLogFilesAsync(string serverId)
    {
        var logDir = GetServerLogDirectory(serverId);
        
        if (!Directory.Exists(logDir))
        {
            _logger.LogWarning("日志目录不存在: {Dir}", logDir);
            return new List<string>();
        }

        var logFiles = Directory.GetFiles(logDir, "L*.log")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToList();

        return await Task.FromResult(logFiles);
    }

    /// <summary>
    /// 读取日志文件内容
    /// </summary>
    public async Task<string> ReadLogFileAsync(string logFilePath, int maxLines = 1000)
    {
        if (!File.Exists(logFilePath))
        {
            return string.Empty;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(logFilePath);
            var recentLines = lines.TakeLast(maxLines);
            return string.Join(Environment.NewLine, recentLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取日志文件失败: {Path}", logFilePath);
            return $"读取日志失败: {ex.Message}";
        }
    }

    // BuildStartupArguments 方法已移至 LaunchArgumentsBuilder 工具类
    // 以便在UI和服务中共享使用

    private void StartReadingOutput(ServerProcess serverProcess)
    {
        var process = serverProcess.Process;
        var serverId = serverProcess.Server.Id;

        // 读取标准输出
        Task.Run(async () =>
        {
            try
            {
                while (!process.HasExited)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line != null)
                    {
                        OnLogReceived(serverId, line, Abstractions.LogLevel.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取服务器输出失败");
            }
        });

        // 读取标准错误
        Task.Run(async () =>
        {
            try
            {
                while (!process.HasExited)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (line != null)
                    {
                        OnLogReceived(serverId, line, Abstractions.LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取服务器错误输出失败");
            }
        });

        // 监控进程退出
        Task.Run(async () =>
        {
            try
            {
                await Task.Run(() => process.WaitForExit());
                
                _logger.LogInformation("服务器进程已退出，ServerId: {ServerId}, ExitCode: {ExitCode}", 
                    serverId, process.ExitCode);
                
                // 获取 ServerProcess 以检查是否为手动停止
                var isManualStop = false;
                if (_runningServers.TryGetValue(serverId, out var sp))
                {
                    isManualStop = sp.IsManualStop;
                }
                
                _runningServers.Remove(serverId);
                var server = await GetServerByIdAsync(serverId);
                if (server != null)
                {
                    // 如果是用户主动停止，则设置为 Stopped
                    // 否则根据退出代码判断（0 表示正常退出，非 0 表示崩溃）
                    ServerStatus newStatus;
                    if (isManualStop)
                    {
                        newStatus = ServerStatus.Stopped;
                        _logger.LogInformation("用户主动停止服务器，设置状态为 Stopped");
                    }
                    else
                    {
                        newStatus = process.ExitCode == 0 ? ServerStatus.Stopped : ServerStatus.Crashed;
                        _logger.LogInformation("服务器自动退出，退出代码: {ExitCode}, 状态: {Status}", 
                            process.ExitCode, newStatus);
                    }
                    
                    ChangeServerStatus(server, newStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "监控进程退出时发生错误: {ServerId}", serverId);
                
                // 确保清理
                _runningServers.Remove(serverId);
                var server = await GetServerByIdAsync(serverId);
                if (server != null)
                {
                    ChangeServerStatus(server, ServerStatus.Crashed);
                }
            }
        });
    }

    private void ChangeServerStatus(Server server, ServerStatus newStatus)
    {
        var oldStatus = server.Status;
        server.Status = newStatus;

        StatusChanged?.Invoke(this, new ServerStatusChangedEventArgs
        {
            ServerId = server.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus
        });
    }

    private void OnLogReceived(string serverId, string content, Abstractions.LogLevel level)
    {
        LogReceived?.Invoke(this, new LogReceivedEventArgs
        {
            ServerId = serverId,
            Content = content,
            Level = level
        });
    }
}

/// <summary>
/// 服务器进程包装
/// </summary>
internal class ServerProcess
{
    public required Process Process { get; set; }
    public required Server Server { get; set; }
    
    /// <summary>
    /// 标记是否为用户主动停止
    /// </summary>
    public bool IsManualStop { get; set; }
}

