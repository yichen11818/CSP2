using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using CSP2.Core.Abstractions;
using CSP2.Core.Logging;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Providers.Frameworks.Metamod;

/// <summary>
/// Metamod:Source框架提供者实现
/// </summary>
public class MetamodFrameworkProvider : IFrameworkProvider
{
    private readonly HttpClient _httpClient;
    private readonly IDownloadManager? _downloadManager;
    private const string MetamodDropBaseUrl = "https://mms.alliedmods.net/mmsdrop/2.0/";
    private const string MetamodLatestWindowsInfoUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-latest-windows";
    private const string MetamodLatestLinuxInfoUrl = "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-latest-linux";

    public MetamodFrameworkProvider(IDownloadManager? downloadManager = null)
    {
        _downloadManager = downloadManager;
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
        
        // Windows: 检查两个关键文件
        var metamodBgtDll = Path.Combine(metaModPath, "bin", "metamod.2.bgt.dll");
        var serverDll = Path.Combine(metaModPath, "bin", "win64", "server.dll");
        
        // Linux: 检查 .so 文件
        var soPath = Path.Combine(metaModPath, "bin", "linuxsteamrt64", "metamod.so");
        
        var dirExists = Directory.Exists(metaModPath);
        var metamodBgtExists = File.Exists(metamodBgtDll);
        var serverDllExists = File.Exists(serverDll);
        var soExists = File.Exists(soPath);
        
        DebugLogger.Debug("Metamod-Check", $"检查路径: {metaModPath}");
        DebugLogger.Debug("Metamod-Check", $"目录存在: {dirExists}");
        DebugLogger.Debug("Metamod-Check", $"metamod.2.bgt.dll: {metamodBgtExists}");
        DebugLogger.Debug("Metamod-Check", $"server.dll: {serverDllExists}");
        DebugLogger.Debug("Metamod-Check", $"metamod.so: {soExists}");
        
        // Windows: 需要两个文件都存在
        // Linux: 只需要 .so 文件存在
        var windowsInstalled = metamodBgtExists && serverDllExists;
        var linuxInstalled = soExists;
        
        return await Task.FromResult(dirExists && (windowsInstalled || linuxInstalled));
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
        // 创建下载任务
        string? downloadTaskId = null;
        DownloadTask? downloadTask = null;
        
        if (_downloadManager != null)
        {
            downloadTask = new DownloadTask
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Metamod:Source",
                Description = "CS2 插件加载器基础框架",
                TaskType = DownloadTaskType.Framework,
                Status = DownloadTaskStatus.Pending,
                Progress = 0
            };
            downloadTaskId = downloadTask.Id;
            _downloadManager.AddTask(downloadTask);
            _downloadManager.UpdateTaskStatus(downloadTaskId, DownloadTaskStatus.Downloading, null);
        }
        
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
            
            _downloadManager?.UpdateTaskProgress(downloadTaskId!, 0, "正在连接到 AlliedModders...");

            // 1. 获取实际的文件名和版本号
            var isWindows = !OperatingSystem.IsLinux();
            var (downloadUrl, fileName, versionStr) = await GetLatestVersionInfoAsync(isWindows);

            if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(fileName))
            {
                DebugLogger.Error("Metamod-Install", "无法获取 Metamod:Source 版本信息");
                _downloadManager?.UpdateTaskStatus(downloadTaskId!, DownloadTaskStatus.Failed, 
                    "无法获取 Metamod:Source 版本信息，请检查网络连接");
                return InstallResult.CreateFailure("无法获取 Metamod:Source 版本信息，请检查网络连接");
            }

            DebugLogger.Info("Metamod-Install", $"找到Metamod:Source版本: {versionStr}");
            DebugLogger.Info("Metamod-Install", $"下载文件: {fileName}");
            DebugLogger.Info("Metamod-Install", $"下载地址: {downloadUrl}");

            progress?.Report(new InstallProgress
            {
                Percentage = 10,
                CurrentStep = $"找到版本: {versionStr}",
                CurrentStepIndex = 1,
                TotalSteps = 5,
                Message = $"准备下载: {fileName}"
            });
            
            _downloadManager?.UpdateTaskProgress(downloadTaskId!, 10, $"找到版本: {versionStr}");

            // 2. 下载文件
            var tempDir = Path.Combine(Path.GetTempPath(), "CSP2", "metamod");
            Directory.CreateDirectory(tempDir);
            var archivePath = Path.Combine(tempDir, fileName);

            DebugLogger.Info("Metamod-Install", $"临时下载路径: {archivePath}");

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
                
                // 更新下载任务的总大小
                if (_downloadManager != null && downloadTask != null)
                {
                    downloadTask.TotalSize = totalBytes;
                }
                
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
                            var overallPercent = 15 + (downloadPercent * 0.35); // 15-50%
                            
                            progress?.Report(new InstallProgress
                            {
                                Percentage = overallPercent,
                                CurrentStep = "下载中",
                                CurrentStepIndex = 2,
                                TotalSteps = 5,
                                Message = $"已下载: {totalRead / 1024:F0} KB / {totalBytes / 1024:F0} KB"
                            });
                            
                            // 更新下载管理器进度
                            if (_downloadManager != null && downloadTask != null)
                            {
                                downloadTask.DownloadedSize = totalRead;
                                _downloadManager.UpdateTaskProgress(downloadTaskId!, overallPercent, 
                                    $"下载中: {totalRead / 1024:F0} KB / {totalBytes / 1024:F0} KB");
                            }
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
            
            _downloadManager?.UpdateTaskProgress(downloadTaskId!, 50, "下载完成，正在解压...");

            // 3. 解压到服务器 csgo 目录（ZIP里已包含 addons/ 结构）
            var csgoPath = Path.Combine(serverPath, "game", "csgo");
            var metamodPath = Path.Combine(csgoPath, "addons", "metamod");
            
            DebugLogger.Info("Metamod-Install", $"安装位置: {metamodPath}");
            DebugLogger.Info("Metamod-Install", $"解压目标: {csgoPath}");
            
            // 如果已存在，先备份
            if (Directory.Exists(metamodPath))
            {
                var backupPath = metamodPath + ".backup." + DateTime.Now.ToString("yyyyMMddHHmmss");
                Directory.Move(metamodPath, backupPath);
                DebugLogger.Info("Metamod-Install", $"已备份旧版本到: {backupPath}");
            }

            // 解压文件
            int totalFiles = 0;
            if (isWindows)
            {
                // Windows: ZIP 格式 - 直接解压到 csgo 目录
                DebugLogger.Info("Metamod-Install", "开始解压文件...");
                
                using (var archive = ZipFile.OpenRead(archivePath))
                {
                    int filesExtracted = 0;
                    totalFiles = archive.Entries.Count;
                    
                    DebugLogger.Info("Metamod-Install", $"ZIP文件包含 {totalFiles} 个条目");

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
                            if (entry.Name.Contains("metamod.2.bgt.dll") || 
                                entry.Name == "server.dll" || 
                                entry.Name.EndsWith(".so") || 
                                entry.Name.EndsWith(".vdf"))
                            {
                                DebugLogger.Debug("Metamod-Install", $"  解压文件: {entry.FullName} -> {destinationPath}");
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
                
                DebugLogger.Info("Metamod-Install", $"解压完成，共 {totalFiles} 个文件");
                
                // 列出安装的主要文件
                if (Directory.Exists(metamodPath))
                {
                    // 检查关键文件
                    var metamodBgtDll = Path.Combine(metamodPath, "bin", "metamod.2.bgt.dll");
                    if (File.Exists(metamodBgtDll))
                    {
                        var fileInfo = new FileInfo(metamodBgtDll);
                        DebugLogger.Info("Metamod-Install", $"  已安装: bin/metamod.2.bgt.dll ({fileInfo.Length / 1024:F0} KB)");
                    }
                    
                    var serverDll = Path.Combine(metamodPath, "bin", "win64", "server.dll");
                    if (File.Exists(serverDll))
                    {
                        var fileInfo = new FileInfo(serverDll);
                        DebugLogger.Info("Metamod-Install", $"  已安装: bin/win64/server.dll ({fileInfo.Length / 1024:F0} KB)");
                    }
                    
                    // 列出目录结构
                    var subdirs = Directory.GetDirectories(metamodPath);
                    DebugLogger.Info("Metamod-Install", $"创建的子目录: {string.Join(", ", subdirs.Select(Path.GetFileName))}");
                }
            }
            else
            {
                // Linux: tar.gz 格式
                // 注：这需要 SharpCompress 或系统tar命令
                // 简化处理：使用 tar 命令解压
                DebugLogger.Error("Metamod-Install", "Linux 版本暂不支持自动安装");
                return InstallResult.CreateFailure("Linux 版本暂不支持自动安装，请手动安装");
            }

            // 4. 创建 VDF 文件（确保服务器加载 Metamod）
            var gameInfoVdfPath = Path.Combine(serverPath, "game", "csgo", "gameinfo.gi");
            DebugLogger.Info("Metamod-Install", "配置 gameinfo.gi 加载 Metamod");
            await EnsureMetamodLoadedAsync(gameInfoVdfPath);

            // 5. 保存版本信息
            await SaveVersionInfoAsync(metamodPath, versionStr);
            DebugLogger.Info("Metamod-Install", $"版本信息已保存: {versionStr}");

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
                DebugLogger.Info("Metamod-Install", "临时文件已清理");
            }
            catch (Exception ex)
            {
                DebugLogger.Warning("Metamod-Install", $"清理临时文件失败: {ex.Message}");
            }

            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                CurrentStep = "安装完成",
                CurrentStepIndex = 5,
                TotalSteps = 5,
                Message = "Metamod:Source 安装成功"
            });

            DebugLogger.Info("Metamod-Install", $"✓ Metamod:Source {versionStr} 安装成功！");
            
            // 标记下载任务为完成
            _downloadManager?.UpdateTaskStatus(downloadTaskId!, DownloadTaskStatus.Completed, null);
            
            return InstallResult.CreateSuccess("Metamod:Source 安装成功");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("Metamod-Install", $"安装失败: {ex.Message}", ex);
            
            // 标记下载任务为失败
            _downloadManager?.UpdateTaskStatus(downloadTaskId!, DownloadTaskStatus.Failed, $"安装失败: {ex.Message}");
            
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

