using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CSP2.Core.Abstractions;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // 注册平台Provider
                services.AddSingleton<IPlatformProvider, WindowsPlatformProvider>();

                // 注册框架Provider
                services.AddSingleton<IFrameworkProvider, MetamodFrameworkProvider>();
                services.AddSingleton<IFrameworkProvider, CSSFrameworkProvider>();

                // 注册ViewModels
                services.AddTransient<MainWindowViewModel>();

                // 注册Views
                services.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

