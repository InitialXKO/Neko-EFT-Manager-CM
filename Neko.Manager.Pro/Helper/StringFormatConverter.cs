using Microsoft.UI.Xaml.Data;
using System;

namespace Neko.EFT.Manager.X.Helper
{
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string format)
            {
                return string.Format(format, value);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
