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
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true
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

        if (force)
        {
            process.Kill(entireProcessTree: true);
        }
        else
        {
            // 尝试优雅关闭
            try
            {
                await process.StandardInput.WriteLineAsync("quit");
                await process.StandardInput.FlushAsync();
                
                // 等待最多10秒
                if (!await Task.Run(() => process.WaitForExit(10000)))
                {
                    // 超时则强制终止
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // 如果优雅关闭失败,强制终止
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
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

