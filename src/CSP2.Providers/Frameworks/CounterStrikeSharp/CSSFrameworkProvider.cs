using CSP2.Core.Abstractions;
using CSP2.Core.Models;

namespace CSP2.Providers.Frameworks.CounterStrikeSharp;

/// <summary>
/// CounterStrikeSharp框架提供者实现
/// </summary>
public class CSSFrameworkProvider : IFrameworkProvider
{
    private readonly HttpClient _httpClient;
    private const string GithubApiUrl = "https://api.github.com/repos/roflmuffin/CounterStrikeSharp/releases/latest";

    public CSSFrameworkProvider()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CSP2-Server-Panel");
    }

    public ProviderMetadata Metadata => new()
    {
        Id = "counterstrikesharp",
        Name = "CounterStrikeSharp",
        Version = "1.0.0",
        Author = "CSP2 Team",
        Description = "CounterStrikeSharp C#插件框架支持",
        Priority = 90
    };

    public FrameworkInfo FrameworkInfo => new()
    {
        Id = "counterstrikesharp",
        Name = "CounterStrikeSharp",
        ShortName = "CSS",
        Description = "基于C#的CS2插件开发框架",
        Dependencies = new[] { "metamod" },
        InstallPath = "game/csgo/addons/counterstrikesharp",
        PluginPath = "game/csgo/addons/counterstrikesharp/plugins",
        ConfigPath = "game/csgo/addons/counterstrikesharp/configs",
        SupportedPlatforms = new[] { "windows", "linux" },
        RepositoryUrl = "https://github.com/roflmuffin/CounterStrikeSharp",
        DocumentationUrl = "https://docs.cssharp.dev"
    };

    public async Task<bool> IsInstalledAsync(string serverPath)
    {
        var cssPath = Path.Combine(serverPath, "game", "csgo", "addons", "counterstrikesharp");
        return await Task.FromResult(Directory.Exists(cssPath) && 
                                     File.Exists(Path.Combine(cssPath, "CounterStrikeSharp.dll")));
    }

    public async Task<string?> GetInstalledVersionAsync(string serverPath)
    {
        if (!await IsInstalledAsync(serverPath))
        {
            return null;
        }

        // TODO: 实现版本检测逻辑
        return "unknown";
    }

    public async Task<InstallResult> InstallAsync(string serverPath, string? version = null, 
        IProgress<InstallProgress>? progress = null)
    {
        try
        {
            progress?.Report(new InstallProgress
            {
                Percentage = 0,
                CurrentStep = "开始安装CounterStrikeSharp",
                CurrentStepIndex = 1,
                TotalSteps = 3
            });

            // TODO: 实现实际的下载和安装逻辑
            await Task.Delay(1000); // 模拟安装

            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                CurrentStep = "安装完成",
                CurrentStepIndex = 3,
                TotalSteps = 3
            });

            return InstallResult.CreateSuccess("CounterStrikeSharp安装成功");
        }
        catch (Exception ex)
        {
            return InstallResult.CreateFailure($"安装失败: {ex.Message}", ex);
        }
    }

    public async Task<InstallResult> UpdateAsync(string serverPath, IProgress<InstallProgress>? progress = null)
    {
        return await InstallAsync(serverPath, null, progress);
    }

    public async Task<bool> UninstallAsync(string serverPath)
    {
        try
        {
            var cssPath = Path.Combine(serverPath, "game", "csgo", "addons", "counterstrikesharp");
            if (Directory.Exists(cssPath))
            {
                Directory.Delete(cssPath, recursive: true);
            }
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<InstalledPlugin>> ScanInstalledPluginsAsync(string serverPath)
    {
        var plugins = new List<InstalledPlugin>();
        var pluginPath = Path.Combine(serverPath, "game", "csgo", "addons", "counterstrikesharp", "plugins");

        if (!Directory.Exists(pluginPath))
        {
            return plugins;
        }

        // 扫描插件目录
        foreach (var dir in Directory.GetDirectories(pluginPath))
        {
            var dirName = Path.GetFileName(dir);
            var pluginDll = Path.Combine(dir, $"{dirName}.dll");
            
            if (File.Exists(pluginDll))
            {
                plugins.Add(new InstalledPlugin
                {
                    Id = dirName.ToLower(),
                    Name = dirName,
                    Version = "unknown",
                    Framework = "counterstrikesharp",
                    Enabled = true,
                    InstallPath = dir,
                    InstalledAt = Directory.GetCreationTime(dir)
                });
            }
        }

        return await Task.FromResult(plugins);
    }

    public async Task<InstallResult> InstallPluginAsync(string serverPath, PluginInfo pluginInfo, 
        IProgress<InstallProgress>? progress = null)
    {
        try
        {
            progress?.Report(new InstallProgress
            {
                Percentage = 0,
                CurrentStep = $"开始安装插件: {pluginInfo.Name}",
                CurrentStepIndex = 1,
                TotalSteps = 3
            });

            // TODO: 实现实际的插件下载和安装逻辑
            await Task.Delay(500); // 模拟安装

            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                CurrentStep = "安装完成",
                CurrentStepIndex = 3,
                TotalSteps = 3
            });

            return InstallResult.CreateSuccess($"插件 {pluginInfo.Name} 安装成功");
        }
        catch (Exception ex)
        {
            return InstallResult.CreateFailure($"安装失败: {ex.Message}", ex);
        }
    }

    public async Task<bool> UninstallPluginAsync(string serverPath, InstalledPlugin plugin)
    {
        try
        {
            if (Directory.Exists(plugin.InstallPath))
            {
                Directory.Delete(plugin.InstallPath, recursive: true);
            }
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SetPluginEnabledAsync(string serverPath, InstalledPlugin plugin, bool enabled)
    {
        try
        {
            // CSS通过重命名.dll为.disabled来禁用插件
            var dllPath = Path.Combine(plugin.InstallPath, $"{plugin.Name}.dll");
            var disabledPath = Path.Combine(plugin.InstallPath, $"{plugin.Name}.dll.disabled");

            if (enabled)
            {
                if (File.Exists(disabledPath))
                {
                    File.Move(disabledPath, dllPath);
                }
            }
            else
            {
                if (File.Exists(dllPath))
                {
                    File.Move(dllPath, disabledPath);
                }
            }

            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> CheckUpdateAsync(string currentVersion)
    {
        // TODO: 实现版本检查逻辑
        return await Task.FromResult<string?>(null);
    }
}

