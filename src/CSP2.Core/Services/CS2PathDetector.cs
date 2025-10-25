using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace CSP2.Core.Services;

/// <summary>
/// CS2路径检测信息
/// </summary>
public class CS2InstallInfo
{
    /// <summary>
    /// 安装路径
    /// </summary>
    public required string InstallPath { get; set; }

    /// <summary>
    /// 检测来源
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// 是否有效（包含cs2.exe）
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// CS2可执行文件路径
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// 安装大小（字节）
    /// </summary>
    public long? InstallSize { get; set; }
}

/// <summary>
/// CS2路径检测服务
/// </summary>
public class CS2PathDetector
{
    private readonly ILogger<CS2PathDetector> _logger;

    public CS2PathDetector(ILogger<CS2PathDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 检测所有可能的CS2安装路径
    /// </summary>
    public async Task<List<CS2InstallInfo>> DetectAllInstallationsAsync()
    {
        var installations = new List<CS2InstallInfo>();

        _logger.LogInformation("开始检测CS2安装路径");

        // 1. 从注册表检测
        var registryPath = await DetectFromRegistryAsync();
        if (registryPath != null)
        {
            installations.Add(registryPath);
        }

        // 2. 从Steam库文件夹检测
        var steamLibraryPaths = await DetectFromSteamLibrariesAsync();
        installations.AddRange(steamLibraryPaths);

        // 3. 从默认路径检测
        var defaultPath = await DetectFromDefaultPathAsync();
        if (defaultPath != null)
        {
            installations.Add(defaultPath);
        }

        // 去重（基于路径）
        installations = installations
            .GroupBy(i => i.InstallPath.TrimEnd('\\', '/').ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        _logger.LogInformation("检测完成，找到 {Count} 个CS2安装", installations.Count);
        
        return installations;
    }

    /// <summary>
    /// 从注册表检测CS2路径
    /// </summary>
    [SupportedOSPlatform("windows")]
    private async Task<CS2InstallInfo?> DetectFromRegistryAsync()
    {
        try
        {
            _logger.LogDebug("尝试从注册表检测CS2路径");

            // 检查 HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\cs2
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\cs2");
            if (key != null)
            {
                var installPath = key.GetValue("installpath") as string;
                if (!string.IsNullOrEmpty(installPath))
                {
                    _logger.LogInformation("从注册表找到CS2路径: {Path}", installPath);
                    
                    var info = new CS2InstallInfo
                    {
                        InstallPath = installPath,
                        Source = "Windows注册表"
                    };

                    await ValidateInstallationAsync(info);
                    return info;
                }
            }

            // 也检查64位注册表路径
            using var key64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\cs2");
            if (key64 != null)
            {
                var installPath = key64.GetValue("installpath") as string;
                if (!string.IsNullOrEmpty(installPath))
                {
                    _logger.LogInformation("从注册表(64位)找到CS2路径: {Path}", installPath);
                    
                    var info = new CS2InstallInfo
                    {
                        InstallPath = installPath,
                        Source = "Windows注册表"
                    };

                    await ValidateInstallationAsync(info);
                    return info;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从注册表检测CS2路径失败");
        }

        return null;
    }

    /// <summary>
    /// 从Steam库文件夹检测CS2路径
    /// </summary>
    private async Task<List<CS2InstallInfo>> DetectFromSteamLibrariesAsync()
    {
        var installations = new List<CS2InstallInfo>();

        try
        {
            _logger.LogDebug("尝试从Steam库文件夹检测CS2路径");

            // 获取Steam安装路径
            var steamPath = GetSteamInstallPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                _logger.LogWarning("未找到Steam安装路径");
                return installations;
            }

            _logger.LogDebug("Steam安装路径: {Path}", steamPath);

            // 读取 libraryfolders.vdf
            var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFoldersPath))
            {
                _logger.LogWarning("未找到libraryfolders.vdf文件: {Path}", libraryFoldersPath);
                return installations;
            }

            _logger.LogDebug("读取libraryfolders.vdf: {Path}", libraryFoldersPath);
            var vdfContent = await File.ReadAllTextAsync(libraryFoldersPath);

            // 解析VDF文件，查找所有库路径
            var libraryPaths = ParseLibraryPaths(vdfContent);
            _logger.LogInformation("找到 {Count} 个Steam库路径", libraryPaths.Count);

            // 在每个库路径中查找CS2
            foreach (var libraryPath in libraryPaths)
            {
                var cs2Path = await FindCS2InLibraryAsync(libraryPath);
                if (cs2Path != null)
                {
                    installations.Add(cs2Path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从Steam库文件夹检测CS2路径失败");
        }

        return installations;
    }

    /// <summary>
    /// 从默认路径检测CS2
    /// </summary>
    private async Task<CS2InstallInfo?> DetectFromDefaultPathAsync()
    {
        try
        {
            _logger.LogDebug("尝试从默认路径检测CS2");

            var defaultPath = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive";
            if (Directory.Exists(defaultPath))
            {
                _logger.LogInformation("在默认路径找到CS2: {Path}", defaultPath);
                
                var info = new CS2InstallInfo
                {
                    InstallPath = defaultPath,
                    Source = "默认安装路径"
                };

                await ValidateInstallationAsync(info);
                return info;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从默认路径检测CS2失败");
        }

        return null;
    }

    /// <summary>
    /// 获取Steam安装路径
    /// </summary>
    [SupportedOSPlatform("windows")]
    private string? GetSteamInstallPath()
    {
        try
        {
            // 尝试从注册表获取Steam路径
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key != null)
            {
                var path = key.GetValue("SteamPath") as string;
                if (!string.IsNullOrEmpty(path))
                {
                    return path.Replace('/', '\\');
                }
            }

            // 尝试32位注册表
            using var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (key32 != null)
            {
                var path = key32.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }

            // 尝试64位注册表
            using var key64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key64 != null)
            {
                var path = key64.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取Steam安装路径失败");
        }

        return null;
    }

    /// <summary>
    /// 解析libraryfolders.vdf文件，提取所有库路径
    /// </summary>
    private List<string> ParseLibraryPaths(string vdfContent)
    {
        var paths = new List<string>();

        try
        {
            // VDF格式示例:
            // "path"    "D:\\SteamLibrary"
            var pathRegex = new Regex(@"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);
            var matches = pathRegex.Matches(vdfContent);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var path = match.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(path))
                    {
                        paths.Add(path);
                        _logger.LogDebug("解析到库路径: {Path}", path);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析libraryfolders.vdf失败");
        }

        return paths;
    }

    /// <summary>
    /// 在Steam库中查找CS2
    /// </summary>
    private async Task<CS2InstallInfo?> FindCS2InLibraryAsync(string libraryPath)
    {
        try
        {
            // 检查 appmanifest_730.acf (CS2的AppID是730)
            var manifestPath = Path.Combine(libraryPath, "steamapps", "appmanifest_730.acf");
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            _logger.LogDebug("找到CS2清单文件: {Path}", manifestPath);

            // 读取清单文件获取安装文件夹名
            var manifestContent = await File.ReadAllTextAsync(manifestPath);
            var installDirMatch = Regex.Match(manifestContent, @"""installdir""\s+""([^""]+)""", RegexOptions.IgnoreCase);
            
            if (!installDirMatch.Success)
            {
                return null;
            }

            var installDir = installDirMatch.Groups[1].Value;
            var cs2Path = Path.Combine(libraryPath, "steamapps", "common", installDir);

            if (!Directory.Exists(cs2Path))
            {
                return null;
            }

            _logger.LogInformation("在Steam库中找到CS2: {Path}", cs2Path);

            var info = new CS2InstallInfo
            {
                InstallPath = cs2Path,
                Source = $"Steam库 ({libraryPath})"
            };

            await ValidateInstallationAsync(info);
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "在Steam库中查找CS2失败: {Path}", libraryPath);
            return null;
        }
    }

    /// <summary>
    /// 验证CS2安装是否有效
    /// </summary>
    private async Task ValidateInstallationAsync(CS2InstallInfo info)
    {
        try
        {
            // 检查cs2.exe是否存在
            var exePath = Path.Combine(info.InstallPath, "game", "bin", "win64", "cs2.exe");
            info.ExecutablePath = exePath;
            info.IsValid = File.Exists(exePath);

            if (info.IsValid)
            {
                _logger.LogInformation("验证CS2安装有效: {Path}", info.InstallPath);

                // 尝试获取安装大小
                try
                {
                    var dirInfo = new DirectoryInfo(info.InstallPath);
                    info.InstallSize = await Task.Run(() => 
                        dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                               .Sum(f => f.Length));
                }
                catch
                {
                    // 获取大小失败不影响验证结果
                }
            }
            else
            {
                _logger.LogWarning("CS2安装无效，找不到cs2.exe: {Path}", exePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "验证CS2安装失败: {Path}", info.InstallPath);
            info.IsValid = false;
        }
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

