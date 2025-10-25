using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CSP2.Desktop.ViewModels;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 日志级别到颜色的转换器
/// </summary>
public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UILogLevel level)
        {
            return level switch
            {
                UILogLevel.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // 红色
                UILogLevel.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // 黄色
                UILogLevel.Info => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // 蓝色
                UILogLevel.Command => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // 绿色
                UILogLevel.Debug => new SolidColorBrush(Color.FromRgb(156, 163, 175)), // 灰色
                _ => new SolidColorBrush(Color.FromRgb(226, 232, 240))
            };
        }
        return new SolidColorBrush(Color.FromRgb(226, 232, 240));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

