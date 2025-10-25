using System.IO.Compression;
using System.Text.Json;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;

namespace CSP2.Providers.Frameworks.Metamod;

/// <summary>
/// Metamod:Source框架提供者实现
/// </summary>
public class MetamodFrameworkProvider : IFrameworkProvider
{
    private readonly HttpClient _httpClient;
    private const string GithubApiUrl = "https://api.github.com/repos/alliedmodders/metamod-source/releases/latest";
    private const string GithubDownloadUrl = "https://mms.alliedmods.net/mmsdrop/";

    public MetamodFrameworkProvider()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CSP2-Server-Panel");
    }

    public ProviderMetadata Metadata => new()
    {
        Id = "metamod",
        Name = "Metamod:Source",
        Version = "1.0.0",
        Author = "CSP2 Team",
        Description = "Metamod:Source框架支持",
        Priority = 100
    };

    public FrameworkInfo FrameworkInfo => new()
    {
        Id = "metamod",
        Name = "Metamod:Source",
        ShortName = "MM:S",
        Description = "CS2服务器插件加载器基础框架",
        Dependencies = Array.Empty<string>(),
        InstallPath = "game/csgo/addons/metamod",
        PluginPath = "game/csgo/addons/metamod/plugins",
        ConfigPath = "game/csgo/addons/metamod/config",
        SupportedPlatforms = new[] { "windows", "linux" },
        RepositoryUrl = "https://github.com/alliedmodders/metamod-source",
        DocumentationUrl = "https://wiki.alliedmods.net/Metamod:Source"
    };

    public async Task<bool> IsInstalledAsync(string serverPath)
    {
        var metaModPath = Path.Combine(serverPath, "game", "csgo", "addons", "metamod");
        return await Task.FromResult(Directory.Exists(metaModPath) && 
                                     File.Exists(Path.Combine(metaModPath, "bin", "win64", "metamod.dll")));
    }

    public async Task<string?> GetInstalledVersionAsync(string serverPath)
    {
        if (!await IsInstalledAsync(serverPath))
        {
            return null;
        }

        // 尝试从version.txt读取版本号
        var versionFile = Path.Combine(serverPath, "game", "csgo", "addons", "metamod", "version.txt");
        if (File.Exists(versionFile))
        {
            return await File.ReadAllTextAsync(versionFile);
        }

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
                CurrentStep = "开始安装Metamod:Source",
                CurrentStepIndex = 1,
                TotalSteps = 3
            });

            // TODO: 实现从GitHub下载最新版本的逻辑
            // 这里先返回一个占位符结果
            progress?.Report(new InstallProgress
            {
                Percentage = 33,
                CurrentStep = "下载Metamod:Source...",
                CurrentStepIndex = 2,
                TotalSteps = 3
            });

            await Task.Delay(1000); // 模拟下载

            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                CurrentStep = "安装完成",
                CurrentStepIndex = 3,
                TotalSteps = 3
            });

            return InstallResult.CreateSuccess("Metamod:Source安装成功");
        }
        catch (Exception ex)
        {
            return InstallResult.CreateFailure($"安装失败: {ex.Message}", ex);
        }
    }

    public async Task<InstallResult> UpdateAsync(string serverPath, IProgress<InstallProgress>? progress = null)
    {
        // 更新就是重新安装
        return await InstallAsync(serverPath, null, progress);
    }

    public async Task<bool> UninstallAsync(string serverPath)
    {
        try
        {
            var metaModPath = Path.Combine(serverPath, "game", "csgo", "addons", "metamod");
            if (Directory.Exists(metaModPath))
            {
                Directory.Delete(metaModPath, recursive: true);
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
        // Metamod自身不管理插件，返回空列表
        return await Task.FromResult(new List<InstalledPlugin>());
    }

    public async Task<InstallResult> InstallPluginAsync(string serverPath, PluginInfo pluginInfo, 
        IProgress<InstallProgress>? progress = null)
    {
        // Metamod自身不管理插件
        return await Task.FromResult(
            InstallResult.CreateFailure("Metamod不直接管理插件，请使用具体的插件框架（如CounterStrikeSharp）"));
    }

    public async Task<bool> UninstallPluginAsync(string serverPath, InstalledPlugin plugin)
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> SetPluginEnabledAsync(string serverPath, InstalledPlugin plugin, bool enabled)
    {
        return await Task.FromResult(false);
    }

    public async Task<string?> CheckUpdateAsync(string currentVersion)
    {
        // TODO: 实现版本检查逻辑
        return await Task.FromResult<string?>(null);
    }
}

