using CSP2.Core.Models;
using CSP2.Core.Services;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static CSP2.Core.Services.CS2PathDetector;

namespace CSP2.Desktop.Views.Dialogs;

/// <summary>
/// 服务器安装对话框
/// </summary>
public partial class ServerInstallDialog : Window
{
    private readonly CS2PathDetector _pathDetector;
    private readonly ILogger<ServerInstallDialog> _logger;
    
    private int _currentStep = 1;
    private string _selectedMode = ""; // "steamcmd" or "existing"
    private List<CS2InstallInfo> _detectedInstallations = new();

    public ServerInstallResult? Result { get; private set; }

    public ServerInstallDialog(CS2PathDetector pathDetector, ILogger<ServerInstallDialog> logger)
    {
        _pathDetector = pathDetector;
        _logger = logger;
        
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 扫描现有安装
        try
        {
            DetectedInstallsText.Text = "正在扫描...";
            _detectedInstallations = await _pathDetector.DetectAllInstallationsAsync();
            var validCount = _detectedInstallations.Count(i => i.IsValid);
            
            if (validCount > 0)
            {
                DetectedInstallsText.Text = $"检测到 {validCount} 个可用安装";
            }
            else
            {
                DetectedInstallsText.Text = "未检测到现有安装";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "扫描现有安装失败");
            DetectedInstallsText.Text = "扫描失败";
        }

        // 初始化默认安装路径
        InstallPathTextBox.Text = $@"C:\CS2Servers\Server{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    private void SteamCmdOption_Click(object sender, MouseButtonEventArgs e)
    {
        _selectedMode = "steamcmd";
        GoToStep2();
    }

    private void ExistingInstallOption_Click(object sender, MouseButtonEventArgs e)
    {
        if (_detectedInstallations.Count == 0)
        {
            MessageBox.Show(
                "未检测到任何现有的 CS2 安装。\n\n请选择 SteamCMD 下载方式。",
                "未找到安装",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _selectedMode = "existing";
        GoToStep2();
    }

    private void GoToStep2()
    {
        _currentStep = 2;
        
        Step1Panel.Visibility = Visibility.Collapsed;
        Step2Panel.Visibility = Visibility.Visible;
        Step3Panel.Visibility = Visibility.Collapsed;

        BackButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Collapsed;
        InstallButton.Visibility = Visibility.Visible;

        if (_selectedMode == "steamcmd")
        {
            InstallPathPanel.Visibility = Visibility.Visible;
            ExistingInstallPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            InstallPathPanel.Visibility = Visibility.Collapsed;
            ExistingInstallPanel.Visibility = Visibility.Visible;

            // 填充现有安装下拉框
            ExistingInstallComboBox.Items.Clear();
            foreach (var install in _detectedInstallations.Where(i => i.IsValid))
            {
                var sizeStr = install.InstallSize.HasValue 
                    ? $" ({CS2PathDetector.FormatFileSize(install.InstallSize.Value)})" 
                    : "";
                ExistingInstallComboBox.Items.Add(new ComboBoxItem
                {
                    Content = $"{install.Source} - {install.InstallPath}{sizeStr}",
                    Tag = install
                });
            }

            if (ExistingInstallComboBox.Items.Count > 0)
            {
                ExistingInstallComboBox.SelectedIndex = 0;
            }
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep == 2)
        {
            _currentStep = 1;
            Step1Panel.Visibility = Visibility.Visible;
            Step2Panel.Visibility = Visibility.Collapsed;
            Step3Panel.Visibility = Visibility.Collapsed;

            BackButton.Visibility = Visibility.Collapsed;
            NextButton.Visibility = Visibility.Collapsed;
            InstallButton.Visibility = Visibility.Collapsed;
        }
    }

    private void BrowseInstallPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择服务器安装目录",
            SelectedPath = InstallPathTextBox.Text
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InstallPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证输入
        if (string.IsNullOrWhiteSpace(ServerNameTextBox.Text))
        {
            MessageBox.Show("请输入服务器名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_selectedMode == "steamcmd")
        {
            if (string.IsNullOrWhiteSpace(InstallPathTextBox.Text))
            {
                MessageBox.Show("请选择安装路径", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else if (_selectedMode == "existing")
        {
            if (ExistingInstallComboBox.SelectedItem == null)
            {
                MessageBox.Show("请选择现有安装", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        if (!int.TryParse(PortTextBox.Text, out int port) || port < 1024 || port > 65535)
        {
            MessageBox.Show("端口必须在 1024-65535 之间", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(MaxPlayersTextBox.Text, out int maxPlayers) || maxPlayers < 1 || maxPlayers > 64)
        {
            MessageBox.Show("最大玩家数必须在 1-64 之间", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tickRateItem = TickRateComboBox.SelectedItem as ComboBoxItem;
        if (tickRateItem == null || !int.TryParse(tickRateItem.Content?.ToString(), out int tickRate))
        {
            MessageBox.Show("请选择 Tick Rate", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 准备结果
        var config = new ServerConfig
        {
            IpAddress = IpAddressTextBox.Text,
            Port = port,
            Map = MapTextBox.Text,
            MaxPlayers = maxPlayers,
            TickRate = tickRate,
            MapGroup = MapGroupTextBox.Text,
            ServerName = ServerNameTextBox.Text
        };

        if (_selectedMode == "steamcmd")
        {
            Result = new ServerInstallResult
            {
                Mode = ServerInstallMode.SteamCmd,
                ServerName = ServerNameTextBox.Text,
                InstallPath = InstallPathTextBox.Text,
                Config = config
            };
        }
        else
        {
            var selectedItem = ExistingInstallComboBox.SelectedItem as ComboBoxItem;
            var installInfo = selectedItem?.Tag as CS2InstallInfo;
            
            if (installInfo == null)
            {
                MessageBox.Show("选择的安装无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Result = new ServerInstallResult
            {
                Mode = installInfo.Source.Contains("Steam") 
                    ? ServerInstallMode.ExistingSteam 
                    : ServerInstallMode.ExistingLocal,
                ServerName = ServerNameTextBox.Text,
                InstallPath = installInfo.InstallPath,
                Config = config,
                ExistingInstallInfo = installInfo
            };
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        // 预留用于多步骤向导
    }
}

/// <summary>
/// 服务器安装结果
/// </summary>
public class ServerInstallResult
{
    public required ServerInstallMode Mode { get; set; }
    public required string ServerName { get; set; }
    public required string InstallPath { get; set; }
    public required ServerConfig Config { get; set; }
    public CS2InstallInfo? ExistingInstallInfo { get; set; }
}

/// <summary>
/// 服务器安装模式
/// </summary>
public enum ServerInstallMode
{
    SteamCmd,
    ExistingSteam,
    ExistingLocal
}

