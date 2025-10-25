using CSP2.Core.Abstractions;
using CSP2.Core.Models;
using Microsoft.Extensions.Logging;

namespace CSP2.Core.Services;

/// <summary>
/// Provider注册中心
/// </summary>
public class ProviderRegistry
{
    private readonly List<IPlatformProvider> _platformProviders = new();
    private readonly List<IFrameworkProvider> _frameworkProviders = new();
    private readonly ILogger<ProviderRegistry> _logger;

    public ProviderRegistry(ILogger<ProviderRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 注册平台提供者
    /// </summary>
    public void RegisterPlatformProvider(IPlatformProvider provider)
    {
        if (_platformProviders.Any(p => p.Metadata.Id == provider.Metadata.Id))
        {
            _logger.LogWarning("平台Provider {Id} 已注册，将被覆盖", provider.Metadata.Id);
            _platformProviders.RemoveAll(p => p.Metadata.Id == provider.Metadata.Id);
        }

        _platformProviders.Add(provider);
        _logger.LogInformation("已注册平台Provider: {Name} v{Version}", 
            provider.Metadata.Name, provider.Metadata.Version);
    }

    /// <summary>
    /// 注册框架提供者
    /// </summary>
    public void RegisterFrameworkProvider(IFrameworkProvider provider)
    {
        if (_frameworkProviders.Any(p => p.Metadata.Id == provider.Metadata.Id))
        {
            _logger.LogWarning("框架Provider {Id} 已注册，将被覆盖", provider.Metadata.Id);
            _frameworkProviders.RemoveAll(p => p.Metadata.Id == provider.Metadata.Id);
        }

        _frameworkProviders.Add(provider);
        _logger.LogInformation("已注册框架Provider: {Name} v{Version}", 
            provider.Metadata.Name, provider.Metadata.Version);
    }

    /// <summary>
    /// 自动选择最佳平台Provider
    /// </summary>
    public IPlatformProvider GetBestPlatformProvider()
    {
        var supportedProviders = _platformProviders
            .Where(p => p.IsSupported())
            .OrderByDescending(p => p.Metadata.Priority)
            .ToList();

        if (supportedProviders.Count == 0)
        {
            throw new InvalidOperationException("No provider found for the current platform");
        }

        var selected = supportedProviders.First();
        _logger.LogInformation("选择平台Provider: {Name}", selected.Metadata.Name);
        return selected;
    }

    /// <summary>
    /// 获取指定框架Provider
    /// </summary>
    public IFrameworkProvider? GetFrameworkProvider(string frameworkId)
    {
        return _frameworkProviders.FirstOrDefault(p => p.Metadata.Id == frameworkId);
    }

    /// <summary>
    /// 列出所有可用框架
    /// </summary>
    public List<FrameworkInfo> GetAvailableFrameworks()
    {
        return _frameworkProviders
            .Select(p => p.FrameworkInfo)
            .OrderBy(f => f.Name)
            .ToList();
    }

    /// <summary>
    /// 获取所有已注册的平台Provider
    /// </summary>
    public List<IPlatformProvider> GetAllPlatformProviders()
    {
        return _platformProviders.ToList();
    }

    /// <summary>
    /// 获取所有已注册的框架Provider
    /// </summary>
    public List<IFrameworkProvider> GetAllFrameworkProviders()
    {
        return _frameworkProviders.ToList();
    }
}


