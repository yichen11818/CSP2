using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 设置页面ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly ISteamCmdService _steamCmdService;
    private readonly ILogger<SettingsViewModel> _logger;

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

    [ObservableProperty]
    private bool _isSteamCmdInstalled = false;

    [ObservableProperty]
    private string _steamCmdStatus = "未检查";

    [ObservableProperty]
    private bool _isInstallingsteamCmd = false;

    [ObservableProperty]
    private double _installProgress = 0;

    [ObservableProperty]
    private string _installMessage = string.Empty;

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

    public SettingsViewModel(
        IConfigurationService configurationService,
        ISteamCmdService steamCmdService,
        ILogger<SettingsViewModel> logger)
    {
        _configurationService = configurationService;
        _steamCmdService = steamCmdService;
        _logger = logger;
        
        // 加载设置
        _ = LoadSettingsAsync();
        
        // 检查 SteamCMD 状态
        _ = CheckSteamCmdStatusAsync();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _configurationService.LoadAppSettingsAsync();
            
            // 加载UI设置
            Theme = settings.Ui?.Theme ?? "浅色";
            Language = settings.Ui?.Language ?? "简体中文";
            AutoCheckUpdates = settings.Ui?.AutoCheckUpdates ?? true;
            
            // 加载SteamCMD设置
            SteamCmdPath = settings.SteamCmd?.InstallPath ?? @"C:\CSP2\steamcmd";
            AutoDownloadSteamCmd = settings.SteamCmd?.AutoDownload ?? true;
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
            var settings = new AppSettings
            {
                Ui = new UiSettings
                {
                    Theme = Theme,
                    Language = Language,
                    AutoCheckUpdates = AutoCheckUpdates
                },
                SteamCmd = new SteamCmdSettings
                {
                    InstallPath = SteamCmdPath,
                    AutoDownload = AutoDownloadSteamCmd
                }
            };
            
            await _configurationService.SaveAppSettingsAsync(settings);
            System.Diagnostics.Debug.WriteLine("设置已保存");
            
            // TODO: 显示成功消息
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            // TODO: 显示错误消息
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
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择 SteamCMD 安装路径",
            InitialDirectory = SteamCmdPath
        };

        if (dialog.ShowDialog() == true)
        {
            SteamCmdPath = dialog.FolderName;
            _ = CheckSteamCmdStatusAsync();
        }
    }

    /// <summary>
    /// 检查 SteamCMD 状态
    /// </summary>
    [RelayCommand]
    private async Task CheckSteamCmdStatusAsync()
    {
        try
        {
            _logger.LogInformation("检查 SteamCMD 状态");
            SteamCmdStatus = "检查中...";
            
            IsSteamCmdInstalled = await _steamCmdService.IsSteamCmdInstalledAsync();
            
            if (IsSteamCmdInstalled)
            {
                var path = _steamCmdService.GetSteamCmdPath();
                SteamCmdStatus = $"✅ 已安装 ({path})";
                _logger.LogInformation("SteamCMD 已安装在: {Path}", path);
            }
            else
            {
                SteamCmdStatus = "❌ 未安装";
                _logger.LogInformation("SteamCMD 未安装");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查 SteamCMD 状态失败");
            SteamCmdStatus = $"检查失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 安装 SteamCMD
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstallSteamCmd))]
    private async Task InstallSteamCmdAsync()
    {
        try
        {
            _logger.LogInformation("开始安装 SteamCMD");
            IsInstallingsteamCmd = true;
            InstallProgress = 0;
            InstallMessage = "准备安装...";

            var progress = new Progress<DownloadProgress>(p =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InstallProgress = p.Percentage;
                    InstallMessage = p.Message;
                });
            });

            var installPath = string.IsNullOrWhiteSpace(SteamCmdPath) 
                ? _steamCmdService.GetSteamCmdPath() 
                : SteamCmdPath;

            var success = await _steamCmdService.InstallSteamCmdAsync(installPath, progress);

            if (success)
            {
                _logger.LogInformation("SteamCMD 安装成功");
                InstallMessage = "✅ 安装成功！";
                await CheckSteamCmdStatusAsync();
                MessageBox.Show("SteamCMD 安装成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _logger.LogError("SteamCMD 安装失败");
                InstallMessage = "❌ 安装失败";
                MessageBox.Show("SteamCMD 安装失败，请查看日志了解详细信息。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装 SteamCMD 时出错");
            InstallMessage = $"❌ 错误: {ex.Message}";
            MessageBox.Show($"安装 SteamCMD 时出错：\n\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsInstallingsteamCmd = false;
        }
    }

    private bool CanInstallSteamCmd() => !IsInstallingsteamCmd && !IsSteamCmdInstalled;
}

