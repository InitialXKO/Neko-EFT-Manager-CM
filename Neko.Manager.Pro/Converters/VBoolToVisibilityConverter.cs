using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Neko.EFT.Manager.X.Converters;

public partial class VStringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string stringValue && stringValue.Contains("已连接"))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // 由于没有双向绑定的需求，可以直接返回默认值
        return null;
    }
}
