using System.Diagnostics;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
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
    }

    public async Task<List<Server>> GetServersAsync()
    {
        if (_servers.Count == 0)
        {
            _servers = await _configService.LoadServersAsync();
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
        var server = new Server
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            InstallPath = installPath,
            Config = config ?? new ServerConfig(),
            Status = ServerStatus.Stopped,
            CreatedAt = DateTime.Now
        };

        _servers.Add(server);
        await _configService.SaveServersAsync(_servers);

        _logger.LogInformation("已添加服务器: {Name} ({Id})", name, server.Id);
        return server;
    }

    public async Task<bool> UpdateServerAsync(Server server)
    {
        var index = _servers.FindIndex(s => s.Id == server.Id);
        if (index == -1)
        {
            return false;
        }

        _servers[index] = server;
        await _configService.SaveServersAsync(_servers);

        _logger.LogInformation("已更新服务器: {Name} ({Id})", server.Name, server.Id);
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
            _logger.LogInformation("已删除服务器: {Id}", serverId);
        }

        return removed;
    }

    public async Task<bool> StartServerAsync(string serverId)
    {
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

            // 构建启动参数
            var executablePath = Path.Combine(server.InstallPath, "game", "bin", "win64", "cs2.exe");
            if (!File.Exists(executablePath))
            {
                _logger.LogError("找不到CS2可执行文件: {Path}", executablePath);
                ChangeServerStatus(server, ServerStatus.Stopped);
                return false;
            }

            var arguments = BuildStartupArguments(server.Config);
            var workingDirectory = Path.Combine(server.InstallPath, "game", "bin", "win64");

            // 启动进程
            var process = await _platformProvider.StartServerProcessAsync(
                executablePath, arguments, workingDirectory);

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

            ChangeServerStatus(server, ServerStatus.Running);
            _logger.LogInformation("服务器已启动: {Name}", server.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动服务器失败: {Name}", server.Name);
            ChangeServerStatus(server, ServerStatus.Crashed);
            return false;
        }
    }

    public async Task<bool> StopServerAsync(string serverId, bool force = false)
    {
        var server = await GetServerByIdAsync(serverId);
        if (server == null || !_runningServers.ContainsKey(serverId))
        {
            return false;
        }

        try
        {
            ChangeServerStatus(server, ServerStatus.Stopping);

            var serverProcess = _runningServers[serverId];
            await _platformProvider.StopServerProcessAsync(serverProcess.Process, force);

            _runningServers.Remove(serverId);
            ChangeServerStatus(server, ServerStatus.Stopped);

            _logger.LogInformation("服务器已停止: {Name}", server.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止服务器失败: {Name}", server.Name);
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
        if (!_runningServers.ContainsKey(serverId))
        {
            return false;
        }

        try
        {
            var serverProcess = _runningServers[serverId];
            await serverProcess.Process.StandardInput.WriteLineAsync(command);
            await serverProcess.Process.StandardInput.FlushAsync();

            _logger.LogDebug("已发送命令到服务器 {Id}: {Command}", serverId, command);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送命令失败: {Command}", command);
            return false;
        }
    }

    private string BuildStartupArguments(ServerConfig config)
    {
        var args = new List<string>
        {
            "-dedicated",
            $"-port {config.Port}",
            $"+map {config.Map}",
            $"-maxplayers {config.MaxPlayers}",
            $"+game_type {config.GameType}",
            $"+game_mode {config.GameMode}",
            $"-tickrate {config.TickRate}"
        };

        if (!string.IsNullOrEmpty(config.SteamToken))
        {
            args.Add($"+sv_setsteamaccount {config.SteamToken}");
        }

        foreach (var (key, value) in config.CustomArgs)
        {
            args.Add($"{key} {value}");
        }

        return string.Join(" ", args);
    }

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
            await Task.Run(() => process.WaitForExit());
            
            _runningServers.Remove(serverId);
            var server = await GetServerByIdAsync(serverId);
            if (server != null)
            {
                var newStatus = process.ExitCode == 0 ? ServerStatus.Stopped : ServerStatus.Crashed;
                ChangeServerStatus(server, newStatus);
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
}

