using System.Globalization;
using System.Windows.Data;
using CSP2.Core.Models;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 服务器状态到文本的转换器
/// </summary>
public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ServerStatus status)
        {
            return status switch
            {
                ServerStatus.Running => "运行中",
                ServerStatus.Stopped => "已停止",
                ServerStatus.Starting => "启动中",
                ServerStatus.Stopping => "停止中",
                ServerStatus.Crashed => "已崩溃",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

