using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Neko.EFT.Manager.X.Converters;
public class RaidInformationFormatter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string jsonString)
        {
            try
            {
                // 解析 JSON 字符串
                var jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                if (jsonObject != null)
                {
                    var location = jsonObject.GetValueOrDefault("location", "未知地点");
                    var side = jsonObject.GetValueOrDefault("side", "未知阵营");
                    var time = jsonObject.GetValueOrDefault("time", "未知时间");
                    Debug.WriteLine($"地图: {location}, 阵营: {side}, 时间: {time}");
                    return $"地图: {location}, 阵营: {side}, 时间: {time}";
                    
                }
            }
            catch
            {
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

