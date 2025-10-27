using System.Globalization;
using System.Windows.Data;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 将布尔值转换为安装按钮文本的转换器
/// </summary>
public class BoolToInstallTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isInstalled)
        {
            return isInstalled ? "🔄 重新安装" : "📥 安装";
        }
        return "📥 安装";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

