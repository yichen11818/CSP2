using System.Windows;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Forms;
using CSP2.Desktop.ViewModels;
using CSP2.Desktop.Services;
using CSP2.Desktop.Helpers;
using CSP2.Core.Abstractions;
using System;

namespace CSP2.Desktop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private NotifyIcon? _notifyIcon;
    private bool _isRealClosing = false;
    private readonly IConfigurationService _configurationService;
    private readonly JsonLocalizationService _localizationService;

    public MainWindow(MainWindowViewModel viewModel, JsonLocalizationService localizationService, IConfigurationService configurationService)
    {
        _configurationService = configurationService;
        _localizationService = localizationService;
        
        // 注意：LocalizationHelper 已在 App.xaml.cs 的 OnStartup 中初始化
        
        InitializeComponent();
        DataContext = viewModel;
        
        // 初始化系统托盘图标
        InitializeNotifyIcon();
        
        // 监听窗口状态变化
        this.StateChanged += MainWindow_StateChanged;
        
        // 窗口加载完成后检查是否首次运行
        this.Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// 窗口加载完成事件
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // 检查是否首次运行
            var settings = await _configurationService.LoadAppSettingsAsync();
            
            if (settings.Ui.IsFirstRun)
            {
                DebugLogger.Info("MainWindow", "首次运行，播放欢迎烟花动画");
                
                // 延迟一点时间以确保窗口完全显示
                await Task.Delay(300);
                
                // 显示烟花动画
                FireworksAnimation.Visibility = Visibility.Visible;
                FireworksAnimation.Play();
                
                // 动画播放1.5秒后隐藏
                await Task.Delay(1500);
                FireworksAnimation.Visibility = Visibility.Collapsed;
                
                // 标记为已运行
                settings.Ui.IsFirstRun = false;
                await _configurationService.SaveAppSettingsAsync(settings);
                
                DebugLogger.Info("MainWindow", "欢迎动画播放完成");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Error("MainWindow", $"播放欢迎动画失败: {ex.Message}", ex);
            // 动画失败不影响主程序运行
        }
    }

    /// <summary>
    /// 初始化系统托盘图标
    /// </summary>
    private void InitializeNotifyIcon()
    {
        try
        {
            DebugLogger.Debug("MainWindow", "初始化系统托盘图标");
            
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // 使用默认应用图标
                Text = "CSP2 - Counter-Strike 2 Server Panel",
                Visible = true
            };

            // 双击托盘图标显示窗口
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            
            var showMenuItem = new ToolStripMenuItem(_localizationService.GetString("Msg.ShowMainWindow"), null, (s, e) => ShowWindow());
            showMenuItem.Font = new Font(showMenuItem.Font, System.Drawing.FontStyle.Bold);
            contextMenu.Items.Add(showMenuItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            contextMenu.Items.Add(new ToolStripMenuItem(_localizationService.GetString("Msg.Exit"), null, (s, e) => ExitApplication()));

            _notifyIcon.ContextMenuStrip = contextMenu;
            
            DebugLogger.Debug("MainWindow", "系统托盘图标初始化完成");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("MainWindow", $"初始化系统托盘图标失败: {ex.Message}", ex);
            // 托盘图标初始化失败不影响主程序运行，只记录错误
        }
    }

    /// <summary>
    /// 显示窗口
    /// </summary>
    private void ShowWindow()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        _isRealClosing = true;
        _notifyIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// 窗口状态变化事件
    /// </summary>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // 最小化时隐藏窗口
        if (this.WindowState == WindowState.Minimized)
        {
            this.Hide();
            _notifyIcon?.ShowBalloonTip(2000, "CSP2", "程序已最小化到系统托盘", ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// 标题栏/顶部区域拖动事件
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        catch (Exception ex)
        {
            // DragMove 在某些情况下可能会抛出异常（如双击时）
            DebugLogger.Debug("MainWindow", $"窗口拖动异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 最小化按钮点击事件
    /// </summary>
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 触发窗口关闭事件，会执行 OnClosing 中的逻辑
        this.Close();
    }

    /// <summary>
    /// 窗口关闭事件
    /// </summary>
    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isRealClosing)
        {
            try
            {
                // 加载设置
                var settings = await _configurationService.LoadAppSettingsAsync();
                
                // 如果还没有询问过用户，弹窗询问
                if (!settings.Ui.MinimizeToTrayAsked)
                {
                    e.Cancel = true; // 先取消关闭
                    
                    var promptMessage = _localizationService.GetString("Msg.FirstClosePrompt");
                    var promptTitle = _localizationService.GetString("Msg.FirstCloseTitle");
                    
                    var result = System.Windows.MessageBox.Show(
                        promptMessage,
                        promptTitle,
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Cancel)
                    {
                        // 用户取消，不做任何操作
                        return;
                    }
                    
                    // 保存用户的选择
                    settings.Ui.MinimizeToTray = (result == MessageBoxResult.Yes);
                    settings.Ui.MinimizeToTrayAsked = true;
                    await _configurationService.SaveAppSettingsAsync(settings);
                    
                    DebugLogger.Info("MainWindow", $"用户首次关闭选择: {(settings.Ui.MinimizeToTray ? "最小化到托盘" : "退出程序")}");
                }
                
                // 根据设置决定行为
                if (settings.Ui.MinimizeToTray)
                {
                    // 最小化到托盘
                    e.Cancel = true;
                    this.WindowState = WindowState.Minimized;
                }
                else
                {
                    // 退出程序
                    _isRealClosing = true;
                    _notifyIcon?.Dispose();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error("MainWindow", $"处理关闭事件时出错: {ex.Message}", ex);
                // 出错时默认最小化到托盘
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
            }
        }
        else
        {
            // 真正关闭
            _notifyIcon?.Dispose();
            base.OnClosing(e);
        }
    }
}

