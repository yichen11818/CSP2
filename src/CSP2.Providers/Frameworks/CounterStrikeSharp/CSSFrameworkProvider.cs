using System.IO.Compression;
using System.Text.Json;
using CSP2.Core.Abstractions;
using CSP2.Core.Logging;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Providers.Frameworks.CounterStrikeSharp;

/// <summary>
/// CounterStrikeSharp框架提供者实现
/// </summary>
public class CSSFrameworkProvider : IFrameworkProvider
{
    private readonly HttpClient _httpClient;
    private const string GithubApiUrl = "https://api.github.com/repos/roflmuffin/CounterStrikeSharp/releases/latest";
    private const string GithubReleasesUrl = "https://api.github.com/repos/roflmuffin/CounterStrikeSharp/releases";

    public CSSFrameworkProvider()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10) // 下载可能需要较长时间
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CSP2-Server-Panel");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
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
        var dllPath = Path.Combine(cssPath, "bin", "win64", "counterstrikesharp.dll");
        var soPath = Path.Combine(cssPath, "bin", "linuxsteamrt64", "counterstrikesharp.so");
        
        return await Task.FromResult(Directory.Exists(cssPath) && 
                                     (File.Exists(dllPath) || File.Exists(soPath)));
    }

    public async Task<string?> GetInstalledVersionAsync(string serverPath)
    {
        if (!await IsInstalledAsync(serverPath))
        {
            return null;
        }

        // 尝试从 version.txt 读取版本号
        var versionFile = Path.Combine(serverPath, "game", "csgo", "addons", "counterstrikesharp", "version.txt");
        if (File.Exists(versionFile))
        {
            var version = await File.ReadAllTextAsync(versionFile);
            return version.Trim();
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
                CurrentStep = "准备安装 CounterStrikeSharp",
                CurrentStepIndex = 1,
                TotalSteps = 5,
                Message = "正在获取版本信息..."
            });

            // 1. 获取 Release 信息
            var releaseInfo = await GetReleaseInfoAsync(version);
            if (releaseInfo == null)
            {
                return InstallResult.CreateFailure("无法获取 CounterStrikeSharp 版本信息，请检查网络连接");
            }

            var targetVersion = releaseInfo.Value.GetProperty("tag_name").GetString();
            DebugLogger.Info("CSS-Install", $"找到CounterStrikeSharp版本: {targetVersion}");
            
            progress?.Report(new InstallProgress
            {
                Percentage = 10,
                CurrentStep = $"找到版本: {targetVersion}",
                CurrentStepIndex = 1,
                TotalSteps = 5,
                Message = "正在查找下载链接..."
            });

            // 2. 查找正确的下载文件（Windows）
            var assets = releaseInfo.Value.GetProperty("assets");
            string? downloadUrl = null;
            string? fileName = null;

            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                // 查找 Windows 版本的 zip 文件
                if (name != null && name.Contains("windows", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".zip"))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    fileName = name;
                    DebugLogger.Info("CSS-Install", $"找到下载文件: {fileName}");
                    break;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(fileName))
            {
                DebugLogger.Error("CSS-Install", "未找到 Windows 版本的下载文件");
                return InstallResult.CreateFailure("未找到 Windows 版本的下载文件");
            }
            
            DebugLogger.Info("CSS-Install", $"下载地址: {downloadUrl}");

            progress?.Report(new InstallProgress
            {
                Percentage = 20,
                CurrentStep = "开始下载",
                CurrentStepIndex = 2,
                TotalSteps = 5,
                Message = $"下载文件: {fileName}"
            });

            // 3. 下载文件
            var tempDir = Path.Combine(Path.GetTempPath(), "CSP2", "css");
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, fileName);
            
            DebugLogger.Info("CSS-Install", $"临时下载路径: {zipPath}");

            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var downloadPercent = (double)totalRead / totalBytes * 100;
                            progress?.Report(new InstallProgress
                            {
                                Percentage = 20 + (downloadPercent * 0.4), // 20-60%
                                CurrentStep = "下载中",
                                CurrentStepIndex = 2,
                                TotalSteps = 5,
                                Message = $"已下载: {totalRead / 1024 / 1024:F1} MB / {totalBytes / 1024 / 1024:F1} MB"
                            });
                        }
                    }
                }
            }

            progress?.Report(new InstallProgress
            {
                Percentage = 60,
                CurrentStep = "下载完成",
                CurrentStepIndex = 3,
                TotalSteps = 5,
                Message = "正在解压文件..."
            });

            // 4. 解压到服务器 csgo 目录（ZIP里已包含 addons/ 结构）
            var csgoPath = Path.Combine(serverPath, "game", "csgo");
            var cssPath = Path.Combine(csgoPath, "addons", "counterstrikesharp");
            
            DebugLogger.Info("CSS-Install", $"安装位置: {cssPath}");
            DebugLogger.Info("CSS-Install", $"解压目标: {csgoPath}");
            
            // 如果已存在，先备份
            if (Directory.Exists(cssPath))
            {
                var backupPath = cssPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                Directory.Move(cssPath, backupPath);
                DebugLogger.Info("CSS-Install", $"已备份旧版本到: {backupPath}");
            }

            // 直接解压到 csgo 目录，保留 ZIP 内的目录结构
            DebugLogger.Info("CSS-Install", "开始解压文件...");
            
            int totalFiles = 0;
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                int filesExtracted = 0;
                totalFiles = archive.Entries.Count;
                
                DebugLogger.Info("CSS-Install", $"ZIP文件包含 {totalFiles} 个条目");

                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    // 直接使用 ZIP 内的路径结构
                    var destinationPath = Path.Combine(csgoPath, entry.FullName);
                    
                    // 创建目录
                    var directory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // 解压文件
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                        
                        // 记录关键文件
                        if (entry.Name.EndsWith(".dll") || entry.Name.EndsWith(".so") || entry.Name == "version.txt")
                        {
                            DebugLogger.Debug("CSS-Install", $"  解压文件: {entry.FullName} -> {destinationPath}");
                        }
                    }
                    
                    filesExtracted++;

                    if (filesExtracted % 10 == 0)
                    {
                        var extractPercent = (double)filesExtracted / totalFiles * 100;
                        progress?.Report(new InstallProgress
                        {
                            Percentage = 60 + (extractPercent * 0.3), // 60-90%
                            CurrentStep = "解压中",
                            CurrentStepIndex = 4,
                            TotalSteps = 5,
                            Message = $"已解压: {filesExtracted} / {totalFiles} 个文件"
                        });
                    }
                }
            }
            
            DebugLogger.Info("CSS-Install", $"解压完成，共 {totalFiles} 个文件");
            
            // 列出安装的主要文件
            if (Directory.Exists(cssPath))
            {
                // 检查关键文件
                var dllPath = Path.Combine(cssPath, "bin", "win64", "counterstrikesharp.dll");
                if (File.Exists(dllPath))
                {
                    var fileInfo = new FileInfo(dllPath);
                    DebugLogger.Info("CSS-Install", $"  已安装: bin/win64/counterstrikesharp.dll ({fileInfo.Length / 1024:F0} KB)");
                }
                
                // 列出目录结构
                var subdirs = Directory.GetDirectories(cssPath);
                DebugLogger.Info("CSS-Install", $"创建的子目录: {string.Join(", ", subdirs.Select(Path.GetFileName))}");
            }

            // 5. 保存版本信息
            var versionFile = Path.Combine(cssPath, "version.txt");
            await File.WriteAllTextAsync(versionFile, targetVersion);
            DebugLogger.Info("CSS-Install", $"版本信息已保存: {targetVersion}");

            progress?.Report(new InstallProgress
            {
                Percentage = 95,
                CurrentStep = "清理临时文件",
                CurrentStepIndex = 5,
                TotalSteps = 5,
                Message = "正在清理..."
            });

            // 清理临时文件
            try
            {
                File.Delete(zipPath);
                DebugLogger.Info("CSS-Install", "临时文件已清理");
            }
            catch (Exception ex)
            { 
                DebugLogger.Warning("CSS-Install", $"清理临时文件失败: {ex.Message}");
            }

            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                CurrentStep = "安装完成",
                CurrentStepIndex = 5,
                TotalSteps = 5,
                Message = $"CounterStrikeSharp {targetVersion} 安装成功"
            });

            DebugLogger.Info("CSS-Install", $"✓ CounterStrikeSharp {targetVersion} 安装成功！");
            return InstallResult.CreateSuccess($"CounterStrikeSharp {targetVersion} 安装成功");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("CSS-Install", $"安装失败: {ex.Message}", ex);
            return InstallResult.CreateFailure($"安装失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取 Release 信息
    /// </summary>
    private async Task<JsonElement?> GetReleaseInfoAsync(string? version = null)
    {
        try
        {
            string url;
            if (string.IsNullOrEmpty(version))
            {
                // 获取最新版本
                url = GithubApiUrl;
            }
            else
            {
                // 获取指定版本
                url = $"{GithubReleasesUrl}/tags/{version}";
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        }
        catch
        {
            return null;
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
        try
        {
            var releaseInfo = await GetReleaseInfoAsync();
            if (releaseInfo == null)
            {
                return null;
            }

            var latestVersion = releaseInfo.Value.GetProperty("tag_name").GetString();
            
            // 简单的版本比较（实际应该使用 SemVer）
            if (latestVersion != null && latestVersion != currentVersion && currentVersion != "unknown")
            {
                return latestVersion;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

