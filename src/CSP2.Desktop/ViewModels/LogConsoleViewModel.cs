using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using System.Collections.ObjectModel;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 日志控制台ViewModel
/// </summary>
public partial class LogConsoleViewModel : ObservableObject
{
    private readonly IServerManager _serverManager;

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

    public LogConsoleViewModel(IServerManager serverManager)
    {
        _serverManager = serverManager;
        
        DebugLogger.Debug("LogConsoleViewModel", "构造函数开始执行");
        
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
            
            // 限制日志数量，防止内存溢出
            if (Logs.Count > 1000)
            {
                Logs.RemoveAt(0);
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
            // 添加命令到日志
            var commandLog = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = $"> {CommandText}",
                Level = UILogLevel.Command
            };
            Logs.Add(commandLog);

            // 发送命令
            await _serverManager.SendCommandAsync(SelectedServer.Id, CommandText);
            DebugLogger.Debug("SendCommandAsync", "命令发送成功");
            
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
        return SelectedServer != null && 
               SelectedServer.Status == ServerStatus.Running && 
               !string.IsNullOrWhiteSpace(CommandText);
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
    }

    /// <summary>
    /// 切换服务器
    /// </summary>
    partial void OnSelectedServerChanged(Server? value)
    {
        // 切换服务器时清空日志
        Logs.Clear();
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

