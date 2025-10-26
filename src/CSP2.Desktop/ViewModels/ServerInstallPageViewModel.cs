using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using CSP2.Core.Services;
using CSP2.Desktop.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using static CSP2.Core.Services.CS2PathDetector;

namespace CSP2.Desktop.ViewModels;

/// <summary>
/// 服务器安装页面ViewModel
/// </summary>
public partial class ServerInstallPageViewModel : ObservableObject
{
    private readonly IServerManager _serverManager;
    private readonly ISteamCmdService _steamCmdService;
    private readonly CS2PathDetector _pathDetector;
    private readonly ILogger<ServerInstallPageViewModel> _logger;
    private readonly JsonLocalizationService _localizationService;
    private readonly Action<Server>? _onInstallComplete;
    private readonly Action? _onCancel;

    [ObservableProperty]
    private int _currentStep = 1; // 1=选择方式, 2=填写信息

    [ObservableProperty]
    private string _selectedMode = ""; // "steamcmd" or "existing"

    [ObservableProperty]
    private ObservableCollection<CS2InstallInfo> _detectedInstallations = new();

    [ObservableProperty]
    private CS2InstallInfo? _selectedInstallation;

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private string _scanMessage = "Scanning...";

    // 服务器基本信息
    [ObservableProperty]
    private string _serverName = "My CS2 Server";

    [ObservableProperty]
    private string _installPath = "";

    // 安装状态
    [ObservableProperty]
    private bool _isInstalling = false;

    public ServerInstallPageViewModel(
        IServerManager serverManager,
        ISteamCmdService steamCmdService,
        CS2PathDetector pathDetector,
        ILogger<ServerInstallPageViewModel> logger,
        JsonLocalizationService localizationService,
        Action<Server>? onInstallComplete = null,
        Action? onCancel = null)
    {
        _serverManager = serverManager;
        _steamCmdService = steamCmdService;
        _pathDetector = pathDetector;
        _logger = logger;
        _localizationService = localizationService;
        _onInstallComplete = onInstallComplete;
        _onCancel = onCancel;
        
        // 初始化默认路径
        InstallPath = $@"C:\CS2Servers\Server{DateTime.Now:yyyyMMdd_HHmmss}";
        
        // 初始化默认服务器名称
        ServerName = _localizationService.GetString("ServerInstall.ServerNamePlaceholder");
        
        // 自动开始扫描
        _ = ScanInstallationsAsync();
    }

    private async Task ScanInstallationsAsync()
    {
        IsScanning = true;
        ScanMessage = _localizationService.GetString("ServerInstall.ScanningExisting");
        
        try
        {
            var installations = await _pathDetector.DetectAllInstallationsAsync();
            DetectedInstallations.Clear();
            
            foreach (var install in installations.Where(i => i.IsValid))
            {
                DetectedInstallations.Add(install);
            }
            
            ScanMessage = DetectedInstallations.Count > 0 
                ? _localizationService.GetString("ServerInstall.DetectedCount", DetectedInstallations.Count)
                : _localizationService.GetString("ServerInstall.NoDetected");
            
            _logger.LogInformation("扫描完成，找到 {Count} 个有效安装", DetectedInstallations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描安装失败");
            ScanMessage = _localizationService.GetString("ServerInstall.ScanFailed");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task SelectSteamCmdModeAsync()
    {
        _logger.LogInformation("用户选择 SteamCMD 下载模式");
        
        // 检查 SteamCMD 是否已安装
        bool isInstalled = await _steamCmdService.IsSteamCmdInstalledAsync();
        _logger.LogInformation("SteamCMD 安装状态: {IsInstalled}", isInstalled);
        
        if (!isInstalled)
        {
            // 提示用户需要先安装 SteamCMD
            var result = System.Windows.MessageBox.Show(
                _localizationService.GetString("ServerInstall.SteamCmdNotInstalledMsg"),
                _localizationService.GetString("ServerInstall.SteamCmdNotInstalled"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);
            
            if (result != System.Windows.MessageBoxResult.Yes)
            {
                _logger.LogInformation("用户取消 SteamCMD 下载");
                return;
            }
            
            _logger.LogInformation("用户确认将安装 SteamCMD");
        }
        
        // 让用户选择下载路径
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择CS2服务器下载路径",
            SelectedPath = InstallPath
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }
        
        InstallPath = dialog.SelectedPath;
        SelectedMode = "steamcmd";
        
        // 直接开始下载
        await StartDownloadOnlyAsync();
    }

    [RelayCommand]
    private void SelectExistingMode()
    {
        if (DetectedInstallations.Count == 0)
        {
            System.Windows.MessageBox.Show(
                _localizationService.GetString("ServerInstall.NoExistingFound"),
                _localizationService.GetString("ServerInstall.NoExistingFoundTitle"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        // 提示用户使用"添加服务器"功能
        var message = "检测到以下CS2服务器安装：\n\n" + 
                      string.Join("\n", DetectedInstallations.Select(i => $"• {i.Source}: {i.InstallPath}")) +
                      "\n\n请返回「服务器管理」页面，点击「➕ 添加服务器」按钮来配置和添加这些服务器。";
        
        System.Windows.MessageBox.Show(
            message,
            "使用现有服务器",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
        
        // 返回服务器管理页面
        _onCancel?.Invoke();
    }

    [RelayCommand]
    private void BackToStepOne()
    {
        CurrentStep = 1;
    }

    [RelayCommand]
    private void BrowseInstallPath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = _localizationService.GetString("Msg.SelectServerInstallDirectory"),
            SelectedPath = InstallPath
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InstallPath = dialog.SelectedPath;
        }
    }

    /// <summary>
    /// 仅下载CS2服务器文件（不创建服务器配置）
    /// </summary>
    private async Task StartDownloadOnlyAsync()
    {
        if (string.IsNullOrWhiteSpace(InstallPath))
        {
            System.Windows.MessageBox.Show(
                "请选择有效的下载路径",
                "验证失败",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsInstalling = true;

        try
        {
            _logger.LogInformation("开始纯下载CS2服务器文件到: {Path}", InstallPath);
            
            // 检查 SteamCMD
            if (!await _steamCmdService.IsSteamCmdInstalledAsync())
            {
                _logger.LogInformation("SteamCMD未安装，开始安装");
                
                var steamCmdPath = _steamCmdService.GetSteamCmdPath();
                if (!await _steamCmdService.InstallSteamCmdAsync(steamCmdPath, null))
                {
                    throw new InvalidOperationException("SteamCMD 安装失败");
                }
            }

            // 下载CS2服务器文件（SteamCmdService会自动将任务添加到DownloadManager）
            if (!await _steamCmdService.InstallOrUpdateServerAsync(InstallPath, false, null))
            {
                throw new InvalidOperationException("CS2 服务器下载失败");
            }

            _logger.LogInformation("CS2服务器文件下载任务已启动");
            
            // 显示成功消息
            System.Windows.MessageBox.Show(
                $"CS2服务器文件下载任务已启动！\n\n下载路径：{InstallPath}\n\n下载完成后，请返回「服务器管理」页面，点击「➕ 添加服务器」按钮来配置和添加服务器。\n\n您可以在「下载管理」页面查看下载进度。",
                "下载已启动",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            
            // 返回服务器管理页面
            _onCancel?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载失败");
            System.Windows.MessageBox.Show(
                $"下载失败：{ex.Message}",
                "下载失败",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    [RelayCommand]
    private async Task StartInstallAsync()
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            System.Windows.MessageBox.Show(
                _localizationService.GetString("ServerInstall.EnterServerName"),
                _localizationService.GetString("ServerInstall.ValidationFailed"), 
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (SelectedMode == "steamcmd" && string.IsNullOrWhiteSpace(InstallPath))
        {
            System.Windows.MessageBox.Show(
                _localizationService.GetString("ServerInstall.SelectDownloadPath"),
                _localizationService.GetString("ServerInstall.ValidationFailed"), 
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (SelectedMode == "existing" && SelectedInstallation == null)
        {
            System.Windows.MessageBox.Show(
                _localizationService.GetString("ServerInstall.SelectExistingInstall"),
                _localizationService.GetString("ServerInstall.ValidationFailed"), 
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsInstalling = true;

        try
        {
            // 使用默认配置
            var config = new ServerConfig
            {
                IpAddress = "0.0.0.0",
                Port = 27015,
                Map = "de_dust2",
                MaxPlayers = 10,
                TickRate = 128,
                ServerName = ServerName
            };

            if (SelectedMode == "steamcmd")
            {
                // SteamCMD下载：先创建占位符Server，立即返回主页面，后台下载
                await StartSteamCmdInstallAsync(config);
            }
            else if (SelectedMode == "existing")
            {
                // 现有安装：直接添加，完成后返回
                var server = await AddExistingServerAsync(config);
                
                if (server != null)
                {
                    _onInstallComplete?.Invoke(server);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "操作失败");
            System.Windows.MessageBox.Show(
                _localizationService.GetString("ServerInstall.OperationFailed", ex.Message),
                _localizationService.GetString("ServerInstall.OperationFailedTitle"),
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            CurrentStep = 2;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    /// <summary>
    /// 开始SteamCMD下载（立即返回，后台下载）
    /// </summary>
    private async Task StartSteamCmdInstallAsync(ServerConfig config)
    {
        _logger.LogInformation("开始SteamCMD下载流程");
        
        // 先创建占位符服务器（不验证路径，因为文件还未下载）
        var server = await _serverManager.AddServerWithoutValidationAsync(ServerName, InstallPath, config);
        server.InstallSource = ServerInstallSource.SteamCmd;
        server.IsManagedByCSP2 = true;
        server.Status = ServerStatus.Stopped; // 初始状态为停止，下载完成后可启动
        await _serverManager.UpdateServerAsync(server);
        
        // 立即返回到主页面
        _onInstallComplete?.Invoke(server);
        
        // 在后台执行下载
        _ = Task.Run(async () =>
        {
            try
            {
                await InstallWithSteamCmdAsync(server.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台下载失败");
            }
        });
    }

    /// <summary>
    /// 实际执行SteamCMD下载（后台任务）
    /// </summary>
    private async Task InstallWithSteamCmdAsync(string serverId)
    {
        _logger.LogInformation("后台下载服务器文件: {ServerId}", serverId);
        
        try
        {
            // 检查 SteamCMD
            if (!await _steamCmdService.IsSteamCmdInstalledAsync())
            {
                _logger.LogInformation("SteamCMD未安装，开始安装");
                
                var steamCmdPath = _steamCmdService.GetSteamCmdPath();
                if (!await _steamCmdService.InstallSteamCmdAsync(steamCmdPath, null))
                {
                    throw new InvalidOperationException("SteamCMD 安装失败");
                }
            }

            // 下载CS2服务器文件（SteamCmdService会自动将任务添加到DownloadManager）
            _logger.LogInformation("开始下载CS2服务器文件到: {Path}", InstallPath);
            
            if (!await _steamCmdService.InstallOrUpdateServerAsync(InstallPath, false, null))
            {
                throw new InvalidOperationException("CS2 服务器下载失败");
            }

            _logger.LogInformation("服务器文件下载完成: {ServerId}", serverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "服务器文件下载失败: {ServerId}", serverId);
            
            // 可以选择更新服务器状态为错误
            // 或者通过事件通知UI
        }
    }

    private async Task<Server?> AddExistingServerAsync(ServerConfig config)
    {
        if (SelectedInstallation == null)
            throw new InvalidOperationException("未选择现有安装");

        _logger.LogInformation("添加现有安装: {Path}", SelectedInstallation.InstallPath);

        var server = await _serverManager.AddServerAsync(ServerName, SelectedInstallation.InstallPath, config);
        
        if (SelectedInstallation.Source.Contains("Steam"))
        {
            server.InstallSource = ServerInstallSource.ExistingSteam;
            server.IsManagedByCSP2 = false;
        }
        else
        {
            server.InstallSource = ServerInstallSource.ExistingLocal;
            server.IsManagedByCSP2 = false;
        }
        
        await _serverManager.UpdateServerAsync(server);

        return server;
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancel?.Invoke();
    }
}

