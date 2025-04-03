using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.UI.Xaml;

namespace Neko.EFT.Manager.X.Converters;
public class ActivityStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Debug.WriteLine($"RaidInformation: {value}");
        if (value is JsonElement jsonElement)
        {
            try
            {
                var location = jsonElement.GetProperty("location").GetString();
                var side = jsonElement.GetProperty("side").GetString();
                var time = jsonElement.GetProperty("time").GetString();
                return $"地图: {location}, 阵营: {side}, 时间: {time}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RaidInformation parse error: {ex.Message}");
                return "无效的战局信息";
            }
        }

        return "无信息";
    }



    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

