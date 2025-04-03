using System;
using Microsoft.UI.Xaml.Data;

namespace Neko.EFT.Manager.X.Converters;
public class BoolToConnectionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "断开连接" : "连接配置";
        }
        return "连接配置"; // 默认值
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value.ToString() == "断开连接";
    }
}

