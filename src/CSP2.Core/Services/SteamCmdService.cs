using System.Diagnostics;
using System.IO.Compression;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// SteamCMD服务实现
/// </summary>
public class SteamCmdService : ISteamCmdService
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<SteamCmdService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IDownloadManager? _downloadManager;

    private const string SteamCmdDownloadUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
    private const string SteamCmdDownloadUrlChina = "https://media.st.dl.bscstorage.net/client/installer/steamcmd.zip";
    private const int Cs2AppId = 730; // CS2的AppID（游戏和专用服务器共用）

    public SteamCmdService(
        IConfigurationService configService,
        ILogger<SteamCmdService> logger,
        HttpClient httpClient,
        IDownloadManager? downloadManager = null)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = httpClient;
        _downloadManager = downloadManager;
    }

    public async Task<bool> IsSteamCmdInstalledAsync()
    {
        var steamCmdPath = GetSteamCmdPath();
        var exePath = Path.Combine(steamCmdPath, "steamcmd.exe");
        return await Task.FromResult(File.Exists(exePath));
    }

    public async Task<bool> InstallSteamCmdAsync(string installPath, IProgress<DownloadProgress>? progress = null)
    {
        DownloadTask? downloadTask = null;

        // 如果有下载管理器，创建下载任务
        if (_downloadManager != null)
        {
            downloadTask = new DownloadTask
            {
                Name = "SteamCMD",
                Description = "下载并安装SteamCMD",
                TaskType = DownloadTaskType.SteamCmd,
                Status = DownloadTaskStatus.Downloading
            };
            _downloadManager.AddTask(downloadTask);
        }

        try
        {
            _logger.LogInformation("开始下载SteamCMD到: {Path}", installPath);
            
            // 尝试使用国际节点，如果失败则使用中国内地节点
            var downloadUrl = SteamCmdDownloadUrl;
            _logger.LogDebug("SteamCMD下载URL: {Url}", downloadUrl);

            // 创建安装目录
            Directory.CreateDirectory(installPath);
            _logger.LogDebug("创建安装目录: {Path}", installPath);

            // 下载SteamCMD
            var zipPath = Path.Combine(installPath, "steamcmd.zip");
            
            progress?.Report(new DownloadProgress
            {
                Percentage = 0,
                Message = "正在下载SteamCMD...",
                TotalBytes = 0,
                BytesDownloaded = 0
            });

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 0, "正在连接下载服务器...");

            HttpResponseMessage? response = null;
            
            try
            {
                response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "国际节点下载失败，尝试使用中国内地节点");
                downloadUrl = SteamCmdDownloadUrlChina;
                response?.Dispose();
                response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
            }

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 5, "开始下载SteamCMD...");

            using (response)
            {
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                _logger.LogDebug("SteamCMD文件大小: {Size} MB", totalBytes / 1024 / 1024);
                
                if (downloadTask != null)
                {
                    downloadTask.TotalSize = totalBytes;
                }
                
                var buffer = new byte[8192];
                var totalRead = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    int bytesRead;
                    var lastProgressUpdate = DateTime.Now;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (downloadTask != null)
                        {
                            downloadTask.DownloadedSize = totalRead;
                        }

                        // 每100ms更新一次进度
                        if ((DateTime.Now - lastProgressUpdate).TotalMilliseconds > 100)
                        {
                            var percentage = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : 0;
                            var message = $"正在下载SteamCMD... {totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB";
                            
                            progress?.Report(new DownloadProgress
                            {
                                Percentage = percentage,
                                Message = message,
                                TotalBytes = totalBytes,
                                BytesDownloaded = totalRead
                            });

                            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", percentage, message);
                            lastProgressUpdate = DateTime.Now;
                        }
                    }
                }
            }

            progress?.Report(new DownloadProgress
            {
                Percentage = 100,
                Message = "正在解压SteamCMD...",
                TotalBytes = 0,
                BytesDownloaded = 0
            });

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 95, "正在解压SteamCMD...");

            // 在后台线程解压，避免阻塞UI
            _logger.LogDebug("开始解压SteamCMD到: {Path}", installPath);
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(zipPath, installPath, true);
                File.Delete(zipPath);
            });
            _logger.LogDebug("SteamCMD解压完成");

            // 保存SteamCMD路径到配置
            var settings = await _configService.LoadAppSettingsAsync();
            settings.SteamCmd.InstallPath = installPath;
            await _configService.SaveAppSettingsAsync(settings);
            _logger.LogDebug("SteamCMD路径已保存到配置");

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 100, "SteamCMD安装完成");

            _logger.LogInformation("SteamCMD安装成功: {Path}", installPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装SteamCMD失败");
            _downloadManager?.UpdateTaskStatus(downloadTask?.Id ?? "", DownloadTaskStatus.Failed, ex.Message);
            return false;
        }
    }

    public async Task<bool> InstallOrUpdateServerAsync(string serverPath, bool validate = false, 
        IProgress<InstallProgress>? progress = null)
    {
        DownloadTask? downloadTask = null;
        
        try
        {
            _logger.LogInformation("开始安装/更新CS2服务器到: {Path}", serverPath);
            _logger.LogDebug("验证模式: {Validate}", validate);

            // 创建下载任务
            if (_downloadManager != null)
            {
                downloadTask = new DownloadTask
                {
                    Name = "CS2服务器",
                    Description = $"下载CS2服务器文件到 {Path.GetFileName(serverPath)}",
                    TaskType = DownloadTaskType.SteamCmd,
                    Status = DownloadTaskStatus.Downloading,
                    TotalSize = 30L * 1024 * 1024 * 1024 // 约30GB（估算值）
                };
                _downloadManager.AddTask(downloadTask);
            }

            // 确保SteamCMD已安装
            _logger.LogDebug("检查SteamCMD是否已安装");
            if (!await IsSteamCmdInstalledAsync())
            {
                _logger.LogWarning("SteamCMD未安装，需要先安装SteamCMD");
                _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 0, "正在安装SteamCMD...");
                
                progress?.Report(new InstallProgress
                {
                    Percentage = 0,
                    Message = "SteamCMD未安装，正在安装...",
                    CurrentStep = "安装SteamCMD",
                    TotalSteps = 2,
                    CurrentStepIndex = 1
                });

                var steamCmdPath = GetSteamCmdPath();
                var downloadProgress = new Progress<DownloadProgress>(p =>
                {
                    progress?.Report(new InstallProgress
                    {
                        Percentage = p.Percentage * 0.3, // SteamCMD下载占总进度的30%
                        Message = p.Message,
                        CurrentStep = "安装SteamCMD",
                        TotalSteps = 2,
                        CurrentStepIndex = 1
                    });
                });

                if (!await InstallSteamCmdAsync(steamCmdPath, downloadProgress))
                {
                    throw new Exception("安装SteamCMD失败");
                }
            }

            // 创建服务器目录
            Directory.CreateDirectory(serverPath);
            _logger.LogDebug("创建服务器目录: {Path}", serverPath);

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 30, "正在下载CS2服务器文件（约30GB）...");
            
            progress?.Report(new InstallProgress
            {
                Percentage = 30,
                Message = "正在下载CS2服务器文件...",
                CurrentStep = "下载服务器文件",
                TotalSteps = 2,
                CurrentStepIndex = 2
            });

            // 构建SteamCMD命令
            var steamCmdExe = Path.Combine(GetSteamCmdPath(), "steamcmd.exe");
            var validateParam = validate ? "validate" : "";
            
            // SteamCMD命令: +force_install_dir <path> +login anonymous +app_update 730 validate +quit
            var arguments = $"+force_install_dir \"{serverPath}\" +login anonymous +app_update {Cs2AppId} {validateParam} +quit";

            _logger.LogInformation("执行SteamCMD命令: {Exe} {Args}", steamCmdExe, arguments);
            _logger.LogDebug("CS2 AppID: {AppId}", Cs2AppId);

            // 启动SteamCMD进程
            var startInfo = new ProcessStartInfo
            {
                FileName = steamCmdExe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = GetSteamCmdPath()
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                var lastProgress = 30.0;
                var lastProgressUpdate = DateTime.Now;
                
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        var output = e.Data.Trim();
                        _logger.LogDebug("SteamCMD: {Output}", output);
                        
                        // 解析SteamCMD的进度输出
                        // 格式: Update state (0x61) downloading, progress: 12.34 (1234567890 / 10000000000)
                        // 或: Update state (0x81) verifying, progress: 56.78 (...)
                        
                        double? parsedProgress = null;
                        string statusMessage = output;
                        
                        // 尝试解析精确进度
                        if (output.Contains("progress:"))
                        {
                            try
                            {
                                // 提取进度百分比
                                var progressStart = output.IndexOf("progress:") + "progress:".Length;
                                var progressEnd = output.IndexOf("(", progressStart);
                                if (progressEnd < 0) progressEnd = output.Length;
                                
                                var progressStr = output.Substring(progressStart, progressEnd - progressStart).Trim();
                                if (double.TryParse(progressStr, out var progressValue))
                                {
                                    parsedProgress = progressValue;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "解析进度失败: {Output}", output);
                            }
                            
                            // 识别状态
                            if (output.Contains("downloading"))
                            {
                                statusMessage = $"正在下载 CS2 服务器文件... {parsedProgress:F2}%";
                            }
                            else if (output.Contains("verifying"))
                            {
                                statusMessage = $"正在验证文件完整性... {parsedProgress:F2}%";
                            }
                            else if (output.Contains("reconfiguring"))
                            {
                                statusMessage = "正在配置服务器...";
                            }
                        }
                        else if (output.Contains("Success!") && output.Contains("fully installed"))
                        {
                            parsedProgress = 100;
                            statusMessage = "✅ CS2服务器安装完成！";
                        }
                        else if (output.Contains("Update state"))
                        {
                            // 其他状态更新
                            if (output.Contains("preallocating"))
                            {
                                statusMessage = "正在预分配磁盘空间...";
                            }
                            else if (output.Contains("validating"))
                            {
                                statusMessage = "正在验证现有文件...";
                            }
                        }
                        
                        // 更新进度
                        if (parsedProgress.HasValue)
                        {
                            // 将0-100的进度映射到30-100的范围（前30%是SteamCMD安装）
                            lastProgress = 30 + (parsedProgress.Value * 0.7);
                        }
                        else if (output.Contains("Update state") || output.Contains("downloading") || output.Contains("verifying"))
                        {
                            // 如果无法解析精确进度，模拟进度增长
                            lastProgress = Math.Min(lastProgress + 0.5, 95);
                        }
                        
                        // 限制更新频率（避免UI刷新过快）
                        if ((DateTime.Now - lastProgressUpdate).TotalMilliseconds > 500 || parsedProgress.HasValue)
                        {
                            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", lastProgress, statusMessage);
                            
                            progress?.Report(new InstallProgress
                            {
                                Percentage = lastProgress,
                                Message = statusMessage,
                                CurrentStep = "下载服务器文件",
                                TotalSteps = 2,
                                CurrentStepIndex = 2
                            });
                            
                            lastProgressUpdate = DateTime.Now;
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogWarning("SteamCMD错误: {Error}", e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("SteamCMD执行失败，退出码: {ExitCode}", process.ExitCode);
                    _downloadManager?.UpdateTaskStatus(downloadTask?.Id ?? "", DownloadTaskStatus.Failed, 
                        $"SteamCMD执行失败，退出码: {process.ExitCode}");
                    return false;
                }
            }

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 100, "CS2服务器安装完成");
            _downloadManager?.UpdateTaskStatus(downloadTask?.Id ?? "", DownloadTaskStatus.Completed);
            
            progress?.Report(new InstallProgress
            {
                Percentage = 100,
                Message = "CS2服务器安装完成",
                CurrentStep = "完成",
                TotalSteps = 2,
                CurrentStepIndex = 2
            });

            _logger.LogInformation("CS2服务器安装/更新成功: {Path}", serverPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装/更新CS2服务器失败");
            _downloadManager?.UpdateTaskStatus(downloadTask?.Id ?? "", DownloadTaskStatus.Failed, ex.Message);
            return false;
        }
    }

    public async Task<bool> ValidateServerFilesAsync(string serverPath, IProgress<InstallProgress>? progress = null)
    {
        _logger.LogInformation("验证CS2服务器文件: {Path}", serverPath);
        return await InstallOrUpdateServerAsync(serverPath, validate: true, progress);
    }

    public string GetSteamCmdPath()
    {
        // 先尝试从配置中读取（使用同步访问，避免死锁）
        try
        {
            var task = Task.Run(async () => await _configService.LoadAppSettingsAsync());
            var settings = task.GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(settings.SteamCmd.InstallPath))
            {
                return settings.SteamCmd.InstallPath;
            }
        }
        catch
        {
            // 如果读取配置失败，使用默认路径
        }

        // 默认路径：应用数据目录下的steamcmd文件夹
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "CSP2", "steamcmd");
    }

    public async Task<bool> UninstallSteamCmdAsync()
    {
        DownloadTask? downloadTask = null;

        // 如果有下载管理器，创建卸载任务
        if (_downloadManager != null)
        {
            downloadTask = new DownloadTask
            {
                Name = "SteamCMD",
                Description = "卸载SteamCMD",
                TaskType = DownloadTaskType.Other,
                Status = DownloadTaskStatus.Downloading
            };
            _downloadManager.AddTask(downloadTask);
        }

        try
        {
            var steamCmdPath = GetSteamCmdPath();
            _logger.LogInformation("开始卸载SteamCMD: {Path}", steamCmdPath);

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 10, "正在检查SteamCMD安装...");

            // 检查目录是否存在
            if (!Directory.Exists(steamCmdPath))
            {
                _logger.LogWarning("SteamCMD目录不存在: {Path}", steamCmdPath);
                _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 100, "SteamCMD未安装");
                return true; // 已经不存在了，算作成功
            }

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 30, "正在删除SteamCMD文件...");

            // 删除目录及其所有内容
            Directory.Delete(steamCmdPath, recursive: true);
            _logger.LogDebug("SteamCMD目录已删除: {Path}", steamCmdPath);

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 80, "正在清理配置...");

            // 清除配置中的路径
            var settings = await _configService.LoadAppSettingsAsync();
            settings.SteamCmd.InstallPath = string.Empty;
            await _configService.SaveAppSettingsAsync(settings);
            _logger.LogDebug("SteamCMD配置已清除");

            _downloadManager?.UpdateTaskProgress(downloadTask?.Id ?? "", 100, "SteamCMD卸载完成");

            _logger.LogInformation("SteamCMD卸载成功: {Path}", steamCmdPath);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "卸载SteamCMD失败：权限不足");
            _downloadManager?.UpdateTaskStatus(downloadTask?.Id ?? "", DownloadTaskStatus.Failed, "权限不足，无法删除文件");
            return false;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "卸载SteamCMD失败：文件被占用");
            _downloadManager?.UpdateTaskStatus(downloadTask?.Id ?? "", DownloadTaskStatus.Failed, "文件被占用，请关闭相关进程后重试");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载SteamCMD失败");
            _downloadManager?.UpdateTaskStatus(downloadTask?.Id ?? "", DownloadTaskStatus.Failed, ex.Message);
            return false;
        }
    }
}

