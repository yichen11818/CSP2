using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using CSP2.Desktop.Services;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 设置页面ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly ISteamCmdService _steamCmdService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly LocalizationService _localizationService;

    [ObservableProperty]
    private string _theme = "浅色";

    [ObservableProperty]
    private LanguageDisplayInfo? _selectedLanguage;
    
    partial void OnSelectedLanguageChanged(LanguageDisplayInfo? value)
    {
        if (value != null)
        {
            try
            {
                _localizationService.ChangeLanguage(value.Code);
                _logger.LogInformation("语言已切换到: {Language}", value.DisplayName);
                DebugLogger.Info("Settings", $"语言切换成功: {value.DisplayName}");
                
                MessageBox.Show(
                    Resources.Strings.ResourceManager.GetString("Msg_LanguageChanged") ?? "Language changed successfully. Some changes may require restart.",
                    "CSP2",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换语言失败");
                DebugLogger.Error("Settings", $"切换语言失败: {ex.Message}", ex);
                MessageBox.Show($"Language change failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private bool _minimizeToTray = true;

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

    public ObservableCollection<LanguageDisplayInfo> Languages { get; } = new();

    public SettingsViewModel(
        IConfigurationService configurationService,
        ISteamCmdService steamCmdService,
        ILogger<SettingsViewModel> logger,
        LocalizationService localizationService)
    {
        _configurationService = configurationService;
        _steamCmdService = steamCmdService;
        _logger = logger;
        _localizationService = localizationService;
        
        _logger.LogInformation("SettingsViewModel 初始化");
        DebugLogger.Debug("SettingsViewModel", "构造函数开始执行");
        
        // 初始化语言列表
        InitializeLanguages();
        
        // 加载设置
        _ = LoadSettingsAsync();
        
        // 检查 SteamCMD 状态
        _ = CheckSteamCmdStatusAsync();
    }
    
    /// <summary>
    /// 初始化支持的语言
    /// </summary>
    private void InitializeLanguages()
    {
        var supportedLanguages = LocalizationService.GetSupportedLanguages();
        foreach (var lang in supportedLanguages)
        {
            Languages.Add(new LanguageDisplayInfo
            {
                Code = lang.Code,
                DisplayName = $"{lang.Flag} {lang.DisplayName}"
            });
        }
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        _logger.LogInformation("开始加载应用设置");
        DebugLogger.Debug("LoadSettingsAsync", "开始加载设置");
        
        try
        {
            var settings = await _configurationService.LoadAppSettingsAsync();
            
            // 加载UI设置
            Theme = settings.Ui?.Theme ?? "浅色";
            var savedLangCode = _localizationService.CurrentLanguageCode;
            SelectedLanguage = Languages.FirstOrDefault(l => l.Code == savedLangCode) ?? Languages.FirstOrDefault();
            AutoCheckUpdates = settings.Ui?.AutoCheckUpdates ?? true;
            MinimizeToTray = settings.Ui?.MinimizeToTray ?? true;
            
            // 加载SteamCMD设置
            SteamCmdPath = settings.SteamCmd?.InstallPath ?? @"C:\CSP2\steamcmd";
            AutoDownloadSteamCmd = settings.SteamCmd?.AutoDownload ?? true;
            
            _logger.LogInformation("应用设置加载成功");
            DebugLogger.Debug("LoadSettingsAsync", "设置加载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载应用设置失败");
            DebugLogger.Error("LoadSettingsAsync", $"加载设置失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _logger.LogInformation("开始保存应用设置");
        DebugLogger.Debug("SaveSettingsAsync", "保存设置");
        
        try
        {
            var settings = new AppSettings
            {
                Ui = new UiSettings
                {
                    Theme = Theme,
                    Language = SelectedLanguage?.Code ?? "zh-CN",
                    AutoCheckUpdates = AutoCheckUpdates,
                    MinimizeToTray = MinimizeToTray
                },
                SteamCmd = new SteamCmdSettings
                {
                    InstallPath = SteamCmdPath,
                    AutoDownload = AutoDownloadSteamCmd
                }
            };
            
            await _configurationService.SaveAppSettingsAsync(settings);
            _logger.LogInformation("应用设置保存成功");
            DebugLogger.Info("SaveSettingsAsync", "设置已保存");
            
            MessageBox.Show("设置已保存！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存应用设置失败");
            DebugLogger.Error("SaveSettingsAsync", $"保存设置失败: {ex.Message}", ex);
            MessageBox.Show($"保存设置失败：\n\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 重置设置
    /// </summary>
    [RelayCommand]
    private void ResetSettings()
    {
        Theme = "浅色";
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == "zh-CN");
        AutoCheckUpdates = true;
        MinimizeToTray = true;
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

            // 通知命令状态变化
            InstallSteamCmdCommand.NotifyCanExecuteChanged();
            UninstallSteamCmdCommand.NotifyCanExecuteChanged();
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

    /// <summary>
    /// 卸载 SteamCMD
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUninstallSteamCmd))]
    private async Task UninstallSteamCmdAsync()
    {
        try
        {
            // 确认对话框
            var result = MessageBox.Show(
                "确定要卸载 SteamCMD 吗？\n\n这将删除所有 SteamCMD 相关文件。",
                "确认卸载",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _logger.LogInformation("开始卸载 SteamCMD");
            IsInstallingsteamCmd = true;
            InstallProgress = 0;
            InstallMessage = "正在卸载 SteamCMD...";

            var success = await _steamCmdService.UninstallSteamCmdAsync();

            if (success)
            {
                _logger.LogInformation("SteamCMD 卸载成功");
                InstallMessage = "✅ 卸载成功！";
                InstallProgress = 100;
                await CheckSteamCmdStatusAsync();
                MessageBox.Show("SteamCMD 已成功卸载！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _logger.LogError("SteamCMD 卸载失败");
                InstallMessage = "❌ 卸载失败";
                MessageBox.Show("SteamCMD 卸载失败，请查看日志了解详细信息。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载 SteamCMD 时出错");
            InstallMessage = $"❌ 错误: {ex.Message}";
            MessageBox.Show($"卸载 SteamCMD 时出错：\n\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsInstallingsteamCmd = false;
            InstallProgress = 0;
        }
    }

    private bool CanUninstallSteamCmd() => !IsInstallingsteamCmd && IsSteamCmdInstalled;
}

/// <summary>
/// 语言显示信息
/// </summary>
public class LanguageDisplayInfo
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    public override string ToString() => DisplayName;
}

