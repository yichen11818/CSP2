using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using CSP2.Core.Services;
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
    private readonly Action<Server>? _onInstallComplete;
    private readonly Action? _onCancel;

    [ObservableProperty]
    private int _currentStep = 1; // 1=选择方式, 2=填写信息, 3=下载中

    [ObservableProperty]
    private string _selectedMode = ""; // "steamcmd" or "existing"

    [ObservableProperty]
    private ObservableCollection<CS2InstallInfo> _detectedInstallations = new();

    [ObservableProperty]
    private CS2InstallInfo? _selectedInstallation;

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private string _scanMessage = "正在扫描...";

    // 服务器基本信息
    [ObservableProperty]
    private string _serverName = "我的 CS2 服务器";

    [ObservableProperty]
    private string _installPath = "";

    // 下载进度
    [ObservableProperty]
    private bool _isInstalling = false;

    [ObservableProperty]
    private double _installProgress = 0;

    [ObservableProperty]
    private string _installMessage = "";

    [ObservableProperty]
    private string _progressIcon = "⬇️";

    public ServerInstallPageViewModel(
        IServerManager serverManager,
        ISteamCmdService steamCmdService,
        CS2PathDetector pathDetector,
        ILogger<ServerInstallPageViewModel> logger,
        Action<Server>? onInstallComplete = null,
        Action? onCancel = null)
    {
        _serverManager = serverManager;
        _steamCmdService = steamCmdService;
        _pathDetector = pathDetector;
        _logger = logger;
        _onInstallComplete = onInstallComplete;
        _onCancel = onCancel;
        
        // 初始化默认路径
        InstallPath = $@"C:\CS2Servers\Server{DateTime.Now:yyyyMMdd_HHmmss}";
        
        // 自动开始扫描
        _ = ScanInstallationsAsync();
    }

    private async Task ScanInstallationsAsync()
    {
        IsScanning = true;
        ScanMessage = "正在扫描现有CS2安装...";
        
        try
        {
            var installations = await _pathDetector.DetectAllInstallationsAsync();
            DetectedInstallations.Clear();
            
            foreach (var install in installations.Where(i => i.IsValid))
            {
                DetectedInstallations.Add(install);
            }
            
            ScanMessage = DetectedInstallations.Count > 0 
                ? $"检测到 {DetectedInstallations.Count} 个可用安装" 
                : "未检测到现有安装";
            
            _logger.LogInformation("扫描完成，找到 {Count} 个有效安装", DetectedInstallations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描安装失败");
            ScanMessage = "扫描失败";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void SelectSteamCmdMode()
    {
        SelectedMode = "steamcmd";
        CurrentStep = 2;
    }

    [RelayCommand]
    private void SelectExistingMode()
    {
        if (DetectedInstallations.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "未检测到任何现有的 CS2 安装。\n\n请选择 SteamCMD 下载方式。",
                "未找到安装",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        SelectedMode = "existing";
        SelectedInstallation = DetectedInstallations.FirstOrDefault();
        CurrentStep = 2;
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
            Description = "选择服务器安装目录",
            SelectedPath = InstallPath
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InstallPath = dialog.SelectedPath;
        }
    }

    [RelayCommand]
    private async Task StartInstallAsync()
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(ServerName))
        {
            System.Windows.MessageBox.Show("请输入服务器名称", "验证失败", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (SelectedMode == "steamcmd" && string.IsNullOrWhiteSpace(InstallPath))
        {
            System.Windows.MessageBox.Show("请选择下载路径", "验证失败", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (SelectedMode == "existing" && SelectedInstallation == null)
        {
            System.Windows.MessageBox.Show("请选择现有安装", "验证失败", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
                MapGroup = "mg_active",
                ServerName = ServerName
            };

            if (SelectedMode == "steamcmd")
            {
                // SteamCMD下载：先创建占位符Server，立即返回主页面，后台下载
                await StartSteamCmdInstallAsync(config);
            }
            else if (SelectedMode == "existing")
            {
                // 现有安装：同步添加，完成后返回
                CurrentStep = 3;
                var server = await AddExistingServerAsync(config);
                
                if (server != null)
                {
                    ProgressIcon = "✅";
                    InstallMessage = "添加完成！";
                    
                    await Task.Delay(1500);
                    _onInstallComplete?.Invoke(server);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "操作失败");
            System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        
        InstallProgress = 50;
        InstallMessage = "正在验证CS2安装...";

        await Task.Delay(500);

        InstallProgress = 80;
        InstallMessage = "正在添加服务器到列表...";

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

        InstallProgress = 100;
        return server;
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancel?.Invoke();
    }
}

