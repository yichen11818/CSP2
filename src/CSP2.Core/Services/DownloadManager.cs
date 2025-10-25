using CSP2.Core.Abstractions;
using CSP2.Core.Models;

namespace CSP2.Core.Services;

/// <summary>
/// 下载管理器实现
/// </summary>
public class DownloadManager : IDownloadManager
{
    private readonly List<DownloadTask> _tasks = new();
    private readonly object _lock = new();

    public IReadOnlyList<DownloadTask> Tasks
    {
        get
        {
            lock (_lock)
            {
                return _tasks.AsReadOnly();
            }
        }
    }

    public int ActiveTaskCount
    {
        get
        {
            lock (_lock)
            {
                return _tasks.Count(t => t.Status == DownloadTaskStatus.Downloading || 
                                       t.Status == DownloadTaskStatus.Pending);
            }
        }
    }

    public event EventHandler<DownloadTask>? TaskAdded;
    public event EventHandler<DownloadTask>? TaskUpdated;
    public event EventHandler<DownloadTask>? TaskCompleted;
    public event EventHandler<DownloadTask>? TaskFailed;

    public void AddTask(DownloadTask task)
    {
        lock (_lock)
        {
            _tasks.Add(task);
        }
        TaskAdded?.Invoke(this, task);
    }

    public Task StartTaskAsync(string taskId)
    {
        DownloadTask? task;
        lock (_lock)
        {
            task = _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        if (task == null)
            return Task.CompletedTask;

        task.Status = DownloadTaskStatus.Downloading;
        task.StartTime = DateTime.Now;
        TaskUpdated?.Invoke(this, task);

        return Task.CompletedTask;
    }

    public Task PauseTaskAsync(string taskId)
    {
        DownloadTask? task;
        lock (_lock)
        {
            task = _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        if (task == null)
            return Task.CompletedTask;

        task.Status = DownloadTaskStatus.Paused;
        TaskUpdated?.Invoke(this, task);

        return Task.CompletedTask;
    }

    public Task CancelTaskAsync(string taskId)
    {
        DownloadTask? task;
        lock (_lock)
        {
            task = _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        if (task == null)
            return Task.CompletedTask;

        task.Status = DownloadTaskStatus.Cancelled;
        TaskUpdated?.Invoke(this, task);

        return Task.CompletedTask;
    }

    public void RemoveTask(string taskId)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                _tasks.Remove(task);
            }
        }
    }

    public void ClearCompletedTasks()
    {
        lock (_lock)
        {
            _tasks.RemoveAll(t => t.Status == DownloadTaskStatus.Completed || 
                                 t.Status == DownloadTaskStatus.Cancelled ||
                                 t.Status == DownloadTaskStatus.Failed);
        }
    }

    public void UpdateTaskProgress(string taskId, double progress, string? logMessage = null)
    {
        DownloadTask? task;
        lock (_lock)
        {
            task = _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        if (task == null)
            return;

        task.Progress = progress;
        
        if (!string.IsNullOrEmpty(logMessage))
        {
            task.LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] {logMessage}");
        }

        // 如果进度达到100%，标记为完成
        if (progress >= 100 && task.Status == DownloadTaskStatus.Downloading)
        {
            task.Status = DownloadTaskStatus.Completed;
            task.CompletedTime = DateTime.Now;
            TaskCompleted?.Invoke(this, task);
        }
        else
        {
            TaskUpdated?.Invoke(this, task);
        }
    }

    public void UpdateTaskStatus(string taskId, DownloadTaskStatus status, string? errorMessage = null)
    {
        DownloadTask? task;
        lock (_lock)
        {
            task = _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        if (task == null)
            return;

        task.Status = status;
        
        if (!string.IsNullOrEmpty(errorMessage))
        {
            task.ErrorMessage = errorMessage;
            task.LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] 错误: {errorMessage}");
        }

        if (status == DownloadTaskStatus.Completed)
        {
            task.CompletedTime = DateTime.Now;
            TaskCompleted?.Invoke(this, task);
        }
        else if (status == DownloadTaskStatus.Failed)
        {
            TaskFailed?.Invoke(this, task);
        }
        else
        {
            TaskUpdated?.Invoke(this, task);
        }
    }
}

