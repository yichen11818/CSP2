using System.Windows;
using System.IO;
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

namespace CSP2.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        // 捕获所有未处理的异常
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"未处理的异常！\n\n{ex?.Message}\n\n{ex?.StackTrace}", 
                "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, e) =>
        {
            MessageBox.Show($"UI线程异常！\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}", 
                "UI错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 配置Serilog日志
        var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
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
        Log.Information("日志目录: {LogsDirectory}", logsDirectory);

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

                    // 注册核心服务
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddSingleton<ISteamCmdService, SteamCmdService>();
                    services.AddSingleton<IServerManager, ServerManager>();
                    services.AddSingleton<IPluginRepositoryService, PluginRepositoryService>();
                    services.AddSingleton<IPluginManager, PluginManager>();

                    // 注册ViewModels
                    services.AddTransient<MainWindowViewModel>();
                    services.AddTransient<ServerManagementViewModel>();
                    services.AddTransient<LogConsoleViewModel>();
                    services.AddTransient<PluginMarketViewModel>();
                    services.AddTransient<SettingsViewModel>();

                    // 注册Views
                    services.AddTransient<MainWindow>();
                    services.AddTransient<Views.Pages.ServerManagementPage>();
                    services.AddTransient<Views.Pages.LogConsolePage>();
                    services.AddTransient<Views.Pages.PluginMarketPage>();
                    services.AddTransient<Views.Pages.SettingsPage>();
                })
                .Build();

            await _host.StartAsync();
            Log.Information("Host服务启动成功");

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            Log.Information("主窗口已显示");
            
            Log.Information("✅ CSP2面板启动成功！");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "❌ CSP2启动失败！");
            MessageBox.Show($"程序启动失败！\n\n错误信息：{ex.Message}\n\n详细：{ex}", 
                "CSP2启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        Log.Information("=== CSP2 面板已退出 ===");
        Log.CloseAndFlush(); // 确保所有日志都被写入
        
        base.OnExit(e);
    }
}

