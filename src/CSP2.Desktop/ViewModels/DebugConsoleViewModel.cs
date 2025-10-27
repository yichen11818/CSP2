using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// Debug控制台ViewModel - 显示应用程序级别的调试日志
/// </summary>
public partial class DebugConsoleViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DebugLogEntry> _logs = new();

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _showDebug = true;

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarning = true;

    [ObservableProperty]
    private bool _showError = true;

    private readonly List<DebugLogEntry> _allLogs = new();

    public DebugConsoleViewModel()
    {
        // 订阅全局日志记录器
        DebugLogger.LogReceived += OnLogReceived;
        
        // 加载历史日志
        LoadHistoryLogs();
    }
    
    /// <summary>
    /// 加载历史日志
    /// </summary>
    private void LoadHistoryLogs()
    {
        try
        {
            var history = DebugLogger.GetHistory();
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var logEvent in history)
                {
                    var entry = new DebugLogEntry
                    {
                        Timestamp = logEvent.Timestamp,
                        Level = logEvent.Level,
                        Category = logEvent.Category,
                        Message = logEvent.Message,
                        Exception = logEvent.Exception
                    };

                    _allLogs.Add(entry);

                    // 应用过滤
                    if (ShouldShowLog(entry))
                    {
                        Logs.Add(entry);
                    }
                }
            });
            
            DebugLogger.Debug("DebugConsoleViewModel", $"已加载 {history.Count} 条历史日志");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("DebugConsoleViewModel", "加载历史日志失败", ex);
        }
    }

    /// <summary>
    /// 接收日志
    /// </summary>
    private void OnLogReceived(object? sender, DebugLogEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var entry = new DebugLogEntry
            {
                Timestamp = e.Timestamp,
                Level = e.Level,
                Category = e.Category,
                Message = e.Message,
                Exception = e.Exception
            };

            _allLogs.Add(entry);

            // 应用过滤
            if (ShouldShowLog(entry))
            {
                Logs.Add(entry);

                // 限制显示的日志数量
                if (Logs.Count > 5000)
                {
                    Logs.RemoveAt(0);
                }
            }

            // 限制全部日志数量
            if (_allLogs.Count > 10000)
            {
                _allLogs.RemoveAt(0);
            }
        });
    }

    /// <summary>
    /// 判断是否应该显示该日志
    /// </summary>
    private bool ShouldShowLog(DebugLogEntry entry)
    {
        // 级别过滤
        var levelMatch = entry.Level switch
        {
            LogLevel.Debug or LogLevel.Trace => ShowDebug,
            LogLevel.Information => ShowInfo,
            LogLevel.Warning => ShowWarning,
            LogLevel.Error or LogLevel.Critical => ShowError,
            _ => true
        };

        if (!levelMatch)
            return false;

        // 文本过滤
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var filter = FilterText.ToLower();
            return entry.Message.ToLower().Contains(filter) ||
                   entry.Category.ToLower().Contains(filter) ||
                   (entry.Exception?.ToLower().Contains(filter) ?? false);
        }

        return true;
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    [RelayCommand]
    private void ClearLogs()
    {
        Logs.Clear();
        _allLogs.Clear();
    }

    /// <summary>
    /// 刷新过滤
    /// </summary>
    [RelayCommand]
    private void RefreshFilter()
    {
        Logs.Clear();
        foreach (var log in _allLogs)
        {
            if (ShouldShowLog(log))
            {
                Logs.Add(log);
            }
        }
    }

    /// <summary>
    /// 导出日志
    /// </summary>
    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        try
        {
            var fileName = $"debug_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                fileName);

            using (var writer = new System.IO.StreamWriter(filePath))
            {
                await writer.WriteLineAsync($"CSP2 Debug Logs - {DateTime.Now}");
                await writer.WriteLineAsync(new string('=', 80));
                await writer.WriteLineAsync();

                foreach (var log in _allLogs)
                {
                    await writer.WriteLineAsync($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{log.Level}] {log.Category}");
                    await writer.WriteLineAsync($"  {log.Message}");
                    if (!string.IsNullOrEmpty(log.Exception))
                    {
                        await writer.WriteLineAsync($"  Exception: {log.Exception}");
                    }
                    await writer.WriteLineAsync();
                }
            }

            System.Windows.MessageBox.Show($"日志已导出到:\n{filePath}", "导出成功",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"导出日志失败:\n{ex.Message}", "导出失败",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    partial void OnShowDebugChanged(bool value) => RefreshFilter();
    partial void OnShowInfoChanged(bool value) => RefreshFilter();
    partial void OnShowWarningChanged(bool value) => RefreshFilter();
    partial void OnShowErrorChanged(bool value) => RefreshFilter();
    partial void OnFilterTextChanged(string value) => RefreshFilter();
}

/// <summary>
/// Debug日志条目
/// </summary>
public class DebugLogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

/// <summary>
/// 全局Debug日志记录器
/// </summary>
public static class DebugLogger
{
    public static event EventHandler<DebugLogEventArgs>? LogReceived;
    public static bool IsDebugMode { get; set; }
    
    // 历史日志缓冲区 - 保存最近的日志用于后续订阅者
    private static readonly List<DebugLogEventArgs> _historyBuffer = new();
    private static readonly object _bufferLock = new object();
    private const int MaxHistorySize = 5000;

    /// <summary>
    /// 获取历史日志（用于初始化订阅者）
    /// </summary>
    public static IReadOnlyList<DebugLogEventArgs> GetHistory()
    {
        lock (_bufferLock)
        {
            return _historyBuffer.ToList();
        }
    }

    public static void Log(LogLevel level, string category, string message, Exception? exception = null)
    {
        if (!IsDebugMode && level < LogLevel.Information)
            return;

        var logEvent = new DebugLogEventArgs
        {
            Timestamp = DateTime.Now,
            Level = level,
            Category = category,
            Message = message,
            Exception = exception?.ToString()
        };

        // 添加到历史缓冲区
        lock (_bufferLock)
        {
            _historyBuffer.Add(logEvent);
            
            // 限制缓冲区大小
            if (_historyBuffer.Count > MaxHistorySize)
            {
                _historyBuffer.RemoveAt(0);
            }
        }

        // 触发事件通知订阅者
        LogReceived?.Invoke(null, logEvent);
    }

    public static void Debug(string category, string message) =>
        Log(LogLevel.Debug, category, message);

    public static void Info(string category, string message) =>
        Log(LogLevel.Information, category, message);

    public static void Warning(string category, string message) =>
        Log(LogLevel.Warning, category, message);

    public static void Error(string category, string message, Exception? exception = null) =>
        Log(LogLevel.Error, category, message, exception);
}

/// <summary>
/// Debug日志事件参数
/// </summary>
public class DebugLogEventArgs : EventArgs
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

