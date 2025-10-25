using CSP2.Core.Models;

namespace CSP2.Core.Abstractions;

/// <summary>
/// 下载管理器接口
/// </summary>
public interface IDownloadManager
{
    /// <summary>
    /// 所有下载任务
    /// </summary>
    IReadOnlyList<DownloadTask> Tasks { get; }

    /// <summary>
    /// 活动下载任务数量
    /// </summary>
    int ActiveTaskCount { get; }

    /// <summary>
    /// 下载任务添加事件
    /// </summary>
    event EventHandler<DownloadTask>? TaskAdded;

    /// <summary>
    /// 下载任务更新事件
    /// </summary>
    event EventHandler<DownloadTask>? TaskUpdated;

    /// <summary>
    /// 下载任务完成事件
    /// </summary>
    event EventHandler<DownloadTask>? TaskCompleted;

    /// <summary>
    /// 下载任务失败事件
    /// </summary>
    event EventHandler<DownloadTask>? TaskFailed;

    /// <summary>
    /// 添加下载任务
    /// </summary>
    void AddTask(DownloadTask task);

    /// <summary>
    /// 开始下载任务
    /// </summary>
    Task StartTaskAsync(string taskId);

    /// <summary>
    /// 暂停下载任务
    /// </summary>
    Task PauseTaskAsync(string taskId);

    /// <summary>
    /// 取消下载任务
    /// </summary>
    Task CancelTaskAsync(string taskId);

    /// <summary>
    /// 移除下载任务
    /// </summary>
    void RemoveTask(string taskId);

    /// <summary>
    /// 清空已完成的任务
    /// </summary>
    void ClearCompletedTasks();

    /// <summary>
    /// 更新任务进度
    /// </summary>
    void UpdateTaskProgress(string taskId, double progress, string? logMessage = null);

    /// <summary>
    /// 更新任务状态
    /// </summary>
    void UpdateTaskStatus(string taskId, DownloadTaskStatus status, string? errorMessage = null);
}

