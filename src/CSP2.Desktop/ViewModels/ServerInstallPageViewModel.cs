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
    private int _currentStep = 1; // 1=选择方式, 2=配置服务器, 3=安装中

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

    // 服务器配置
    [ObservableProperty]
    private string _serverName = "我的 CS2 服务器";

    [ObservableProperty]
    private string _installPath = "";

    [ObservableProperty]
    private string _ipAddress = "0.0.0.0";

    [ObservableProperty]
    private int _port = 27015;

    [ObservableProperty]
    private string _map = "de_dust2";

    [ObservableProperty]
    private int _maxPlayers = 10;

    [ObservableProperty]
    private int _tickRate = 128;

    [ObservableProperty]
    private string _mapGroup = "mg_active";

    // 安装进度
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
            System.Windows.MessageBox.Show("请选择安装路径", "验证失败", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (SelectedMode == "existing" && SelectedInstallation == null)
        {
            System.Windows.MessageBox.Show("请选择现有安装", "验证失败", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (Port < 1024 || Port > 65535)
        {
            System.Windows.MessageBox.Show("端口必须在 1024-65535 之间", "验证失败", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        CurrentStep = 3;
        IsInstalling = true;

        try
        {
            var config = new ServerConfig
            {
                IpAddress = IpAddress,
                Port = Port,
                Map = Map,
                MaxPlayers = MaxPlayers,
                TickRate = TickRate,
                MapGroup = MapGroup,
                ServerName = ServerName
            };

            Server? server = null;

            if (SelectedMode == "steamcmd")
            {
                server = await InstallWithSteamCmdAsync(config);
            }
            else if (SelectedMode == "existing")
            {
                server = await AddExistingServerAsync(config);
            }

            if (server != null)
            {
                ProgressIcon = "✅";
                InstallMessage = "安装完成！";
                
                await Task.Delay(1500);
                _onInstallComplete?.Invoke(server);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装服务器失败");
            ProgressIcon = "❌";
            InstallMessage = $"安装失败: {ex.Message}";
            
            await Task.Delay(3000);
            CurrentStep = 2;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private async Task<Server?> InstallWithSteamCmdAsync(ServerConfig config)
    {
        _logger.LogInformation("开始使用SteamCMD安装服务器");
        
        // 检查 SteamCMD
        InstallProgress = 0;
        InstallMessage = "正在检查 SteamCMD...";
        
        if (!await _steamCmdService.IsSteamCmdInstalledAsync())
        {
            var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                System.Windows.MessageBox.Show(
                    "SteamCMD 未安装。是否现在安装？\n\n" +
                    "SteamCMD 是下载和管理 CS2 服务器文件所必需的工具。",
                    "需要 SteamCMD",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question));

            if (result == System.Windows.MessageBoxResult.No)
            {
                throw new InvalidOperationException("用户取消安装 SteamCMD");
            }
            
            InstallMessage = "正在安装 SteamCMD...";
            
            var downloadProgress = new Progress<DownloadProgress>(p =>
            {
                InstallProgress = p.Percentage * 0.3;
                InstallMessage = p.Message;
            });

            var steamCmdPath = _steamCmdService.GetSteamCmdPath();
            if (!await _steamCmdService.InstallSteamCmdAsync(steamCmdPath, downloadProgress))
            {
                throw new InvalidOperationException("SteamCMD 安装失败");
            }
        }

        // 下载CS2服务器
        InstallProgress = 30;
        InstallMessage = "正在下载 CS2 服务器文件（约30GB）...";

        var installProgress = new Progress<InstallProgress>(p =>
        {
            InstallProgress = 30 + (p.Percentage * 0.7);
            InstallMessage = p.Message;
        });

        if (!await _steamCmdService.InstallOrUpdateServerAsync(InstallPath, false, installProgress))
        {
            throw new InvalidOperationException("CS2 服务器下载失败");
        }

        // 添加服务器
        InstallMessage = "正在添加服务器到列表...";
        var server = await _serverManager.AddServerAsync(ServerName, InstallPath, config);
        
        server.InstallSource = ServerInstallSource.SteamCmd;
        server.IsManagedByCSP2 = true;
        await _serverManager.UpdateServerAsync(server);

        InstallProgress = 100;
        return server;
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

