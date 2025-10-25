using System;
using System.Globalization;
using System.Windows.Data;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 进度条宽度转换器 - 根据进度值计算实际宽度
/// </summary>
public class ProgressBarWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 3)
            return 0.0;

        // values[0]: Grid的实际宽度
        // values[1]: 当前值 (Value)
        // values[2]: 最大值 (Maximum)
        
        if (values[0] is not double actualWidth ||
            values[1] is not double currentValue ||
            values[2] is not double maximum)
        {
            return 0.0;
        }

        if (maximum <= 0)
            return 0.0;

        // 计算进度百分比
        var percentage = Math.Max(0, Math.Min(1, currentValue / maximum));
        
        // 返回实际宽度
        return actualWidth * percentage;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

