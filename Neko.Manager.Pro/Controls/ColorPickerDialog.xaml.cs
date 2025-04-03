using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using System.Globalization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Controls
{
    public sealed partial class ColorPickerDialog : ContentDialog
    {
        public string SelectedColor;

        public ColorPickerDialog()
        {
            this.InitializeComponent();

            string color = App.ManagerConfig.ConsoleFontColorV;

            if (color != null && color != "")
            {
                byte a = byte.Parse(color.Substring(1, 2), NumberStyles.HexNumber);
                byte r = byte.Parse(color.Substring(3, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(color.Substring(5, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(color.Substring(7, 2), NumberStyles.HexNumber);

                ColorPickerControl.Color = Color.FromArgb(a, r, g, b);
            }
        }

        private void ColorPickerControlSelectButton(object sender, RoutedEventArgs e)
        {
            SelectedColor = ColorPickerControl.Color.ToString();
            Hide();
        }
        private void ColorPickerControlCancelButton(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
