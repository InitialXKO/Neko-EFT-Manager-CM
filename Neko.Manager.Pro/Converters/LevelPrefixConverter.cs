using Microsoft.UI.Xaml.Data;
using System;

namespace Neko.EFT.Manager.X.Converters;

public partial class LevelPrefixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int level)
        {
            return $"Lv. {level}";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
