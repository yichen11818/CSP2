using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 集合数量到可见性的转换器（用于显示空状态提示）
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            // 当数量为0时显示，否则隐藏
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

