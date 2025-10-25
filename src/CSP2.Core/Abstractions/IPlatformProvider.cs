using CSP2.Core.Models;
using System.Diagnostics;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 平台提供者接口 - 抽象不同操作系统的差异
/// </summary>
public interface IPlatformProvider
{
    /// <summary>
    /// 提供者元数据
    /// </summary>
    ProviderMetadata Metadata { get; }

    /// <summary>
    /// 当前平台是否支持
    /// </summary>
    bool IsSupported();

    /// <summary>
    /// 启动服务器进程
    /// </summary>
    /// <param name="serverPath">服务器可执行文件路径</param>
    /// <param name="arguments">启动参数</param>
    /// <param name="workingDirectory">工作目录</param>
    /// <returns>进程对象</returns>
    Task<Process> StartServerProcessAsync(string serverPath, string arguments, string workingDirectory);

    /// <summary>
    /// 停止服务器进程
    /// </summary>
    /// <param name="process">进程对象</param>
    /// <param name="force">是否强制终止</param>
    Task StopServerProcessAsync(Process process, bool force = false);

    /// <summary>
    /// 检查端口是否被占用
    /// </summary>
    /// <param name="port">端口号</param>
    /// <returns>是否被占用</returns>
    Task<bool> IsPortInUseAsync(int port);

    /// <summary>
    /// 获取系统信息
    /// </summary>
    /// <returns>系统信息字典</returns>
    Task<Dictionary<string, string>> GetSystemInfoAsync();

    /// <summary>
    /// 检查文件是否有执行权限（Linux特有）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否有执行权限</returns>
    Task<bool> HasExecutePermissionAsync(string filePath);

    /// <summary>
    /// 设置文件执行权限（Linux特有）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    Task SetExecutePermissionAsync(string filePath);
}

