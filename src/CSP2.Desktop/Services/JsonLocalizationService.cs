using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace CSP2.Desktop.Services;

/// <summary>
/// JSON格式的本地化服务 - 管理应用程序语言切换
/// </summary>
public class JsonLocalizationService : INotifyPropertyChanged
{
    private readonly ILogger<JsonLocalizationService> _logger;
    private CultureInfo _currentCulture;
    private Dictionary<string, object> _currentLanguageData = new();
    private readonly string _localesPath;

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

    public JsonLocalizationService(ILogger<JsonLocalizationService> logger)
    {
        _logger = logger;
        
        // 设置Locales路径
        _localesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Locales");
        
        // 从配置加载语言设置，默认中文
        var savedLanguage = Properties.Settings.Default.Language;
        _currentCulture = string.IsNullOrEmpty(savedLanguage) 
            ? new CultureInfo("zh-CN") 
            : new CultureInfo(savedLanguage);
        
        LoadLanguageData(_currentCulture.Name);
        ApplyCulture(_currentCulture);
        _logger.LogInformation("Localization service initialized with culture: {Culture}", _currentCulture.Name);
    }

    /// <summary>
    /// 加载语言数据
    /// </summary>
    private void LoadLanguageData(string languageCode)
    {
        try
        {
            var jsonPath = Path.Combine(_localesPath, $"{languageCode}.json");
            
            // 如果指定语言不存在，回退到英文
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("Language file not found: {Path}, falling back to en.json", jsonPath);
                jsonPath = Path.Combine(_localesPath, "en.json");
            }

            var jsonContent = File.ReadAllText(jsonPath);
            _currentLanguageData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) 
                ?? new Dictionary<string, object>();
            
            _logger.LogInformation("Loaded language data from: {Path}", jsonPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load language data for: {Language}", languageCode);
            _currentLanguageData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public void ChangeLanguage(string languageCode)
    {
        try
        {
            var newCulture = new CultureInfo(languageCode);
            LoadLanguageData(languageCode);
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
        
        // 强制刷新UI
        Application.Current.Dispatcher.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        });
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        try
        {
            var value = GetNestedValue(key);
            if (value == null)
            {
                _logger.LogWarning("Localization key not found: {Key}", key);
                return $"[{key}]";
            }

            var stringValue = value.ToString() ?? $"[{key}]";
            
            // 如果有参数，进行格式化
            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(stringValue, args);
                }
                catch (FormatException)
                {
                    _logger.LogWarning("Format string error for key: {Key}", key);
                    return stringValue;
                }
            }
            
            return stringValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get localized string for key: {Key}", key);
            return $"[{key}]";
        }
    }

    /// <summary>
    /// 获取嵌套的值 (例如 "App.Title" -> _currentLanguageData["App"]["Title"])
    /// </summary>
    private object? GetNestedValue(string key)
    {
        var parts = key.Split('.');
        object? current = _currentLanguageData;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(part, out var value))
                {
                    current = value;
                }
                else
                {
                    return null;
                }
            }
            else if (current is JsonElement element)
            {
                if (element.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        // 处理JsonElement
        if (current is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => jsonElement.ToString()
            };
        }

        return current;
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

