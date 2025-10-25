using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace CSP2.Desktop.Services;

/// <summary>
/// 本地化服务 - 管理应用程序语言切换
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private readonly ILogger<LocalizationService> _logger;
    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        private set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
            }
        }
    }

    public string CurrentLanguageCode => _currentCulture.Name;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;
        
        // 从配置加载语言设置，默认中文
        var savedLanguage = Properties.Settings.Default.Language;
        _currentCulture = string.IsNullOrEmpty(savedLanguage) 
            ? new CultureInfo("zh-CN") 
            : new CultureInfo(savedLanguage);
        
        ApplyCulture(_currentCulture);
        _logger.LogInformation("Localization service initialized with culture: {Culture}", _currentCulture.Name);
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public void ChangeLanguage(string languageCode)
    {
        try
        {
            var newCulture = new CultureInfo(languageCode);
            ApplyCulture(newCulture);
            
            // 保存语言设置
            Properties.Settings.Default.Language = languageCode;
            Properties.Settings.Default.Save();
            
            _logger.LogInformation("Language changed to: {Language}", languageCode);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change language to: {Language}", languageCode);
            throw;
        }
    }

    /// <summary>
    /// 应用文化设置
    /// </summary>
    private void ApplyCulture(CultureInfo culture)
    {
        CurrentCulture = culture;
        
        // 设置当前线程的文化
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        
        // 更新WPF资源字典
        UpdateResourceDictionary(culture);
    }

    /// <summary>
    /// 更新资源字典
    /// </summary>
    private void UpdateResourceDictionary(CultureInfo culture)
    {
        // 设置资源管理器的文化
        Resources.Strings.Culture = culture;
        
        // 强制刷新绑定（通过改变dummy属性）
        Application.Current.Dispatcher.Invoke(() =>
        {
            // 触发所有绑定更新
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        });
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public string GetString(string key)
    {
        try
        {
            var value = Resources.Strings.ResourceManager.GetString(key, _currentCulture);
            return value ?? $"[{key}]";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get localized string for key: {Key}", key);
            return $"[{key}]";
        }
    }

    /// <summary>
    /// 获取支持的语言列表
    /// </summary>
    public static LanguageInfo[] GetSupportedLanguages()
    {
        return new[]
        {
            new LanguageInfo("zh-CN", "简体中文", "🇨🇳"),
            new LanguageInfo("en", "English", "🇺🇸")
        };
    }
}

/// <summary>
/// 语言信息
/// </summary>
public record LanguageInfo(string Code, string DisplayName, string Flag);

