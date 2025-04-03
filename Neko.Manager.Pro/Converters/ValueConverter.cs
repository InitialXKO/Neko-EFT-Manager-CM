using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace Neko.EFT.Manager.X.Converters;

public class ModInfoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ModInfos modInfo)
        {
            return $"{modInfo.name}\n作者: {modInfo.author}\n版本: {modInfo.version}\n添加日期: {modInfo.dateAdded}";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
