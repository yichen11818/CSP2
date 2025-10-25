using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
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

    [ObservableProperty]
    private ObservableCollection<DownloadTask> _downloadTasks = new();

    [ObservableProperty]
    private DownloadTask? _selectedTask;

    public DownloadManagerViewModel(IDownloadManager downloadManager)
    {
        _downloadManager = downloadManager;

        // 订阅事件
        _downloadManager.TaskAdded += OnTaskAdded;
        _downloadManager.TaskUpdated += OnTaskUpdated;
        _downloadManager.TaskCompleted += OnTaskCompleted;
        _downloadManager.TaskFailed += OnTaskFailed;

        // 加载现有任务
        LoadTasks();
    }

    private void LoadTasks()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            DownloadTasks.Clear();
            foreach (var task in _downloadManager.Tasks)
            {
                DownloadTasks.Add(task);
            }
        });
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
            await _downloadManager.PauseTaskAsync(task.Id);
        }
    }

    [RelayCommand]
    private async Task CancelTask(DownloadTask task)
    {
        if (task != null)
        {
            await _downloadManager.CancelTaskAsync(task.Id);
        }
    }

    [RelayCommand]
    private void RemoveTask(DownloadTask task)
    {
        if (task != null)
        {
            _downloadManager.RemoveTask(task.Id);
            DownloadTasks.Remove(task);
        }
    }

    [RelayCommand]
    private void ClearCompleted()
    {
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
    }
}

