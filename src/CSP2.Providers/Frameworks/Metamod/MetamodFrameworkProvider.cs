using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Providers.Frameworks.Metamod;

/// <summary>
/// Metamod:Source框架提供者实现
/// </summary>
public class MetamodFrameworkProvider : IFrameworkProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MetamodFrameworkProvider>? _logger;
    private const string MetamodDropBaseUrl = "https://mms.alliedmods.net/mmsdrop/2.0/";
    private const string MetamodLatestWindowsInfoUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-latest-windows";
    private const string MetamodLatestLinuxInfoUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-latest-linux";

    public MetamodFrameworkProvider(ILogger<MetamodFrameworkProvider>? logger = null)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
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
        var dllPath = Path.Combine(metaModPath, "bin", "win64", "metamod.dll");
        var soPath = Path.Combine(metaModPath, "bin", "linuxsteamrt64", "metamod.so");
        
        return await Task.FromResult(Directory.Exists(metaModPath) && 
                                     (File.Exists(dllPath) || File.Exists(soPath)));
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
                CurrentStep = "准备安装 Metamod:Source",
                CurrentStepIndex = 1,
                TotalSteps = 5,
                Message = "正在连接到 AlliedModders..."
            });

            // 1. 获取实际的文件名和版本号
            var isWindows = !OperatingSystem.IsLinux();
            var (downloadUrl, fileName, versionStr) = await GetLatestVersionInfoAsync(isWindows);

            if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(fileName))
            {
                _logger?.LogError("[Metamod-Install] 无法获取 Metamod:Source 版本信息");
                return InstallResult.CreateFailure("无法获取 Metamod:Source 版本信息，请检查网络连接");
            }

            _logger?.LogInformation("[Metamod-Install] 找到Metamod:Source版本: {Version}", versionStr);
            _logger?.LogInformation("[Metamod-Install] 下载文件: {FileName}", fileName);
            _logger?.LogInformation("[Metamod-Install] 下载地址: {Url}", downloadUrl);

            progress?.Report(new InstallProgress
            {
                Percentage = 10,
                CurrentStep = $"找到版本: {versionStr}",
                CurrentStepIndex = 1,
                TotalSteps = 5,
                Message = $"准备下载: {fileName}"
            });

            // 2. 下载文件
            var tempDir = Path.Combine(Path.GetTempPath(), "CSP2", "metamod");
            Directory.CreateDirectory(tempDir);
            var archivePath = Path.Combine(tempDir, fileName);

            _logger?.LogInformation("[Metamod-Install] 临时下载路径: {Path}", archivePath);

            progress?.Report(new InstallProgress
            {
                Percentage = 15,
                CurrentStep = "开始下载",
                CurrentStepIndex = 2,
                TotalSteps = 5,
                Message = $"下载文件: {fileName}"
            });

            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
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
                                Percentage = 15 + (downloadPercent * 0.35), // 15-50%
                                CurrentStep = "下载中",
                                CurrentStepIndex = 2,
                                TotalSteps = 5,
                                Message = $"已下载: {totalRead / 1024:F0} KB / {totalBytes / 1024:F0} KB"
                            });
                        }
                    }
                }
            }

            progress?.Report(new InstallProgress
            {
                Percentage = 50,
                CurrentStep = "下载完成",
                CurrentStepIndex = 3,
                TotalSteps = 5,
                Message = "正在解压文件..."
            });

            // 3. 解压到服务器 csgo 目录（ZIP里已包含 addons/ 结构）
            var csgoPath = Path.Combine(serverPath, "game", "csgo");
            var metamodPath = Path.Combine(csgoPath, "addons", "metamod");
            
            _logger?.LogInformation("[Metamod-Install] 安装位置: {MetamodPath}", metamodPath);
            _logger?.LogInformation("[Metamod-Install] 解压目标: {CsgoPath}", csgoPath);
            
            // 如果已存在，先备份
            if (Directory.Exists(metamodPath))
            {
                var backupPath = metamodPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                Directory.Move(metamodPath, backupPath);
                _logger?.LogInformation("[Metamod-Install] 已备份旧版本到: {BackupPath}", backupPath);
            }

            // 解压文件
            int totalFiles = 0;
            if (isWindows)
            {
                // Windows: ZIP 格式 - 直接解压到 csgo 目录
                _logger?.LogInformation("[Metamod-Install] 开始解压文件...");
                
                using (var archive = ZipFile.OpenRead(archivePath))
                {
                    int filesExtracted = 0;
                    totalFiles = archive.Entries.Count;
                    
                    _logger?.LogInformation("[Metamod-Install] ZIP文件包含 {TotalFiles} 个条目", totalFiles);

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
                            if (entry.Name.EndsWith(".dll") || entry.Name.EndsWith(".so") || entry.Name.EndsWith(".vdf"))
                            {
                                _logger?.LogDebug("[Metamod-Install] 解压文件: {EntryFullName} -> {DestinationPath}", entry.FullName, destinationPath);
                            }
                        }
                        
                        filesExtracted++;

                        if (filesExtracted % 5 == 0)
                        {
                            var extractPercent = (double)filesExtracted / totalFiles * 100;
                            progress?.Report(new InstallProgress
                            {
                                Percentage = 50 + (extractPercent * 0.40), // 50-90%
                                CurrentStep = "解压中",
                                CurrentStepIndex = 4,
                                TotalSteps = 5,
                                Message = $"已解压: {filesExtracted} / {totalFiles} 个文件"
                            });
                        }
                    }
                }
                
                _logger?.LogInformation("[Metamod-Install] 解压完成，共 {TotalFiles} 个文件", totalFiles);
                
                // 列出安装的主要文件
                if (Directory.Exists(metamodPath))
                {
                    var binPath = Path.Combine(metamodPath, "bin", "win64", "metamod.dll");
                    if (File.Exists(binPath))
                    {
                        var fileInfo = new FileInfo(binPath);
                        _logger?.LogInformation("[Metamod-Install] 已安装: metamod.dll ({FileSize} KB)", fileInfo.Length / 1024);
                    }
                    
                    // 列出目录结构
                    var subdirs = Directory.GetDirectories(metamodPath);
                    var subdirNames = string.Join(", ", subdirs.Select(Path.GetFileName));
                    _logger?.LogInformation("[Metamod-Install] 创建的子目录: {SubDirs}", subdirNames);
                }
            }
            else
            {
                // Linux: tar.gz 格式
                // 注：这需要 SharpCompress 或系统tar命令
                // 简化处理：使用 tar 命令解压
                _logger?.LogError("[Metamod-Install] Linux 版本暂不支持自动安装");
                return InstallResult.CreateFailure("Linux 版本暂不支持自动安装，请手动安装");
            }

            // 4. 创建 VDF 文件（确保服务器加载 Metamod）
            var gameInfoVdfPath = Path.Combine(serverPath, "game", "csgo", "gameinfo.gi");
            _logger?.LogInformation("[Metamod-Install] 配置 gameinfo.gi 加载 Metamod");
            await EnsureMetamodLoadedAsync(gameInfoVdfPath);

            // 5. 保存版本信息
            await SaveVersionInfoAsync(metamodPath, versionStr);
            _logger?.LogInformation("[Metamod-Install] 版本信息已保存: {Version}", versionStr);

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
                File.Delete(archivePath);
                _logger?.LogInformation("[Metamod-Install] 临时文件已清理");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[Metamod-Install] 清理临时文件失败");
            }

            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                CurrentStep = "安装完成",
                CurrentStepIndex = 5,
                TotalSteps = 5,
                Message = "Metamod:Source 安装成功"
            });

            _logger?.LogInformation("[Metamod-Install] ✓ Metamod:Source {Version} 安装成功！", versionStr);
            return InstallResult.CreateSuccess("Metamod:Source 安装成功");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[Metamod-Install] 安装失败");
            return InstallResult.CreateFailure($"安装失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取最新版本信息（从 AlliedModders 官网）
    /// </summary>
    /// <param name="isWindows">是否为 Windows 平台</param>
    /// <returns>(下载URL, 文件名, 版本号)</returns>
    private async Task<(string downloadUrl, string fileName, string version)> GetLatestVersionInfoAsync(bool isWindows)
    {
        try
        {
            // 1. 访问 latest info URL 获取实际文件名
            var infoUrl = isWindows ? MetamodLatestWindowsInfoUrl : MetamodLatestLinuxInfoUrl;
            
            var response = await _httpClient.GetAsync(infoUrl);
            if (!response.IsSuccessStatusCode)
            {
                return (string.Empty, string.Empty, string.Empty);
            }

            // 2. 读取返回的文件名
            var actualFileName = (await response.Content.ReadAsStringAsync()).Trim();
            
            if (string.IsNullOrEmpty(actualFileName))
            {
                return (string.Empty, string.Empty, string.Empty);
            }

            // 3. 从文件名中提取版本号
            // 格式: mmsource-2.0.0-git1367-windows.zip 或 mmsource-2.0.0-git1367-linux.tar.gz
            var versionMatch = Regex.Match(actualFileName, @"mmsource-([\d\.]+-git\d+)");
            var version = versionMatch.Success ? versionMatch.Groups[1].Value : "latest";

            // 4. 构建完整的下载 URL
            var downloadUrl = MetamodDropBaseUrl + actualFileName;

            return (downloadUrl, actualFileName, version);
        }
        catch
        {
            return (string.Empty, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// 确保 Metamod 被加载（修改 gameinfo.gi）
    /// </summary>
    private async Task EnsureMetamodLoadedAsync(string gameInfoPath)
    {
        try
        {
            if (!File.Exists(gameInfoPath))
            {
                return;
            }

            var content = await File.ReadAllTextAsync(gameInfoPath);
            
            // 检查是否已经添加了 Metamod
            if (content.Contains("metamod") || content.Contains("addons/metamod"))
            {
                return; // 已经配置
            }

            // 添加 Metamod 到 SearchPaths
            // 注：这是一个简化的实现，实际应该解析 VDF 格式
            var metamodPath = "Game\tcsgo/addons/metamod";
            var searchPathsIndex = content.IndexOf("SearchPaths", StringComparison.OrdinalIgnoreCase);
            
            if (searchPathsIndex > 0)
            {
                var insertIndex = content.IndexOf("{", searchPathsIndex);
                if (insertIndex > 0)
                {
                    content = content.Insert(insertIndex + 1, $"\n\t\t\t{metamodPath}");
                    await File.WriteAllTextAsync(gameInfoPath, content);
                }
            }
        }
        catch
        {
            // 忽略错误，用户可能需要手动配置
        }
    }

    /// <summary>
    /// 保存版本信息
    /// </summary>
    private async Task SaveVersionInfoAsync(string metamodPath, string version = "")
    {
        try
        {
            var versionFile = Path.Combine(metamodPath, "version.txt");
            
            // 保存实际的版本号
            if (!string.IsNullOrEmpty(version) && version != "latest")
            {
                await File.WriteAllTextAsync(versionFile, version);
            }
            else
            {
                await File.WriteAllTextAsync(versionFile, $"2.0-latest-{DateTime.Now:yyyyMMdd}");
            }
        }
        catch
        {
            // 忽略
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
        try
        {
            // Metamod 官网没有提供 API，简单返回 null
            // 用户可以通过重新安装来更新
            return await Task.FromResult<string?>(null);
        }
        catch
        {
            return null;
        }
    }
}

