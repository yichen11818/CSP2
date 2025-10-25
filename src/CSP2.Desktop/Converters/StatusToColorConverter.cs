using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using CSP2.Core.Models;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 服务器状态到颜色的转换器
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ServerStatus status)
        {
            return status switch
            {
                ServerStatus.Running => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // 绿色
                ServerStatus.Stopped => new SolidColorBrush(Color.FromRgb(107, 114, 128)), // 灰色
                ServerStatus.Starting => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // 蓝色
                ServerStatus.Stopping => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // 黄色
                ServerStatus.Crashed => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // 红色
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
        }
        return new SolidColorBrush(Color.FromRgb(107, 114, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

