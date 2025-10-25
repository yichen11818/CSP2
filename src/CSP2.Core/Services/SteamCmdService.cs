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

    private const string SteamCmdDownloadUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
    private const int Cs2AppId = 730; // CS2的AppID

    public SteamCmdService(
        IConfigurationService configService,
        ILogger<SteamCmdService> logger,
        HttpClient httpClient)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> IsSteamCmdInstalledAsync()
    {
        var steamCmdPath = GetSteamCmdPath();
        var exePath = Path.Combine(steamCmdPath, "steamcmd.exe");
        return await Task.FromResult(File.Exists(exePath));
    }

    public async Task<bool> InstallSteamCmdAsync(string installPath, IProgress<DownloadProgress>? progress = null)
    {
        try
        {
            _logger.LogInformation("开始下载SteamCMD到: {Path}", installPath);

            // 创建安装目录
            Directory.CreateDirectory(installPath);

            // 下载SteamCMD
            var zipPath = Path.Combine(installPath, "steamcmd.zip");
            
            progress?.Report(new DownloadProgress
            {
                Percentage = 0,
                Message = "正在下载SteamCMD...",
                TotalBytes = 0,
                BytesDownloaded = 0
            });

            using (var response = await _httpClient.GetAsync(SteamCmdDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
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

                        // 每100ms更新一次进度
                        if ((DateTime.Now - lastProgressUpdate).TotalMilliseconds > 100)
                        {
                            var percentage = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : 0;
                            progress?.Report(new DownloadProgress
                            {
                                Percentage = percentage,
                                Message = $"正在下载SteamCMD... {totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB",
                                TotalBytes = totalBytes,
                                BytesDownloaded = totalRead
                            });
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

            // 解压
            ZipFile.ExtractToDirectory(zipPath, installPath, true);
            File.Delete(zipPath);

            // 保存SteamCMD路径到配置
            var settings = await _configService.LoadAppSettingsAsync();
            settings.SteamCmd.InstallPath = installPath;
            await _configService.SaveAppSettingsAsync(settings);

            _logger.LogInformation("SteamCMD安装成功: {Path}", installPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装SteamCMD失败");
            return false;
        }
    }

    public async Task<bool> InstallOrUpdateServerAsync(string serverPath, bool validate = false, 
        IProgress<InstallProgress>? progress = null)
    {
        try
        {
            _logger.LogInformation("开始安装/更新CS2服务器到: {Path}", serverPath);

            // 确保SteamCMD已安装
            if (!await IsSteamCmdInstalledAsync())
            {
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
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogDebug("SteamCMD: {Output}", e.Data);
                        
                        // 尝试解析进度
                        if (e.Data.Contains("Update state") || e.Data.Contains("progress"))
                        {
                            progress?.Report(new InstallProgress
                            {
                                Percentage = 50, // 简化进度显示
                                Message = e.Data.Trim(),
                                CurrentStep = "下载服务器文件",
                                TotalSteps = 2,
                                CurrentStepIndex = 2
                            });
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
                    return false;
                }
            }

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
        // 先尝试从配置中读取
        var settings = _configService.LoadAppSettingsAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(settings.SteamCmd.InstallPath))
        {
            return settings.SteamCmd.InstallPath;
        }

        // 默认路径：应用数据目录下的steamcmd文件夹
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "CSP2", "steamcmd");
    }
}

