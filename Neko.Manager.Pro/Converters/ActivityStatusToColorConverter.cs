using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;



namespace Neko.EFT.Manager.X.Converters;

public class ActivityStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string activityStatus)
        {
            return activityStatus switch
            {
                "战局中" => new SolidColorBrush(Microsoft.UI.Colors.PaleVioletRed),
                "主菜单" => new SolidColorBrush(Microsoft.UI.Colors.ForestGreen),
                "服务器请求错误" => new SolidColorBrush(Microsoft.UI.Colors.DarkRed),
                
                _ => new SolidColorBrush(Microsoft.UI.Colors.Orange)
            };
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Gray); // 默认颜色
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
