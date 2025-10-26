using System;
using System.Threading.Tasks;

namespace CSP2.Core.Abstractions;

/// <summary>
/// RCON 客户端接口
/// 用于通过 RCON 协议远程管理 CS2 服务器
/// </summary>
public interface IRCONClient : IDisposable
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 服务器地址
    /// </summary>
    string Host { get; }

    /// <summary>
    /// 服务器端口
    /// </summary>
    int Port { get; }

    /// <summary>
    /// 连接到 RCON 服务器
    /// </summary>
    /// <param name="host">服务器地址</param>
    /// <param name="port">RCON 端口（默认 27015）</param>
    /// <param name="password">RCON 密码</param>
    /// <param name="timeout">连接超时时间（毫秒）</param>
    /// <returns>是否连接成功</returns>
    Task<bool> ConnectAsync(string host, int port, string password, int timeout = 5000);

    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 发送命令到服务器
    /// </summary>
    /// <param name="command">要执行的命令</param>
    /// <returns>服务器响应</returns>
    Task<string> SendCommandAsync(string command);

    /// <summary>
    /// RCON 连接状态改变事件
    /// </summary>
    event EventHandler<RCONConnectionChangedEventArgs>? ConnectionChanged;

    /// <summary>
    /// RCON 错误事件
    /// </summary>
    event EventHandler<RCONErrorEventArgs>? ErrorOccurred;
}

/// <summary>
/// RCON 连接状态改变事件参数
/// </summary>
public class RCONConnectionChangedEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// RCON 错误事件参数
/// </summary>
public class RCONErrorEventArgs : EventArgs
{
    public Exception Exception { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}

