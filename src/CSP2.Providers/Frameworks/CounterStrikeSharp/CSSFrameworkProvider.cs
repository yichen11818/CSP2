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
    private readonly IDownloadManager? _downloadManager;
    private const string GithubApiUrl = "https://api.github.com/repos/roflmuffin/CounterStrikeSharp/releases/latest";
    private const string GithubReleasesUrl = "https://api.github.com/repos/roflmuffin/CounterStrikeSharp/releases";

    public CSSFrameworkProvider(IDownloadManager? downloadManager = null)
    {
        _downloadManager = downloadManager;
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
        
        var dirExists = Directory.Exists(cssPath);
        var dllExists = File.Exists(dllPath);
        var soExists = File.Exists(soPath);
        
        DebugLogger.Debug("CSS-Check", $"检查路径: {cssPath}");
        DebugLogger.Debug("CSS-Check", $"目录存在: {dirExists}, DLL存在: {dllExists}, SO存在: {soExists}");
        
        return await Task.FromResult(dirExists && (dllExists || soExists));
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
        // 创建下载任务
        string? downloadTaskId = null;
        DownloadTask? downloadTask = null;
        
        if (_downloadManager != null)
        {
            downloadTask = new DownloadTask
            {
                Id = Guid.NewGuid().ToString(),
                Name = "CounterStrikeSharp",
                Description = "基于C#的CS2插件开发框架",
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
                CurrentStep = "准备安装 CounterStrikeSharp",
                CurrentStepIndex = 1,
                TotalSteps = 5,
                Message = "正在获取版本信息..."
            });
            
            _downloadManager?.UpdateTaskProgress(downloadTaskId!, 0, "正在获取版本信息...");

            // 1. 获取 Release 信息
            var releaseInfo = await GetReleaseInfoAsync(version);
            if (releaseInfo == null)
            {
                _downloadManager?.UpdateTaskStatus(downloadTaskId!, DownloadTaskStatus.Failed,
                    "无法获取 CounterStrikeSharp 版本信息，请检查网络连接");
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
            
            _downloadManager?.UpdateTaskProgress(downloadTaskId!, 10, $"找到版本: {targetVersion}");

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
                _downloadManager?.UpdateTaskStatus(downloadTaskId!, DownloadTaskStatus.Failed,
                    "未找到 Windows 版本的下载文件");
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
                
                // 更新下载任务的总大小
                if (_downloadManager != null && downloadTask != null)
                {
                    downloadTask.TotalSize = totalBytes;
                }
                
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
                            var overallPercent = 20 + (downloadPercent * 0.4); // 20-60%
                            
                            progress?.Report(new InstallProgress
                            {
                                Percentage = overallPercent,
                                CurrentStep = "下载中",
                                CurrentStepIndex = 2,
                                TotalSteps = 5,
                                Message = $"已下载: {totalRead / 1024 / 1024:F1} MB / {totalBytes / 1024 / 1024:F1} MB"
                            });
                            
                            // 更新下载管理器进度
                            if (_downloadManager != null && downloadTask != null)
                            {
                                downloadTask.DownloadedSize = totalRead;
                                _downloadManager.UpdateTaskProgress(downloadTaskId!, overallPercent,
                                    $"下载中: {totalRead / 1024 / 1024:F1} MB / {totalBytes / 1024 / 1024:F1} MB");
                            }
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
            int metamodFilesCount = 0; // 统计放到 metamod 目录的文件
            
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
                    
                    // 检测是否是 metamod 目录下的文件
                    var isMetamodFile = entry.FullName.StartsWith("addons/metamod/", StringComparison.OrdinalIgnoreCase) ||
                                       entry.FullName.StartsWith("addons\\metamod\\", StringComparison.OrdinalIgnoreCase);
                    
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
                        
                        // 统计 metamod 文件
                        if (isMetamodFile)
                        {
                            metamodFilesCount++;
                            DebugLogger.Info("CSS-Install", $"  添加 Metamod 插件: {entry.FullName}");
                        }
                        
                        // 记录关键文件
                        if (!isMetamodFile && (entry.Name.EndsWith(".dll") || entry.Name.EndsWith(".so") || entry.Name == "version.txt"))
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
            
            // 如果有文件放到了 metamod 目录，记录日志
            if (metamodFilesCount > 0)
            {
                DebugLogger.Info("CSS-Install", $"✓ 已添加 {metamodFilesCount} 个文件到 Metamod 目录（CSS 作为 Metamod 插件运行）");
            }
            
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
            
            // 标记下载任务为完成
            _downloadManager?.UpdateTaskStatus(downloadTaskId!, DownloadTaskStatus.Completed, null);
            
            return InstallResult.CreateSuccess($"CounterStrikeSharp {targetVersion} 安装成功");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("CSS-Install", $"安装失败: {ex.Message}", ex);
            
            // 标记下载任务为失败
            _downloadManager?.UpdateTaskStatus(downloadTaskId!, DownloadTaskStatus.Failed, $"安装失败: {ex.Message}");
            
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
        string? downloadTaskId = null;
        string? zipPath = null;

        try
        {
            DebugLogger.Info("Plugin-Install", $"开始安装插件: {pluginInfo.Name} (ID: {pluginInfo.Id})");

            // 步骤 1: 验证下载信息
            progress?.Report(new InstallProgress
            {
                Percentage = 0,
                CurrentStep = "准备下载",
                CurrentStepIndex = 1,
                TotalSteps = 4,
                Message = $"正在准备安装 {pluginInfo.Name}..."
            });

            if (pluginInfo.Download == null || string.IsNullOrEmpty(pluginInfo.DownloadUrl))
            {
                return InstallResult.CreateFailure("缺少下载信息");
            }

            // 步骤 2: 下载插件
            progress?.Report(new InstallProgress
            {
                Percentage = 10,
                CurrentStep = "下载插件",
                CurrentStepIndex = 2,
                TotalSteps = 4,
                Message = "正在从 GitHub 下载插件包..."
            });

            var tempDir = Path.Combine(Path.GetTempPath(), "csp2_plugin_downloads");
            Directory.CreateDirectory(tempDir);
            zipPath = Path.Combine(tempDir, $"{pluginInfo.Id}_{Guid.NewGuid()}.zip");

            DebugLogger.Info("Plugin-Install", $"下载 URL: {pluginInfo.DownloadUrl}");
            DebugLogger.Info("Plugin-Install", $"临时文件: {zipPath}");

            // 使用 DownloadManager 创建显示任务（如果可用）
            if (_downloadManager != null)
            {
                downloadTaskId = Guid.NewGuid().ToString();
                var downloadTask = new DownloadTask
                {
                    Id = downloadTaskId,
                    Name = $"插件: {pluginInfo.Name}",
                    Description = $"从 GitHub 下载插件 {pluginInfo.Name}",
                    TaskType = DownloadTaskType.Plugin,
                    TotalSize = pluginInfo.DownloadSize,
                    Status = DownloadTaskStatus.Downloading
                };

                _downloadManager.AddTask(downloadTask);
            }

            // 直接使用 HttpClient 下载
            using var response = await _httpClient.GetAsync(pluginInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            using var fileStream = File.Create(zipPath);
            using var httpStream = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[8192];
            long downloadedBytes = 0;
            int bytesRead;

            while ((bytesRead = await httpStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                // 更新 DownloadManager 任务进度（如果可用）
                if (_downloadManager != null && downloadTaskId != null)
                {
                    var percentage = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;
                    _downloadManager.UpdateTaskProgress(downloadTaskId, percentage, 
                        $"已下载: {downloadedBytes / 1024 / 1024:F1} MB / {totalBytes / 1024 / 1024:F1} MB");
                }

                // 报告进度
                if (totalBytes > 0)
                {
                    var percentage = (double)downloadedBytes / totalBytes * 100;
                    progress?.Report(new InstallProgress
                    {
                        Percentage = 10 + (percentage * 0.4), // 10-50%
                        CurrentStep = "下载插件",
                        CurrentStepIndex = 2,
                        TotalSteps = 4,
                        Message = $"已下载: {downloadedBytes / 1024 / 1024:F1} MB / {totalBytes / 1024 / 1024:F1} MB"
                    });
                }
            }

            DebugLogger.Info("Plugin-Install", $"下载完成，文件大小: {new FileInfo(zipPath).Length / 1024 / 1024:F1} MB");

            // 步骤 3: 解压和安装
            progress?.Report(new InstallProgress
            {
                Percentage = 50,
                CurrentStep = "解压插件",
                CurrentStepIndex = 3,
                TotalSteps = 4,
                Message = "正在解压插件文件..."
            });

            var cssPath = Path.Combine(serverPath, "game", "csgo", "addons", "counterstrikesharp");
            if (!Directory.Exists(cssPath))
            {
                return InstallResult.CreateFailure("CounterStrikeSharp 框架未安装");
            }

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var totalEntries = archive.Entries.Count;
                DebugLogger.Info("Plugin-Install", $"ZIP 文件包含 {totalEntries} 个条目");

                // 检查是否使用高级映射配置
                if (pluginInfo.Installation?.Mappings != null && pluginInfo.Installation.Mappings.Length > 0)
                {
                    DebugLogger.Info("Plugin-Install", $"使用高级文件映射配置（{pluginInfo.Installation.Mappings.Length} 个映射规则）");
                    await InstallPluginWithMappings(serverPath, archive, pluginInfo.Installation.Mappings, progress);
                }
                else
                {
                    // 简单安装模式
                    DebugLogger.Info("Plugin-Install", "使用简单安装模式");
                    await InstallPluginSimple(serverPath, archive, pluginInfo.Installation, progress);
                }
            }

            // 步骤 4: 完成
            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                CurrentStep = "安装完成",
                CurrentStepIndex = 4,
                TotalSteps = 4,
                Message = $"插件 {pluginInfo.Name} 安装成功"
            });

            DebugLogger.Info("Plugin-Install", $"✓ 插件 {pluginInfo.Name} 安装成功");

            // 清理下载文件
            try
            {
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                    DebugLogger.Info("Plugin-Install", "临时文件已清理");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Warning("Plugin-Install", $"清理临时文件失败: {ex.Message}");
            }

            if (downloadTaskId != null)
            {
                _downloadManager?.UpdateTaskStatus(downloadTaskId, DownloadTaskStatus.Completed, null);
            }

            return InstallResult.CreateSuccess($"插件 {pluginInfo.Name} 安装成功");
        }
        catch (Exception ex)
        {
            DebugLogger.Error("Plugin-Install", $"安装插件失败: {ex.Message}", ex);

            // 清理临时文件
            if (!string.IsNullOrEmpty(zipPath) && File.Exists(zipPath))
            {
                try { File.Delete(zipPath); } catch { }
            }

            if (downloadTaskId != null)
            {
                _downloadManager?.UpdateTaskStatus(downloadTaskId, DownloadTaskStatus.Failed, ex.Message);
            }

            return InstallResult.CreateFailure($"安装失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 使用高级文件映射规则安装插件
    /// </summary>
    private async Task InstallPluginWithMappings(string serverPath, ZipArchive archive, 
        FileMapping[] mappings, IProgress<InstallProgress>? progress)
    {
        var totalEntries = archive.Entries.Count;
        var processedEntries = 0;

        foreach (var mapping in mappings)
        {
            DebugLogger.Info("Plugin-Install", $"处理映射: {mapping.Source} -> {mapping.Target}");

            var matchingEntries = archive.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name) && MatchesPattern(e.FullName, mapping.Source))
                .ToList();

            DebugLogger.Info("Plugin-Install", $"  匹配到 {matchingEntries.Count} 个文件");

            foreach (var entry in matchingEntries)
            {
                // 计算目标路径
                var relativePath = GetRelativePath(entry.FullName, mapping.Source);
                var targetPath = Path.Combine(serverPath, mapping.Target, relativePath);

                // 创建目录
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 检查是否被排除
                if (mapping.Exclude != null && mapping.Exclude.Any(pattern => MatchesPattern(relativePath, pattern)))
                {
                    DebugLogger.Debug("Plugin-Install", $"  跳过（已排除）: {entry.FullName}");
                    continue;
                }

                // 解压文件
                entry.ExtractToFile(targetPath, overwrite: true);
                DebugLogger.Debug("Plugin-Install", $"  提取: {entry.FullName} -> {relativePath}");

                processedEntries++;

                if (processedEntries % 10 == 0)
                {
                    var percentage = (double)processedEntries / totalEntries * 100;
                    progress?.Report(new InstallProgress
                    {
                        Percentage = 50 + (percentage * 0.4), // 50-90%
                        CurrentStep = "解压插件",
                        CurrentStepIndex = 3,
                        TotalSteps = 4,
                        Message = $"已解压: {processedEntries} / {totalEntries} 个文件"
                    });
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 简单模式安装插件
    /// </summary>
    private async Task InstallPluginSimple(string serverPath, ZipArchive archive, 
        InstallationInfo? installation, IProgress<InstallProgress>? progress)
    {
        var targetPath = installation?.TargetPath ?? "game/csgo/addons/counterstrikesharp/plugins";
        var filePatterns = installation?.Files ?? new[] { "*" };

        var fullTargetPath = Path.Combine(serverPath, targetPath);
        Directory.CreateDirectory(fullTargetPath);

        var totalEntries = archive.Entries.Count;
        var processedEntries = 0;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            // 检查文件是否匹配模式
            if (!filePatterns.Any(pattern => MatchesPattern(entry.FullName, pattern)))
                continue;

            var destinationPath = Path.Combine(fullTargetPath, entry.FullName);
            var directory = Path.GetDirectoryName(destinationPath);
            
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            entry.ExtractToFile(destinationPath, overwrite: true);

            processedEntries++;

            if (processedEntries % 10 == 0)
            {
                var percentage = (double)processedEntries / totalEntries * 100;
                progress?.Report(new InstallProgress
                {
                    Percentage = 50 + (percentage * 0.4), // 50-90%
                    CurrentStep = "解压插件",
                    CurrentStepIndex = 3,
                    TotalSteps = 4,
                    Message = $"已解压: {processedEntries} 个文件"
                });
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 匹配文件路径模式（支持 * 和 ** 通配符）
    /// </summary>
    private bool MatchesPattern(string path, string pattern)
    {
        // 规范化路径分隔符
        path = path.Replace('\\', '/');
        pattern = pattern.Replace('\\', '/');

        // 处理 ** 通配符（匹配任意层级目录）
        if (pattern.Contains("**"))
        {
            var parts = pattern.Split("**", StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0)
                return true; // ** 匹配所有

            if (parts.Length == 1)
            {
                // **/pattern 或 pattern/**
                if (pattern.StartsWith("**"))
                    return path.EndsWith(parts[0].TrimStart('/')) || MatchesSimplePattern(path, parts[0].TrimStart('/'));
                else
                    return path.StartsWith(parts[0].TrimEnd('/')) || MatchesSimplePattern(path, parts[0].TrimEnd('/'));
            }

            // prefix/**/suffix
            return path.StartsWith(parts[0].TrimEnd('/')) && path.EndsWith(parts[1].TrimStart('/'));
        }

        // 处理简单的 * 通配符
        return MatchesSimplePattern(path, pattern);
    }

    /// <summary>
    /// 匹配简单的单层通配符模式
    /// </summary>
    private bool MatchesSimplePattern(string path, string pattern)
    {
        if (pattern == "*" || pattern == "*/*")
            return true;

        // 转换为正则表达式
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(path, regexPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 获取相对路径
    /// </summary>
    private string GetRelativePath(string fullPath, string basePath)
    {
        fullPath = fullPath.Replace('\\', '/');
        basePath = basePath.Replace('\\', '/');

        // 移除通配符
        basePath = basePath.TrimEnd('*');
        basePath = basePath.TrimEnd('/');

        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            var relative = fullPath.Substring(basePath.Length).TrimStart('/');
            return relative;
        }

        // 如果不以 basePath 开头，尝试提取最后的路径部分
        var basePathParts = basePath.Split('/');
        var fullPathParts = fullPath.Split('/');

        // 找到匹配的起始点
        for (int i = 0; i < basePathParts.Length && i < fullPathParts.Length; i++)
        {
            if (basePathParts[i] == fullPathParts[i] || basePathParts[i] == "*")
            {
                if (i == basePathParts.Length - 1)
                {
                    return string.Join("/", fullPathParts.Skip(i + 1));
                }
            }
            else
            {
                break;
            }
        }

        return Path.GetFileName(fullPath);
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

