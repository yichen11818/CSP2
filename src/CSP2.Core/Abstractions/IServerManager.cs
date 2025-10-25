using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 服务器管理器接口
/// </summary>
public interface IServerManager
{
    /// <summary>
    /// 获取所有服务器
    /// </summary>
    /// <returns>服务器列表</returns>
    Task<List<Server>> GetServersAsync();

    /// <summary>
    /// 根据ID获取服务器
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>服务器对象，不存在返回null</returns>
    Task<Server?> GetServerByIdAsync(string serverId);

    /// <summary>
    /// 添加服务器
    /// </summary>
    /// <param name="name">服务器名称</param>
    /// <param name="installPath">安装路径</param>
    /// <param name="config">配置信息</param>
    /// <returns>创建的服务器对象</returns>
    Task<Server> AddServerAsync(string name, string installPath, ServerConfig? config = null);

    /// <summary>
    /// 更新服务器信息
    /// </summary>
    /// <param name="server">服务器对象</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateServerAsync(Server server);

    /// <summary>
    /// 删除服务器
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteServerAsync(string serverId);

    /// <summary>
    /// 启动服务器
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>是否成功</returns>
    Task<bool> StartServerAsync(string serverId);

    /// <summary>
    /// 停止服务器
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <param name="force">是否强制停止</param>
    /// <returns>是否成功</returns>
    Task<bool> StopServerAsync(string serverId, bool force = false);

    /// <summary>
    /// 重启服务器
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>是否成功</returns>
    Task<bool> RestartServerAsync(string serverId);

    /// <summary>
    /// 发送命令到服务器
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <param name="command">命令文本</param>
    /// <returns>是否成功</returns>
    Task<bool> SendCommandAsync(string serverId, string command);

    /// <summary>
    /// 获取服务器日志目录
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>日志目录路径</returns>
    string GetServerLogDirectory(string serverId);

    /// <summary>
    /// 获取服务器日志文件列表
    /// </summary>
    /// <param name="serverId">服务器ID</param>
    /// <returns>日志文件路径列表</returns>
    Task<List<string>> GetServerLogFilesAsync(string serverId);

    /// <summary>
    /// 读取日志文件内容
    /// </summary>
    /// <param name="logFilePath">日志文件路径</param>
    /// <param name="maxLines">最大读取行数</param>
    /// <returns>日志内容</returns>
    Task<string> ReadLogFileAsync(string logFilePath, int maxLines = 1000);

    /// <summary>
    /// 日志接收事件
    /// </summary>
    event EventHandler<LogReceivedEventArgs>? LogReceived;

    /// <summary>
    /// 服务器状态变化事件
    /// </summary>
    event EventHandler<ServerStatusChangedEventArgs>? StatusChanged;
}

/// <summary>
/// 日志接收事件参数
/// </summary>
public class LogReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 服务器ID
    /// </summary>
    public required string ServerId { get; set; }

    /// <summary>
    /// 日志内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Info;

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 服务器状态变化事件参数
/// </summary>
public class ServerStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 服务器ID
    /// </summary>
    public required string ServerId { get; set; }

    /// <summary>
    /// 旧状态
    /// </summary>
    public ServerStatus OldStatus { get; set; }

    /// <summary>
    /// 新状态
    /// </summary>
    public ServerStatus NewStatus { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 日志级别
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

