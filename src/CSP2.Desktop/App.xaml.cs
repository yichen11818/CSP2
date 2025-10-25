using System.Windows;
using System.IO;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using CSP2.Core.Abstractions;
using CSP2.Core.Services;
using CSP2.Providers.Platforms.Windows;
using CSP2.Providers.Frameworks.Metamod;
using CSP2.Providers.Frameworks.CounterStrikeSharp;
using CSP2.Desktop.Views;
using CSP2.Desktop.ViewModels;
using CSP2.Desktop.Services;

namespace CSP2.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private static Mutex? _mutex;
    private const string MutexName = "CSP2_SingleInstance_Mutex";

    public App()
    {
        // 捕获所有未处理的异常
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            
            // 记录到日志系统
            Log.Fatal(ex, "未处理的异常 (AppDomain)");
            ViewModels.DebugLogger.Error("UnhandledException", $"严重错误: {ex?.Message}", ex);
            
            // 使用新的错误对话框显示详细信息
            Dispatcher.Invoke(() =>
            {
                Views.ErrorDialog.Show(
                    errorMessage: ex?.Message ?? "未知错误",
                    exception: ex,
                    subtitle: "严重错误 - 应用程序域异常"
                );
            });
        };

        DispatcherUnhandledException += (s, e) =>
        {
            // 记录到日志系统
            Log.Error(e.Exception, "UI线程未处理的异常");
            ViewModels.DebugLogger.Error("DispatcherException", $"UI线程异常: {e.Exception.Message}", e.Exception);
            
            // 使用新的错误对话框显示详细信息
            Views.ErrorDialog.Show(
                errorMessage: e.Exception.Message,
                exception: e.Exception,
                subtitle: "操作已停止 - UI线程异常"
            );
            
            // 标记为已处理，防止程序崩溃退出
            e.Handled = true;
        };

        // 捕获Task中未观察到的异常
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            // 记录到日志系统
            Log.Error(e.Exception, "Task中未观察到的异常");
            ViewModels.DebugLogger.Error("UnobservedTaskException", $"异步任务异常: {e.Exception.Message}", e.Exception);
            
            // 标记异常已处理，防止程序崩溃
            e.SetObserved();
            
            // 在UI线程显示错误对话框
            Dispatcher.BeginInvoke(() =>
            {
                Views.ErrorDialog.Show(
                    errorMessage: e.Exception.GetBaseException().Message,
                    exception: e.Exception.GetBaseException(),
                    subtitle: "后台任务异常"
                );
            });
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 检查单实例
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            var msg = "CSP2 已经在运行中！\n\n请检查系统托盘图标。";
            try
            {
                msg = CSP2.Desktop.Resources.Strings.ResourceManager.GetString("Msg_AlreadyRunning") ?? msg;
            }
            catch { }
            
            MessageBox.Show(msg, "CSP2", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // 检查是否为Debug模式
        var isDebugMode = e.Args.Contains("--debug") || 
                         Environment.GetEnvironmentVariable("CSP2_DEBUG") == "true";
        ViewModels.DebugLogger.IsDebugMode = isDebugMode;

        // 配置Serilog日志
        var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);

        var logLevel = isDebugMode ? LogEventLevel.Debug : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logsDirectory, "csp2-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10 * 1024 * 1024) // 10MB
            .CreateLogger();

        Log.Information("=== CSP2 面板启动 ===");
        Log.Information("运行模式: {Mode}", isDebugMode ? "DEBUG" : "RELEASE");
        Log.Information("日志目录: {LogsDirectory}", logsDirectory);
        
        if (isDebugMode)
        {
            ViewModels.DebugLogger.Info("Startup", "Debug模式已启用 - 详细日志记录已开启");
        }

        try
        {
            _host = Host.CreateDefaultBuilder()
                .UseSerilog() // 使用Serilog
                .ConfigureServices((context, services) =>
                {
                    // 注册ProviderRegistry并初始化
                    services.AddSingleton(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<ProviderRegistry>>();
                        var registry = new ProviderRegistry(logger);
                        
                        // 注册平台Provider
                        var windowsProvider = new WindowsPlatformProvider();
                        registry.RegisterPlatformProvider(windowsProvider);
                        
                        // 注册框架Provider
                        var metamodProvider = new MetamodFrameworkProvider();
                        var cssProvider = new CSSFrameworkProvider();
                        registry.RegisterFrameworkProvider(metamodProvider);
                        registry.RegisterFrameworkProvider(cssProvider);
                        
                        return registry;
                    });

                    // 注册HttpClient
                    services.AddHttpClient();

                    // 注册本地化服务
                    services.AddSingleton<LocalizationService>();

                    // 注册核心服务
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddSingleton<IDownloadManager, DownloadManager>();
                    services.AddSingleton<ISteamCmdService, SteamCmdService>();
                    services.AddSingleton<IServerManager, ServerManager>();
                    services.AddSingleton<IPluginRepositoryService, PluginRepositoryService>();
                    services.AddSingleton<IPluginManager, PluginManager>();
                    services.AddSingleton<CS2PathDetector>();

                    // 注册ViewModels
                    services.AddSingleton<MainWindowViewModel>(); // 改为单例以保持下载状态
                    services.AddTransient<DownloadManagerViewModel>();
                    services.AddTransient<ServerManagementViewModel>();
                    services.AddTransient<LogConsoleViewModel>();
                    services.AddTransient<PluginMarketViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddSingleton<DebugConsoleViewModel>(); // Debug控制台使用单例

                    // 注册Views
                    services.AddTransient<MainWindow>();
                    services.AddTransient<Views.Pages.ServerManagementPage>();
                    services.AddTransient<Views.Pages.LogConsolePage>();
                    services.AddTransient<Views.Pages.PluginMarketPage>();
                    services.AddTransient<Views.Pages.DownloadManagerPage>();
                    services.AddTransient<Views.Pages.SettingsPage>();
                    services.AddTransient<Views.Pages.DebugConsolePage>();
                })
                .Build();

            await _host.StartAsync();
            Log.Information("Host服务启动成功");

            // 初始化本地化服务
            var localization = _host.Services.GetRequiredService<LocalizationService>();
            Log.Information("本地化服务已初始化，当前语言: {Language}", localization.CurrentLanguageCode);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Log.Information("主窗口已显示");
            
            Log.Information("✅ CSP2面板启动成功！");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "❌ CSP2启动失败！");
            var errorMsg = $"程序启动失败！\n\n错误信息：{ex.Message}\n\n详细：{ex}";
            var errorTitle = "CSP2启动错误";
            
            try
            {
                var msgTemplate = CSP2.Desktop.Resources.Strings.ResourceManager.GetString("Msg_StartupFailed");
                if (msgTemplate != null)
                {
                    errorMsg = string.Format(msgTemplate, ex.Message, ex.ToString());
                }
                errorTitle = CSP2.Desktop.Resources.Strings.ResourceManager.GetString("Msg_StartupError") ?? errorTitle;
            }
            catch { }
            
            MessageBox.Show(errorMsg, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("=== CSP2 面板正在退出 ===");
        
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
            Log.Information("Host服务已停止");
        }

        // 释放互斥锁
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();

        Log.Information("=== CSP2 面板已退出 ===");
        Log.CloseAndFlush(); // 确保所有日志都被写入
        
        base.OnExit(e);
    }
}

