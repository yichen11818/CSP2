using Microsoft.Extensions.Logging;
using System.Text;

namespace CSP2.Core.Logging;

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
    
    // 文件日志
    private static StreamWriter? _fileWriter;
    private static readonly object _fileLock = new object();

    /// <summary>
    /// 初始化文件日志
    /// </summary>
    public static void InitializeFileLogging(string logDirectory)
    {
        try
        {
            Directory.CreateDirectory(logDirectory);
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var logFilePath = Path.Combine(logDirectory, $"app_{timestamp}.log");
            
            lock (_fileLock)
            {
                _fileWriter?.Close();
                _fileWriter?.Dispose();
                
                _fileWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
            
            Info("DebugLogger", $"文件日志已启用: {logFilePath}");
        }
        catch (Exception ex)
        {
            // 文件日志初始化失败不应影响程序运行
            Console.WriteLine($"初始化文件日志失败: {ex.Message}");
        }
    }
    
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

        // 写入文件日志
        WriteToFile(logEvent);

        // 触发事件通知订阅者
        LogReceived?.Invoke(null, logEvent);
    }
    
    /// <summary>
    /// 写入文件日志
    /// </summary>
    private static void WriteToFile(DebugLogEventArgs logEvent)
    {
        try
        {
            lock (_fileLock)
            {
                if (_fileWriter != null)
                {
                    var logLine = $"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEvent.Level,11}] [{logEvent.Category}] {logEvent.Message}";
                    _fileWriter.WriteLine(logLine);
                    
                    if (!string.IsNullOrEmpty(logEvent.Exception))
                    {
                        _fileWriter.WriteLine($"    Exception: {logEvent.Exception}");
                    }
                }
            }
        }
        catch
        {
            // 忽略文件写入错误，避免影响程序运行
        }
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

