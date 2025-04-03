using System;
using Microsoft.UI.Xaml.Data;
using Neko.EFT.Manager.X.Classes;

namespace Neko.EFT.Manager.X.Classes;

public class VersionSelectorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ServerModInfo modInfo)
        {
            return !string.IsNullOrEmpty(modInfo.AkiVersion) ? modInfo.AkiVersion : modInfo.SPTVersion;
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
