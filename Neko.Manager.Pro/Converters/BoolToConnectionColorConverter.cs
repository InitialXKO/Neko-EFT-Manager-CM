using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Neko.EFT.Manager.X.Converters;
public class BoolToConnectionColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isConnected)
        {
            return new SolidColorBrush(isConnected ? Colors.LightCoral : Colors.LightSeaGreen);
        }
        return new SolidColorBrush(Colors.LightSeaGreen); // 默认颜色
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is SolidColorBrush brush && brush.Color == Colors.LightCoral;
    }
}

