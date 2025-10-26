using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
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

    public LogConsoleViewModel(IServerManager serverManager)
    {
        _serverManager = serverManager;
        
        DebugLogger.Debug("LogConsoleViewModel", "构造函数开始执行");
        
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
    /// 切换服务器
    /// </summary>
    partial void OnSelectedServerChanged(Server? value)
    {
        // 切换服务器时清空日志
        Logs.Clear();
        LogText = string.Empty;
        
        if (value != null)
        {
            DebugLogger.Info("OnSelectedServerChanged", $"切换到服务器: {value.Name}");
            LogText = $"=== 已切换到服务器: {value.Name} ==={Environment.NewLine}{Environment.NewLine}";
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    ~LogConsoleViewModel()
    {
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

