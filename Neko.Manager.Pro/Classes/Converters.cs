using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Neko.EFT.Manager.X.Classes
{

    public class CompatibilityStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is CompatibilityStatus status)
            {
                return status switch
                {
                    CompatibilityStatus.Compatible => "兼容",
                    CompatibilityStatus.Incompatible => "不兼容",
                    _ => "未知",
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public class CompatibilityStatusToColorConverter : IValueConverter
    {


        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is CompatibilityStatus status)
            {
                return status switch
                {
                    CompatibilityStatus.Compatible => CreateAcrylicBrush(Colors.SpringGreen),
                    CompatibilityStatus.Incompatible => CreateAcrylicBrush(Colors.OrangeRed),
                    _ => CreateAcrylicBrush(Colors.LightCyan),
                };
            }
            return CreateAcrylicBrush(Colors.LightCyan);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private SolidColorBrush CreateAcrylicBrush(Color color)
        {
            var acrylicColor = Color.FromArgb(153, color.R, color.G, color.B); // 153 is roughly 60% opacity
            var brush = new SolidColorBrush(acrylicColor);

            return brush;
        }
    }


}
