using System;
using System.Globalization;
using System.Windows.Data;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 百分比宽度转换器 - 根据百分比计算实际宽度
/// </summary>
public class PercentageWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double actualWidth)
            return 0.0;

        if (parameter is not string percentStr)
            return 0.0;

        if (!double.TryParse(percentStr, out double percent))
            return 0.0;

        // 将百分比转换为 0-1 的小数
        var percentage = Math.Max(0, Math.Min(100, percent)) / 100.0;
        
        // 返回实际宽度
        return actualWidth * percentage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

