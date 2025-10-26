using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly JsonLocalizationService _localizationService;
    private readonly ThemeService _themeService;
    private readonly ApplicationRestartService _restartService;

    [ObservableProperty]
    private string _theme = "Light";
    
    partial void OnThemeChanged(string value)
    {
        if (_themeService != null)
        {
            _themeService.ApplyTheme(value);
            
            if (!_isLoadingSettings)
            {
                _ = SaveThemeSettingAsync(value);
                
                // 询问是否重启以完全应用主题
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // 短暂延迟，让设置保存完成
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var mainWindow = Application.Current.MainWindow;
                            _restartService.ShowRestartConfirmation(mainWindow, "Theme");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "显示重启确认对话框失败");
                        }
                    });
                });
            }
        }
    }

    private bool _isLoadingSettings = false;

    [ObservableProperty]
    private LanguageDisplayInfo? _selectedLanguage;
    
    partial void OnSelectedLanguageChanged(LanguageDisplayInfo? value)
    {
        // 加载设置时不触发语言切换
        if (_isLoadingSettings || value == null)
        {
            return;
        }

        // 检查语言是否真的改变了
        if (value.Code == _localizationService.CurrentLanguageCode)
        {
            return;
        }

        try
        {
            _localizationService.ChangeLanguage(value.Code);
            _logger.LogInformation("语言已切换到: {Language}", value.DisplayName);
            DebugLogger.Info("Settings", $"语言切换成功: {value.DisplayName}");
            
            // 自动保存语言设置
            _ = SaveLanguageSettingAsync(value.Code);
            
            // 询问是否重启以完全应用语言设置
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // 短暂延迟，让设置保存完成
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var mainWindow = Application.Current.MainWindow;
                        _restartService.ShowRestartConfirmation(mainWindow, "Language");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "显示重启确认对话框失败");
                    }
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换语言失败");
            DebugLogger.Error("Settings", $"切换语言失败: {ex.Message}", ex);
            MessageBox.Show($"Language change failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 保存语言设置
    /// </summary>
    private async Task SaveLanguageSettingAsync(string languageCode)
    {
        try
        {
            var settings = await _configurationService.LoadAppSettingsAsync();
            settings.Ui.Language = languageCode;
            await _configurationService.SaveAppSettingsAsync(settings);
            _logger.LogInformation("语言设置已自动保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存语言设置失败");
        }
    }

    /// <summary>
    /// 保存主题设置
    /// </summary>
    private async Task SaveThemeSettingAsync(string theme)
    {
        try
        {
            var settings = await _configurationService.LoadAppSettingsAsync();
            settings.Ui.Theme = theme;
            await _configurationService.SaveAppSettingsAsync(settings);
            _logger.LogInformation("主题设置已自动保存: {Theme}", theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存主题设置失败");
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
    private string _steamCmdStatus = "Not Checked";

    [ObservableProperty]
    private bool _isInstallingsteamCmd = false;

    [ObservableProperty]
    private double _installProgress = 0;

    [ObservableProperty]
    private string _installMessage = string.Empty;

    public ObservableCollection<string> Themes { get; } = new()
    {
        "Light",
        "Dark",
        "Auto"
    };

    public ObservableCollection<LanguageDisplayInfo> Languages { get; } = new();

    public SettingsViewModel(
        IConfigurationService configurationService,
        ISteamCmdService steamCmdService,
        ILogger<SettingsViewModel> logger,
        JsonLocalizationService localizationService,
        ThemeService themeService,
        ApplicationRestartService restartService)
    {
        _configurationService = configurationService;
        _steamCmdService = steamCmdService;
        _logger = logger;
        _localizationService = localizationService;
        _themeService = themeService;
        _restartService = restartService;
        
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
        var supportedLanguages = JsonLocalizationService.GetSupportedLanguages();
        foreach (var lang in supportedLanguages)
        {
            Languages.Add(new LanguageDisplayInfo
            {
                Code = lang.Code,
                DisplayName = $"{lang.Flag} {lang.DisplayName}"
            });
        }
        _logger.LogInformation("已初始化 {Count} 种语言", Languages.Count);
        DebugLogger.Debug("InitializeLanguages", $"语言列表: {string.Join(", ", Languages.Select(l => l.Code))}");
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
            _isLoadingSettings = true;
            
            var settings = await _configurationService.LoadAppSettingsAsync();
            
            // 加载UI设置
            Theme = settings.Ui?.Theme ?? "Light";
            var savedLangCode = _localizationService.CurrentLanguageCode;
            _logger.LogInformation("尝试加载语言设置: {LangCode}, 语言列表数量: {Count}", savedLangCode, Languages.Count);
            
            SelectedLanguage = Languages.FirstOrDefault(l => l.Code == savedLangCode) ?? Languages.FirstOrDefault();
            
            _logger.LogInformation("已设置 SelectedLanguage: {DisplayName} ({Code})", 
                SelectedLanguage?.DisplayName ?? "null", 
                SelectedLanguage?.Code ?? "null");
            
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
        finally
        {
            _isLoadingSettings = false;
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
            // 先加载现有设置，避免丢失其他字段
            var settings = await _configurationService.LoadAppSettingsAsync();
            
            // 更新需要修改的字段
            settings.Ui.Theme = Theme;
            settings.Ui.Language = SelectedLanguage?.Code ?? "zh-CN";
            settings.Ui.AutoCheckUpdates = AutoCheckUpdates;
            settings.Ui.MinimizeToTray = MinimizeToTray;
            
            settings.SteamCmd.InstallPath = SteamCmdPath;
            settings.SteamCmd.AutoDownload = AutoDownloadSteamCmd;
            
            await _configurationService.SaveAppSettingsAsync(settings);
            _logger.LogInformation("应用设置保存成功");
            DebugLogger.Info("SaveSettingsAsync", "设置已保存");
            
            MessageBox.Show(
                _localizationService.GetString("Msg.SettingsSaved"),
                _localizationService.GetString("Common.OK"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存应用设置失败");
            DebugLogger.Error("SaveSettingsAsync", $"保存设置失败: {ex.Message}", ex);
            MessageBox.Show($"{_localizationService.GetString("Msg.SaveFailed")}\n\n{ex.Message}", _localizationService.GetString("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 重置设置
    /// </summary>
    [RelayCommand]
    private void ResetSettings()
    {
        Theme = "Light";
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
            Title = _localizationService.GetString("Msg.SelectSteamCmdPath"),
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
            SteamCmdStatus = _localizationService.GetString("Msg.CheckingStatus");
            
            IsSteamCmdInstalled = await _steamCmdService.IsSteamCmdInstalledAsync();
            
            if (IsSteamCmdInstalled)
            {
                var path = _steamCmdService.GetSteamCmdPath();
                SteamCmdStatus = _localizationService.GetString("Msg.SteamCmdInstalled", path);
                _logger.LogInformation("SteamCMD 已安装在: {Path}", path);
            }
            else
            {
                SteamCmdStatus = _localizationService.GetString("Msg.SteamCmdNotInstalled");
                _logger.LogInformation("SteamCMD 未安装");
            }

            // 通知命令状态变化
            InstallSteamCmdCommand.NotifyCanExecuteChanged();
            UninstallSteamCmdCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查 SteamCMD 状态失败");
            SteamCmdStatus = _localizationService.GetString("Msg.CheckStatusFailed", ex.Message);
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
            InstallMessage = _localizationService.GetString("Msg.InstallingPrep");

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
                InstallMessage = _localizationService.GetString("Msg.InstallSuccess");
                await CheckSteamCmdStatusAsync();
                MessageBox.Show(
                    _localizationService.GetString("Msg.SteamCmdInstallSuccess"),
                    _localizationService.GetString("Common.OK"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                _logger.LogError("SteamCMD 安装失败");
                InstallMessage = _localizationService.GetString("Msg.InstallFailed");
                MessageBox.Show(
                    _localizationService.GetString("Msg.SteamCmdInstallFailedMsg"),
                    _localizationService.GetString("Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装 SteamCMD 时出错");
            InstallMessage = $"❌ {_localizationService.GetString("Error.Title")}: {ex.Message}";
            MessageBox.Show(
                _localizationService.GetString("Msg.InstallError", ex.Message),
                _localizationService.GetString("Error.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
                _localizationService.GetString("Msg.UninstallConfirm"),
                _localizationService.GetString("Msg.ConfirmUninstall"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _logger.LogInformation("开始卸载 SteamCMD");
            IsInstallingsteamCmd = true;
            InstallProgress = 0;
            InstallMessage = _localizationService.GetString("Msg.UninstallingSteamCmd");

            var success = await _steamCmdService.UninstallSteamCmdAsync();

            if (success)
            {
                _logger.LogInformation("SteamCMD 卸载成功");
                InstallMessage = _localizationService.GetString("Msg.UninstallSuccess");
                InstallProgress = 100;
                await CheckSteamCmdStatusAsync();
                MessageBox.Show(
                    _localizationService.GetString("Msg.SteamCmdUninstallSuccess"),
                    _localizationService.GetString("Common.OK"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                _logger.LogError("SteamCMD 卸载失败");
                InstallMessage = _localizationService.GetString("Msg.UninstallFailed");
                MessageBox.Show(
                    _localizationService.GetString("Msg.SteamCmdUninstallFailedMsg"),
                    _localizationService.GetString("Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载 SteamCMD 时出错");
            InstallMessage = $"❌ {_localizationService.GetString("Error.Title")}: {ex.Message}";
            MessageBox.Show(
                _localizationService.GetString("Msg.UninstallError", ex.Message),
                _localizationService.GetString("Error.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

