using System;
using System.Globalization;
using System.Windows.Data;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 本地化转换器 - 用于XAML中的动态本地化
/// </summary>
public class LocalizationConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string key)
            return value;

        try
        {
            var localizedValue = Resources.Strings.ResourceManager.GetString(key, Resources.Strings.Culture);
            return localizedValue ?? $"[{key}]";
        }
        catch
        {
            return $"[{key}]";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 本地化扩展 - 简化XAML中的使用
/// </summary>
public class LocalizationExtension : System.Windows.Markup.MarkupExtension
{
    public string Key { get; set; }

    public LocalizationExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        try
        {
            var localizedValue = Resources.Strings.ResourceManager.GetString(Key, Resources.Strings.Culture);
            return localizedValue ?? $"[{Key}]";
        }
        catch
        {
            return $"[{Key}]";
        }
    }
}

