using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 下载管理器ViewModel
/// </summary>
public partial class DownloadManagerViewModel : ObservableObject
{
    private readonly IDownloadManager _downloadManager;
    private readonly ILogger<DownloadManagerViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<DownloadTask> _downloadTasks = new();

    [ObservableProperty]
    private DownloadTask? _selectedTask;

    public DownloadManagerViewModel(IDownloadManager downloadManager, ILogger<DownloadManagerViewModel> logger)
    {
        _downloadManager = downloadManager;
        _logger = logger;

        _logger.LogInformation("DownloadManagerViewModel 初始化");
        DebugLogger.Debug("DownloadManagerViewModel", "构造函数开始执行");

        // 订阅事件
        _downloadManager.TaskAdded += OnTaskAdded;
        _downloadManager.TaskUpdated += OnTaskUpdated;
        _downloadManager.TaskCompleted += OnTaskCompleted;
        _downloadManager.TaskFailed += OnTaskFailed;
        DebugLogger.Debug("DownloadManagerViewModel", "已订阅下载管理器事件");

        // 加载现有任务
        LoadTasks();
    }

    private void LoadTasks()
    {
        DebugLogger.Debug("LoadTasks", "开始加载下载任务");
        
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DownloadTasks.Clear();
                foreach (var task in _downloadManager.Tasks)
                {
                    DownloadTasks.Add(task);
                }
                _logger.LogInformation("加载了 {Count} 个下载任务", DownloadTasks.Count);
                DebugLogger.Debug("LoadTasks", $"加载了 {DownloadTasks.Count} 个下载任务");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载下载任务失败");
            DebugLogger.Error("LoadTasks", $"加载失败: {ex.Message}", ex);
        }
    }

    private void OnTaskAdded(object? sender, DownloadTask e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            DownloadTasks.Add(e);
        });
    }

    private void OnTaskUpdated(object? sender, DownloadTask e)
    {
        // 触发属性更新
        Application.Current.Dispatcher.Invoke(() =>
        {
            var task = DownloadTasks.FirstOrDefault(t => t.Id == e.Id);
            if (task != null)
            {
                // 触发UI更新
                var index = DownloadTasks.IndexOf(task);
                if (index >= 0)
                {
                    DownloadTasks[index] = task;
                }
            }
        });
    }

    private void OnTaskCompleted(object? sender, DownloadTask e)
    {
        OnTaskUpdated(sender, e);
    }

    private void OnTaskFailed(object? sender, DownloadTask e)
    {
        OnTaskUpdated(sender, e);
    }

    [RelayCommand]
    private async Task PauseTask(DownloadTask task)
    {
        if (task != null)
        {
            try
            {
                _logger.LogInformation("暂停下载任务: {TaskName}", task.Name);
                DebugLogger.Info("PauseTask", $"暂停任务: {task.Name}");
                await _downloadManager.PauseTaskAsync(task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停下载任务 {TaskName} 失败", task.Name);
                DebugLogger.Error("PauseTask", $"暂停任务失败: {ex.Message}", ex);
            }
        }
    }

    [RelayCommand]
    private async Task CancelTask(DownloadTask task)
    {
        if (task != null)
        {
            try
            {
                _logger.LogInformation("取消下载任务: {TaskName}", task.Name);
                DebugLogger.Info("CancelTask", $"取消任务: {task.Name}");
                await _downloadManager.CancelTaskAsync(task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消下载任务 {TaskName} 失败", task.Name);
                DebugLogger.Error("CancelTask", $"取消任务失败: {ex.Message}", ex);
            }
        }
    }

    [RelayCommand]
    private void RemoveTask(DownloadTask task)
    {
        if (task != null)
        {
            try
            {
                _logger.LogInformation("移除下载任务: {TaskName}", task.Name);
                DebugLogger.Info("RemoveTask", $"移除任务: {task.Name}");
                _downloadManager.RemoveTask(task.Id);
                DownloadTasks.Remove(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除下载任务 {TaskName} 失败", task.Name);
                DebugLogger.Error("RemoveTask", $"移除任务失败: {ex.Message}", ex);
            }
        }
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        try
        {
            _logger.LogInformation("清除已完成的下载任务");
            DebugLogger.Debug("ClearCompleted", "开始清除已完成任务");
            
            _downloadManager.ClearCompletedTasks();
            var completedTasks = DownloadTasks
                .Where(t => t.Status == DownloadTaskStatus.Completed ||
                           t.Status == DownloadTaskStatus.Cancelled ||
                           t.Status == DownloadTaskStatus.Failed)
                .ToList();

            foreach (var task in completedTasks)
            {
                DownloadTasks.Remove(task);
            }
            
            _logger.LogInformation("清除了 {Count} 个已完成任务", completedTasks.Count);
            DebugLogger.Debug("ClearCompleted", $"清除了 {completedTasks.Count} 个任务");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除已完成任务失败");
            DebugLogger.Error("ClearCompleted", $"清除失败: {ex.Message}", ex);
        }
    }
}

