using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSP2.Core.Abstractions;

namespace CSP2.Core.Services;

/// <summary>
/// Source Engine RCON 客户端实现
/// 实现 Source RCON 协议: https://developer.valvesoftware.com/wiki/Source_RCON_Protocol
/// </summary>
public class RCONClient : IRCONClient
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _requestId = 1;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    public bool IsConnected => _client?.Connected ?? false;
    public string Host { get; private set; } = string.Empty;
    public int Port { get; private set; }

    public event EventHandler<RCONConnectionChangedEventArgs>? ConnectionChanged;
    public event EventHandler<RCONErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// 连接到 RCON 服务器
    /// </summary>
    public async Task<bool> ConnectAsync(string host, int port, string password, int timeout = 5000)
    {
        try
        {
            // 关闭现有连接
            await DisconnectAsync();

            Host = host;
            Port = port;

            // 创建 TCP 客户端
            _client = new TcpClient();
            
            // 设置超时
            using var cts = new CancellationTokenSource(timeout);
            await _client.ConnectAsync(host, port, cts.Token);

            _stream = _client.GetStream();

            // 发送认证请求
            var authPacket = CreatePacket(_requestId++, RCONPacketType.Auth, password);
            await SendPacketAsync(authPacket);

            // 接收认证响应
            var response = await ReceivePacketAsync(cts.Token);

            // 验证认证结果（认证失败时 ID 为 -1）
            if (response.Id == -1)
            {
                await DisconnectAsync();
                OnConnectionChanged(false, "RCON 认证失败：密码错误");
                return false;
            }

            OnConnectionChanged(true, "RCON 连接成功");
            return true;
        }
        catch (Exception ex)
        {
            await DisconnectAsync();
            OnError(ex, $"RCON 连接失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            if (_stream != null)
            {
                await _stream.DisposeAsync();
                _stream = null;
            }

            _client?.Close();
            _client?.Dispose();
            _client = null;

            OnConnectionChanged(false, "RCON 已断开连接");
        }
        catch
        {
            // 忽略断开连接时的错误
        }
    }

    /// <summary>
    /// 发送命令到服务器
    /// </summary>
    public async Task<string> SendCommandAsync(string command)
    {
        if (!IsConnected || _stream == null)
        {
            throw new InvalidOperationException("RCON 未连接");
        }

        await _sendLock.WaitAsync();
        try
        {
            // 发送命令
            var requestId = _requestId++;
            var packet = CreatePacket(requestId, RCONPacketType.ExecCommand, command);
            await SendPacketAsync(packet);

            // 接收响应
            var response = await ReceivePacketAsync(CancellationToken.None);

            // 验证响应 ID
            if (response.Id != requestId)
            {
                throw new InvalidOperationException($"响应 ID 不匹配: 期望 {requestId}, 收到 {response.Id}");
            }

            return response.Body;
        }
        catch (Exception ex)
        {
            OnError(ex, $"发送 RCON 命令失败: {ex.Message}");
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 创建 RCON 数据包
    /// </summary>
    private static byte[] CreatePacket(int id, RCONPacketType type, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var packetSize = 10 + bodyBytes.Length; // 4 (ID) + 4 (Type) + Body + 2 (null terminators)

        using var ms = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(ms);

        writer.Write(packetSize);           // Size (不包括自身的 4 字节)
        writer.Write(id);                   // Request ID
        writer.Write((int)type);            // Type
        writer.Write(bodyBytes);            // Body
        writer.Write((byte)0);              // Null terminator 1
        writer.Write((byte)0);              // Null terminator 2

        return ms.ToArray();
    }

    /// <summary>
    /// 发送数据包
    /// </summary>
    private async Task SendPacketAsync(byte[] packet)
    {
        if (_stream == null)
            throw new InvalidOperationException("网络流未初始化");

        await _stream.WriteAsync(packet);
        await _stream.FlushAsync();
    }

    /// <summary>
    /// 接收数据包
    /// </summary>
    private async Task<RCONPacket> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        if (_stream == null)
            throw new InvalidOperationException("网络流未初始化");

        // 读取包大小 (4 bytes)
        var sizeBuffer = new byte[4];
        await _stream.ReadExactlyAsync(sizeBuffer, cancellationToken);
        var size = BitConverter.ToInt32(sizeBuffer, 0);

        // 读取包内容
        var dataBuffer = new byte[size];
        await _stream.ReadExactlyAsync(dataBuffer, cancellationToken);

        // 解析包
        var id = BitConverter.ToInt32(dataBuffer, 0);
        var type = (RCONPacketType)BitConverter.ToInt32(dataBuffer, 4);
        var bodyLength = size - 10; // size - (4 bytes ID + 4 bytes type + 2 null terminators)
        var body = Encoding.UTF8.GetString(dataBuffer, 8, bodyLength);

        return new RCONPacket
        {
            Id = id,
            Type = type,
            Body = body
        };
    }

    /// <summary>
    /// 触发连接状态改变事件
    /// </summary>
    private void OnConnectionChanged(bool isConnected, string? message = null)
    {
        ConnectionChanged?.Invoke(this, new RCONConnectionChangedEventArgs
        {
            IsConnected = isConnected,
            Message = message
        });
    }

    /// <summary>
    /// 触发错误事件
    /// </summary>
    private void OnError(Exception ex, string message)
    {
        ErrorOccurred?.Invoke(this, new RCONErrorEventArgs
        {
            Exception = ex,
            Message = message
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DisconnectAsync().GetAwaiter().GetResult();
        _sendLock.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// RCON 数据包类型
/// </summary>
internal enum RCONPacketType
{
    Auth = 3,               // SERVERDATA_AUTH
    AuthResponse = 2,       // SERVERDATA_AUTH_RESPONSE
    ExecCommand = 2,        // SERVERDATA_EXECCOMMAND
    ResponseValue = 0       // SERVERDATA_RESPONSE_VALUE
}

/// <summary>
/// RCON 数据包
/// </summary>
internal class RCONPacket
{
    public int Id { get; set; }
    public RCONPacketType Type { get; set; }
    public string Body { get; set; } = string.Empty;
}

