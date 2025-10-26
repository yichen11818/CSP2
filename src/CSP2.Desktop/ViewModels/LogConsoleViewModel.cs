using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using CSP2.Core.Utilities;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 日志控制台ViewModel
/// </summary>
public partial class LogConsoleViewModel : ObservableObject
{
    private readonly IServerManager _serverManager;
    private readonly StreamWriter? _logFileWriter;
    private readonly string? _currentLogFilePath;
    private readonly CommandHistory _commandHistory;
    private IRCONClient? _rconClient;

    [ObservableProperty]
    private ObservableCollection<Server> _servers = new();

    [ObservableProperty]
    private Server? _selectedServer;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logs = new();

    [ObservableProperty]
    private string _commandText = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private bool _useRCON = false;

    [ObservableProperty]
    private bool _rconConnected = false;

    [ObservableProperty]
    private string _rconStatus = "未连接";

    [ObservableProperty]
    private ObservableCollection<string> _quickCommands = new();

    public LogConsoleViewModel(IServerManager serverManager)
    {
        _serverManager = serverManager;
        
        DebugLogger.Debug("LogConsoleViewModel", "构造函数开始执行");
        
        // 初始化命令历史
        var dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        var historyPath = Path.Combine(dataDirectory, "command_history.txt");
        _commandHistory = new CommandHistory(maxHistorySize: 100, persistencePath: historyPath);
        DebugLogger.Debug("LogConsoleViewModel", $"命令历史已初始化，历史文件: {historyPath}");
        
        // 创建日志文件
        try
        {
            var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "server");
            Directory.CreateDirectory(logsDirectory);
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            _currentLogFilePath = Path.Combine(logsDirectory, $"console_{timestamp}.log");
            _logFileWriter = new StreamWriter(_currentLogFilePath, append: true, Encoding.UTF8)
            {
                AutoFlush = true // 立即写入
            };
            
            DebugLogger.Info("LogConsoleViewModel", $"日志文件已创建: {_currentLogFilePath}");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("LogConsoleViewModel", "创建日志文件失败", ex);
        }
        
        // 订阅日志事件
        _serverManager.LogReceived += OnLogReceived;
        DebugLogger.Debug("LogConsoleViewModel", "已订阅日志接收事件");
        
        // 加载服务器列表
        _ = LoadServersAsync();
    }

    /// <summary>
    /// 加载服务器列表
    /// </summary>
    private async Task LoadServersAsync()
    {
        DebugLogger.Debug("LoadServersAsync", "开始加载服务器列表");
        
        try
        {
            var servers = await _serverManager.GetServersAsync();
            Servers.Clear();
            foreach (var server in servers)
            {
                Servers.Add(server);
            }
            
            // 默认选择第一个运行中的服务器
            SelectedServer = servers.FirstOrDefault(s => s.Status == ServerStatus.Running) ?? servers.FirstOrDefault();
            DebugLogger.Debug("LoadServersAsync", $"加载了 {servers.Count} 个服务器");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("LoadServersAsync", $"加载服务器列表失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 根据服务器ID选择服务器
    /// </summary>
    public async void SelectServerById(string serverId)
    {
        DebugLogger.Info("SelectServerById", $"选择服务器: {serverId}");
        
        // 如果服务器列表为空，先加载
        if (Servers.Count == 0)
        {
            await LoadServersAsync();
        }
        
        // 查找并选择服务器
        var server = Servers.FirstOrDefault(s => s.Id == serverId);
        if (server != null)
        {
            SelectedServer = server;
            DebugLogger.Info("SelectServerById", $"已选择服务器: {server.Name}");
        }
        else
        {
            DebugLogger.Warning("SelectServerById", $"未找到服务器: {serverId}");
        }
    }

    /// <summary>
    /// 日志接收处理
    /// </summary>
    private void OnLogReceived(object? sender, LogReceivedEventArgs e)
    {
        // 只显示选中服务器的日志
        if (SelectedServer == null || e.ServerId != SelectedServer.Id)
            return;

        // 在UI线程添加日志
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var logEntry = new LogEntry
            {
                Timestamp = e.Timestamp,
                Message = e.Content,
                Level = DetermineLogLevel(e.Content)
            };
            
            Logs.Add(logEntry);
            
            // 更新文本显示
            var logLine = $"[{logEntry.Timestamp:HH:mm:ss}] {logEntry.Message}";
            LogText += logLine + Environment.NewLine;
            
            // 写入文件
            try
            {
                _logFileWriter?.WriteLine(logLine);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("OnLogReceived", "写入日志文件失败", ex);
            }
            
            // 限制日志数量，防止内存溢出
            if (Logs.Count > 1000)
            {
                Logs.RemoveAt(0);
                // 重建文本（移除第一行）
                var lines = LogText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    LogText = string.Join(Environment.NewLine, lines.Skip(1)) + Environment.NewLine;
                }
            }
        });
    }

    /// <summary>
    /// 判断日志级别
    /// </summary>
    private UILogLevel DetermineLogLevel(string message)
    {
        var lowerMessage = message.ToLower();
        
        if (lowerMessage.Contains("error") || lowerMessage.Contains("failed") || lowerMessage.Contains("exception"))
            return UILogLevel.Error;
        
        if (lowerMessage.Contains("warning") || lowerMessage.Contains("warn"))
            return UILogLevel.Warning;
        
        if (lowerMessage.Contains("info") || lowerMessage.Contains("loaded") || lowerMessage.Contains("started"))
            return UILogLevel.Info;
        
        return UILogLevel.Debug;
    }

    /// <summary>
    /// 发送命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private async Task SendCommandAsync()
    {
        if (SelectedServer == null || string.IsNullOrWhiteSpace(CommandText))
            return;

        DebugLogger.Info("SendCommandAsync", $"发送命令到服务器 {SelectedServer.Name}: {CommandText}");

        try
        {
            // 添加命令到历史
            _commandHistory.Add(CommandText);
            
            // 添加命令到日志
            var commandLog = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = $"> {CommandText}",
                Level = UILogLevel.Command
            };
            Logs.Add(commandLog);

            // 根据选择使用 RCON 或 stdin 发送命令
            if (UseRCON && _rconClient?.IsConnected == true)
            {
                // 通过 RCON 发送
                var response = await _rconClient.SendCommandAsync(CommandText);
                
                // 显示 RCON 响应
                if (!string.IsNullOrWhiteSpace(response))
                {
                    var responseLog = new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Message = response,
                        Level = UILogLevel.Info
                    };
                    Logs.Add(responseLog);
                    LogText += $"[{responseLog.Timestamp:HH:mm:ss}] {response}{Environment.NewLine}";
                }
                
                DebugLogger.Debug("SendCommandAsync", "RCON 命令发送成功");
            }
            else
            {
                // 通过 stdin 发送
                await _serverManager.SendCommandAsync(SelectedServer.Id, CommandText);
                DebugLogger.Debug("SendCommandAsync", "命令发送成功");
            }
            
            // 清空输入框
            CommandText = string.Empty;
        }
        catch (Exception ex)
        {
            DebugLogger.Error("SendCommandAsync", $"发送命令失败: {ex.Message}", ex);
            
            var errorLog = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = $"错误: {ex.Message}",
                Level = UILogLevel.Error
            };
            Logs.Add(errorLog);
        }
    }

    private bool CanSendCommand()
    {
        if (SelectedServer == null || string.IsNullOrWhiteSpace(CommandText))
            return false;

        // 如果使用 RCON，必须已连接
        if (UseRCON)
            return RconConnected;

        // 否则服务器必须在运行
        return SelectedServer.Status == ServerStatus.Running;
    }

    /// <summary>
    /// 连接 RCON
    /// </summary>
    [RelayCommand]
    private async Task ConnectRCONAsync()
    {
        if (SelectedServer == null)
            return;

        try
        {
            DebugLogger.Info("ConnectRCONAsync", "开始连接 RCON");

            // 创建 RCON 客户端
            _rconClient = new CSP2.Core.Services.RCONClient();

            // 获取 RCON 配置
            var config = SelectedServer.RCONConfig;
            if (string.IsNullOrWhiteSpace(config.Password))
            {
                System.Windows.MessageBox.Show(
                    "请先在服务器配置中设置 RCON 密码！",
                    "RCON 配置错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            RconStatus = "连接中...";

            // 连接
            var success = await _rconClient.ConnectAsync(
                config.Host,
                config.Port,
                config.Password,
                config.Timeout);

            if (success)
            {
                RconConnected = true;
                RconStatus = $"已连接 ({config.Host}:{config.Port})";
                DebugLogger.Info("ConnectRCONAsync", "RCON 连接成功");

                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Message = $"=== RCON 已连接到 {config.Host}:{config.Port} ===",
                    Level = UILogLevel.Info
                };
                Logs.Add(logEntry);
                LogText += $"[{logEntry.Timestamp:HH:mm:ss}] {logEntry.Message}{Environment.NewLine}";
            }
            else
            {
                RconConnected = false;
                RconStatus = "连接失败";
                DebugLogger.Error("ConnectRCONAsync", "RCON 连接失败", null!);

                System.Windows.MessageBox.Show(
                    $"RCON 连接失败！\n\n请检查:\n1. 服务器是否启动\n2. RCON 密码是否正确\n3. IP 和端口是否正确",
                    "RCON 连接失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            RconConnected = false;
            RconStatus = "连接失败";
            DebugLogger.Error("ConnectRCONAsync", $"RCON 连接异常: {ex.Message}", ex);

            System.Windows.MessageBox.Show(
                $"RCON 连接异常:\n{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 断开 RCON
    /// </summary>
    [RelayCommand]
    private async Task DisconnectRCONAsync()
    {
        try
        {
            if (_rconClient != null)
            {
                await _rconClient.DisconnectAsync();
                _rconClient.Dispose();
                _rconClient = null;
            }

            RconConnected = false;
            RconStatus = "未连接";
            DebugLogger.Info("DisconnectRCONAsync", "RCON 已断开连接");

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = "=== RCON 已断开连接 ===",
                Level = UILogLevel.Info
            };
            Logs.Add(logEntry);
            LogText += $"[{logEntry.Timestamp:HH:mm:ss}] {logEntry.Message}{Environment.NewLine}";
        }
        catch (Exception ex)
        {
            DebugLogger.Error("DisconnectRCONAsync", $"断开 RCON 失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
        LogText = string.Empty;
    }

    /// <summary>
    /// 复制日志到剪贴板
    /// </summary>
    [RelayCommand]
    private void CopyLogs()
    {
        try
        {
            if (!string.IsNullOrEmpty(LogText))
            {
                System.Windows.Clipboard.SetText(LogText);
                DebugLogger.Info("CopyLogs", "日志已复制到剪贴板");
                
                System.Windows.MessageBox.Show(
                    "日志已复制到剪贴板！",
                    "复制成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "没有可复制的日志",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error("CopyLogs", "复制日志失败", ex);
            System.Windows.MessageBox.Show(
                $"复制失败：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 导出日志到文件
    /// </summary>
    [RelayCommand]
    private void ExportLogs()
    {
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "日志文件 (*.log)|*.log|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                DefaultExt = ".log",
                FileName = $"server_log_{DateTime.Now:yyyy-MM-dd_HHmmss}.log",
                Title = "导出日志"
            };

            if (saveDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveDialog.FileName, LogText, Encoding.UTF8);
                DebugLogger.Info("ExportLogs", $"日志已导出到: {saveDialog.FileName}");
                
                var result = System.Windows.MessageBox.Show(
                    $"日志已成功导出到：\n{saveDialog.FileName}\n\n是否打开文件所在文件夹？",
                    "导出成功",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    var directory = Path.GetDirectoryName(saveDialog.FileName);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", directory);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error("ExportLogs", "导出日志失败", ex);
            System.Windows.MessageBox.Show(
                $"导出失败：{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 导航到上一条命令（↑ 键）
    /// </summary>
    public void NavigateHistoryOlder()
    {
        var command = _commandHistory.GetOlder();
        if (command != null)
        {
            CommandText = command;
        }
    }

    /// <summary>
    /// 导航到下一条命令（↓ 键）
    /// </summary>
    public void NavigateHistoryNewer()
    {
        var command = _commandHistory.GetNewer();
        if (command != null)
        {
            CommandText = command;
        }
    }

    /// <summary>
    /// 添加快捷命令
    /// </summary>
    [RelayCommand]
    private async Task AddQuickCommandAsync()
    {
        if (SelectedServer == null || string.IsNullOrWhiteSpace(CommandText))
            return;

        var command = CommandText.Trim();

        // 检查是否已存在
        if (QuickCommands.Contains(command))
        {
            System.Windows.MessageBox.Show(
                "该命令已存在于快捷命令列表中！",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // 添加到列表
        QuickCommands.Add(command);
        
        // 保存到服务器配置
        SelectedServer.Config.QuickCommands = QuickCommands.ToList();
        await _serverManager.UpdateServerAsync(SelectedServer);

        DebugLogger.Info("AddQuickCommandAsync", $"添加快捷命令: {command}");
        
        System.Windows.MessageBox.Show(
            $"快捷命令已添加：{command}",
            "成功",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// 执行快捷命令
    /// </summary>
    [RelayCommand]
    private async Task ExecuteQuickCommandAsync(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        // 设置到输入框并发送
        CommandText = command;
        await SendCommandAsync();
    }

    /// <summary>
    /// 删除快捷命令
    /// </summary>
    [RelayCommand]
    private async Task RemoveQuickCommandAsync(string? command)
    {
        if (string.IsNullOrWhiteSpace(command) || SelectedServer == null)
            return;

        var result = System.Windows.MessageBox.Show(
            $"确定要删除快捷命令吗？\n\n{command}",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            QuickCommands.Remove(command);
            
            // 保存到服务器配置
            SelectedServer.Config.QuickCommands = QuickCommands.ToList();
            await _serverManager.UpdateServerAsync(SelectedServer);

            DebugLogger.Info("RemoveQuickCommandAsync", $"删除快捷命令: {command}");
        }
    }

    /// <summary>
    /// 切换服务器
    /// </summary>
    partial void OnSelectedServerChanged(Server? value)
    {
        // 切换服务器时清空日志
        Logs.Clear();
        LogText = string.Empty;
        
        // 加载服务器的快捷命令
        QuickCommands.Clear();
        if (value != null)
        {
            DebugLogger.Info("OnSelectedServerChanged", $"切换到服务器: {value.Name}");
            LogText = $"=== 已切换到服务器: {value.Name} ==={Environment.NewLine}{Environment.NewLine}";
            
            // 加载快捷命令
            foreach (var cmd in value.Config.QuickCommands)
            {
                QuickCommands.Add(cmd);
            }
        }
    }
    
    /// <summary>
    /// 切换 RCON 模式
    /// </summary>
    partial void OnUseRCONChanged(bool value)
    {
        if (value && !RconConnected)
        {
            // 自动尝试连接 RCON
            _ = ConnectRCONAsync();
        }
        else if (!value && RconConnected)
        {
            // 切换回 stdin 时断开 RCON
            _ = DisconnectRCONAsync();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    ~LogConsoleViewModel()
    {
        _rconClient?.Dispose();
        _logFileWriter?.Close();
        _logFileWriter?.Dispose();
    }
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
    public UILogLevel Level { get; set; }
}

/// <summary>
/// UI日志级别（区别于Core的LogLevel）
/// </summary>
public enum UILogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Command
}

