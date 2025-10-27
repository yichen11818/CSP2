using System.Globalization;
using System.Windows.Data;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 将布尔值转换为状态文本的转换器
/// </summary>
public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isInstalled)
        {
            return isInstalled ? "✅ 已安装" : "❌ 未安装";
        }
        return "❓ 未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

