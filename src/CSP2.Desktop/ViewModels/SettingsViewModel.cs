using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using System.Collections.ObjectModel;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 设置页面ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;

    [ObservableProperty]
    private string _theme = "浅色";

    [ObservableProperty]
    private string _language = "简体中文";

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private string _steamCmdPath = @"C:\CSP2\steamcmd";

    [ObservableProperty]
    private bool _autoDownloadSteamCmd = true;

    public ObservableCollection<string> Themes { get; } = new()
    {
        "浅色",
        "深色",
        "自动"
    };

    public ObservableCollection<string> Languages { get; } = new()
    {
        "简体中文",
        "English"
    };

    public SettingsViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
        
        // 加载设置
        _ = LoadSettingsAsync();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            // TODO: 从配置服务加载实际设置
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            // TODO: 保存设置到配置服务
            System.Diagnostics.Debug.WriteLine("设置已保存");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 重置设置
    /// </summary>
    [RelayCommand]
    private void ResetSettings()
    {
        Theme = "浅色";
        Language = "简体中文";
        AutoCheckUpdates = true;
        SteamCmdPath = @"C:\CSP2\steamcmd";
        AutoDownloadSteamCmd = true;
    }

    /// <summary>
    /// 浏览SteamCmd路径
    /// </summary>
    [RelayCommand]
    private void BrowseSteamCmdPath()
    {
        // TODO: 显示文件夹选择对话框
        System.Diagnostics.Debug.WriteLine("选择SteamCmd路径");
    }
}

