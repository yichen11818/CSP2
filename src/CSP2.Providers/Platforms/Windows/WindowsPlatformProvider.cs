using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;

namespace CSP2.Providers.Platforms.Windows;

/// <summary>
/// Windows平台提供者实现
/// </summary>
public class WindowsPlatformProvider : IPlatformProvider
{
    public ProviderMetadata Metadata => new()
    {
        Id = "windows",
        Name = "Windows",
        Version = "1.0.0",
        Author = "CSP2 Team",
        Description = "Windows平台支持",
        Priority = 100
    };

    public bool IsSupported()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public async Task<Process> StartServerProcessAsync(string serverPath, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = false,  // 允许显示服务器控制台窗口
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false  // 不重定向stdin，避免与CS2控制台冲突
        };

        var process = new Process { StartInfo = startInfo };
        
        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动服务器进程");
        }

        await Task.CompletedTask;
        return process;
    }

    public async Task StopServerProcessAsync(Process process, bool force = false)
    {
        if (process.HasExited)
        {
            return;
        }

        // CS2服务器不支持通过stdin发送quit命令（会导致控制台错误）
        // 直接终止进程是最可靠的方式
        try
        {
            if (!force)
            {
                // 给进程一点时间保存状态
                await Task.Delay(500);
            }
            
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception)
        {
            // 进程可能已经退出，忽略异常
        }
    }

    public Task<bool> IsPortInUseAsync(int port)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = ipGlobalProperties.GetActiveTcpListeners();
        var connections = ipGlobalProperties.GetActiveTcpConnections();

        bool inUse = listeners.Any(x => x.Port == port) || 
                     connections.Any(x => x.LocalEndPoint.Port == port);

        return Task.FromResult(inUse);
    }

    public Task<Dictionary<string, string>> GetSystemInfoAsync()
    {
        var info = new Dictionary<string, string>
        {
            ["OS"] = "Windows",
            ["Version"] = Environment.OSVersion.VersionString,
            ["Architecture"] = RuntimeInformation.OSArchitecture.ToString(),
            ["ProcessorCount"] = Environment.ProcessorCount.ToString(),
            ["MachineName"] = Environment.MachineName,
            ["UserName"] = Environment.UserName,
            ["Is64BitOS"] = Environment.Is64BitOperatingSystem.ToString(),
            [".NET Version"] = RuntimeInformation.FrameworkDescription
        };

        return Task.FromResult(info);
    }

    public Task<bool> HasExecutePermissionAsync(string filePath)
    {
        // Windows不需要特殊的执行权限
        return Task.FromResult(File.Exists(filePath));
    }

    public Task SetExecutePermissionAsync(string filePath)
    {
        // Windows不需要设置执行权限
        return Task.CompletedTask;
    }
}

