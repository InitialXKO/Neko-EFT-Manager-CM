using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes
{
    public class NullToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 如果值为 null，则返回 FallbackValue
            return value ?? parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

}
