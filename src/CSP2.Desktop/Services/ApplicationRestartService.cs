using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace CSP2.Desktop.Services;

/// <summary>
/// 应用程序重启服务
/// </summary>
public class ApplicationRestartService
{
    private readonly ILogger<ApplicationRestartService> _logger;

    public ApplicationRestartService(ILogger<ApplicationRestartService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 重启应用程序
    /// </summary>
    /// <param name="delay">延迟时间（毫秒）</param>
    public void RestartApplication(int delay = 1000)
    {
        try
        {
            _logger.LogInformation("准备重启应用程序，延迟 {Delay}ms", delay);

            // 获取当前应用程序路径
            var currentProcess = Process.GetCurrentProcess();
            var applicationPath = currentProcess.MainModule?.FileName;

            if (string.IsNullOrEmpty(applicationPath))
            {
                _logger.LogError("无法获取应用程序路径");
                return;
            }

            // 创建重启脚本
            var restartScript = CreateRestartScript(applicationPath, delay);
            
            // 执行重启脚本
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{restartScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo);
            
            _logger.LogInformation("重启脚本已启动");

            // 关闭当前应用程序
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启应用程序失败");
            MessageBox.Show($"重启失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 创建重启脚本
    /// </summary>
    /// <param name="applicationPath">应用程序路径</param>
    /// <param name="delay">延迟时间</param>
    /// <returns>脚本路径</returns>
    private string CreateRestartScript(string applicationPath, int delay)
    {
        var tempPath = Path.GetTempPath();
        var scriptPath = Path.Combine(tempPath, "CSP2_Restart.bat");

        var scriptContent = $@"@echo off
timeout /t {delay / 1000} /nobreak >nul
start """" ""{applicationPath}""
del ""{scriptPath}""
";

        File.WriteAllText(scriptPath, scriptContent);
        return scriptPath;
    }

    /// <summary>
    /// 显示重启确认对话框并执行重启
    /// </summary>
    /// <param name="owner">父窗口</param>
    /// <param name="changeType">更改类型</param>
    /// <returns>是否执行了重启</returns>
    public bool ShowRestartConfirmation(Window? owner, string changeType = "")
    {
        try
        {
            _logger.LogInformation("准备显示重启确认对话框，更改类型: {ChangeType}", changeType);
            
            var shouldRestart = Views.Dialogs.RestartConfirmDialog.ShowDialog(owner, changeType);
            
            if (shouldRestart)
            {
                _logger.LogInformation("用户确认重启应用程序，更改类型: {ChangeType}", changeType);
                RestartApplication();
                return true;
            }
            else
            {
                _logger.LogInformation("用户取消重启应用程序");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示重启确认对话框失败");
            
            // 如果对话框失败，使用简单的MessageBox作为备用
            var result = MessageBox.Show(
                $"设置已更改，需要重启应用程序以完全生效。\n\n是否立即重启？",
                "需要重启",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                RestartApplication();
                return true;
            }
            
            return false;
        }
    }
}
