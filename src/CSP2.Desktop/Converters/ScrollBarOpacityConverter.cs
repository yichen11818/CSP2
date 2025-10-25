using System;
using System.Globalization;
using System.Windows.Data;

namespace CSP2.Desktop.Converters;

/// <summary>
/// 滚动条透明度转换器 - 鼠标悬停时显示，否则半透明
/// </summary>
public class ScrollBarOpacityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return 0.5;

        // values[0]: ScrollBar的IsMouseOver
        // values[1]: Thumb的IsMouseOver
        
        bool scrollBarMouseOver = values[0] is bool sb && sb;
        bool thumbMouseOver = values[1] is bool tb && tb;

        // 如果任一为true，返回完全不透明
        return (scrollBarMouseOver || thumbMouseOver) ? 1.0 : 0.5;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

