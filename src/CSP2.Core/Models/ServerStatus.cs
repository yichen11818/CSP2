namespace CSP2.Core.Models;

/// <summary>
/// 服务器状态枚举
/// </summary>
public enum ServerStatus
{
    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,

    /// <summary>
    /// 启动中
    /// </summary>
    Starting,

    /// <summary>
    /// 运行中
    /// </summary>
    Running,

    /// <summary>
    /// 停止中
    /// </summary>
    Stopping,

    /// <summary>
    /// 崩溃
    /// </summary>
    Crashed
}

