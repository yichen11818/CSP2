namespace CSP2.Core.Models;

/// <summary>
/// 安装结果
/// </summary>
public class InstallResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 异常信息（如果有）
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 已安装的文件列表
    /// </summary>
    public List<string> InstalledFiles { get; set; } = new();

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static InstallResult CreateSuccess(string message, List<string>? installedFiles = null)
    {
        return new InstallResult
        {
            Success = true,
            Message = message,
            InstalledFiles = installedFiles ?? new()
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static InstallResult CreateFailure(string message, Exception? exception = null)
    {
        return new InstallResult
        {
            Success = false,
            Message = message,
            ErrorMessage = exception?.Message,
            Exception = exception
        };
    }
}

