using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// Workshop 地图历史 ViewModel
/// </summary>
public partial class MapHistoryViewModel : ObservableObject
{
    private readonly IMapHistoryService _mapHistoryService;
    private readonly IServerManager _serverManager;
    private readonly ILogger<MapHistoryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<MapHistoryEntry> _mapHistory = new();

    [ObservableProperty]
    private MapHistoryEntry? _selectedMap;

    [ObservableProperty]
    private string? _currentServerId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _totalMaps;

    [ObservableProperty]
    private int _totalLoads;

    public ICommand LoadHistoryCommand { get; }
    public ICommand DeleteMapCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand OpenWorkshopPageCommand { get; }
    public ICommand RefreshCommand { get; }

    public MapHistoryViewModel(
        IMapHistoryService mapHistoryService,
        IServerManager serverManager,
        ILogger<MapHistoryViewModel> logger)
    {
        _mapHistoryService = mapHistoryService;
        _serverManager = serverManager;
        _logger = logger;

        LoadHistoryCommand = new AsyncRelayCommand<string>(LoadHistoryAsync);
        DeleteMapCommand = new AsyncRelayCommand<MapHistoryEntry>(DeleteMapAsync);
        ClearHistoryCommand = new AsyncRelayCommand(ClearHistoryAsync);
        OpenWorkshopPageCommand = new RelayCommand<string>(OpenWorkshopPage);
        RefreshCommand = new AsyncRelayCommand(RefreshHistoryAsync);
    }

    /// <summary>
    /// 加载地图历史
    /// </summary>
    private async Task LoadHistoryAsync(string? serverId)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在加载地图历史...";
            CurrentServerId = serverId;

            List<MapHistoryEntry> entries;

            if (string.IsNullOrEmpty(serverId))
            {
                // 加载所有服务器的地图历史
                entries = await _mapHistoryService.GetAllEntriesAsync();
                StatusMessage = "显示所有服务器的地图历史";
            }
            else
            {
                // 加载指定服务器的地图历史
                entries = await _mapHistoryService.GetServerEntriesAsync(serverId);
                
                var server = await _serverManager.GetServerByIdAsync(serverId);
                StatusMessage = server != null 
                    ? $"显示服务器 {server.Name} 的地图历史"
                    : "显示地图历史";
            }

            // 更新UI
            MapHistory.Clear();
            foreach (var entry in entries.OrderByDescending(e => e.LastLoadedAt))
            {
                MapHistory.Add(entry);
            }

            // 更新统计
            TotalMaps = entries.Count;
            TotalLoads = entries.Sum(e => e.LoadCount);

            _logger.LogInformation("已加载 {Count} 条地图历史记录", entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载地图历史失败");
            StatusMessage = $"加载失败: {ex.Message}";
            MessageBox.Show($"加载地图历史失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 刷新当前历史
    /// </summary>
    private async Task RefreshHistoryAsync()
    {
        await LoadHistoryAsync(CurrentServerId);
    }

    /// <summary>
    /// 删除地图记录
    /// </summary>
    private async Task DeleteMapAsync(MapHistoryEntry? entry)
    {
        if (entry == null) return;

        try
        {
            var result = MessageBox.Show(
                $"确定要删除地图 \"{entry.MapName}\" 的历史记录吗？\n\n这将同时删除预览图。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            StatusMessage = "正在删除...";

            if (!string.IsNullOrEmpty(CurrentServerId))
            {
                var success = await _mapHistoryService.DeleteEntryAsync(CurrentServerId, entry.WorkshopId);
                
                if (success)
                {
                    MapHistory.Remove(entry);
                    TotalMaps = MapHistory.Count;
                    TotalLoads = MapHistory.Sum(e => e.LoadCount);
                    
                    StatusMessage = $"已删除 {entry.MapName}";
                    _logger.LogInformation("已删除地图记录: {MapName} ({WorkshopId})",
                        entry.MapName, entry.WorkshopId);
                }
                else
                {
                    StatusMessage = "删除失败";
                    MessageBox.Show("删除地图记录失败", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除地图记录失败");
            StatusMessage = $"删除失败: {ex.Message}";
            MessageBox.Show($"删除地图记录失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    private async Task ClearHistoryAsync()
    {
        try
        {
            var result = MessageBox.Show(
                string.IsNullOrEmpty(CurrentServerId)
                    ? "确定要清空所有服务器的地图历史吗？\n\n这将删除所有记录和预览图，此操作不可撤销。"
                    : "确定要清空当前服务器的地图历史吗？\n\n这将删除所有记录和预览图，此操作不可撤销。",
                "确认清空",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            IsLoading = true;
            StatusMessage = "正在清空历史...";

            if (string.IsNullOrEmpty(CurrentServerId))
            {
                await _mapHistoryService.ClearAllHistoryAsync();
                _logger.LogInformation("已清空所有地图历史");
            }
            else
            {
                await _mapHistoryService.ClearServerHistoryAsync(CurrentServerId);
                _logger.LogInformation("已清空服务器地图历史: {ServerId}", CurrentServerId);
            }

            MapHistory.Clear();
            TotalMaps = 0;
            TotalLoads = 0;
            StatusMessage = "历史已清空";

            MessageBox.Show("地图历史已清空", "完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空历史失败");
            StatusMessage = $"清空失败: {ex.Message}";
            MessageBox.Show($"清空历史失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 打开 Workshop 页面
    /// </summary>
    private void OpenWorkshopPage(string? workshopId)
    {
        if (string.IsNullOrEmpty(workshopId)) return;

        try
        {
            var url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopId}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            _logger.LogDebug("打开 Workshop 页面: {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开 Workshop 页面失败: {WorkshopId}", workshopId);
            MessageBox.Show($"打开 Workshop 页面失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

