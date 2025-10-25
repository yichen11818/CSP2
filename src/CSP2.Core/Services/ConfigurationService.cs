using System.Text.Json;
using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// 配置服务实现
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _dataDirectory;
    private readonly string _serversFilePath;
    private readonly string _settingsFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        
        // 数据目录：应用程序目录下的 data 文件夹
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _dataDirectory = Path.Combine(appDirectory, "data");
        _serversFilePath = Path.Combine(_dataDirectory, "servers.json");
        _settingsFilePath = Path.Combine(_dataDirectory, "settings.json");

        // 确保数据目录存在
        EnsureDataDirectory();
    }

    private void EnsureDataDirectory()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
            _logger.LogInformation("创建数据目录: {Path}", _dataDirectory);
        }
    }

    public async Task<List<Server>> LoadServersAsync()
    {
        try
        {
            if (!File.Exists(_serversFilePath))
            {
                _logger.LogInformation("服务器配置文件不存在，返回空列表");
                return new List<Server>();
            }

            var json = await File.ReadAllTextAsync(_serversFilePath);
            var servers = JsonSerializer.Deserialize<List<Server>>(json, JsonOptions);
            
            _logger.LogInformation("已加载 {Count} 个服务器配置", servers?.Count ?? 0);
            return servers ?? new List<Server>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载服务器配置失败");
            return new List<Server>();
        }
    }

    public async Task<bool> SaveServersAsync(List<Server> servers)
    {
        try
        {
            var json = JsonSerializer.Serialize(servers, JsonOptions);
            await File.WriteAllTextAsync(_serversFilePath, json);
            
            _logger.LogInformation("已保存 {Count} 个服务器配置", servers.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存服务器配置失败");
            return false;
        }
    }

    public async Task<AppSettings> LoadAppSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("应用设置文件不存在，返回默认设置");
                return new AppSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            
            _logger.LogInformation("已加载应用设置");
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载应用设置失败");
            return new AppSettings();
        }
    }

    public async Task<bool> SaveAppSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            
            _logger.LogInformation("已保存应用设置");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存应用设置失败");
            return false;
        }
    }

    public string GetDataDirectory()
    {
        return _dataDirectory;
    }
}


