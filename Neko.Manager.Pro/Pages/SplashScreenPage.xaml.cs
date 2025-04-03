using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplashScreenPage : Page
    {
        private readonly string[] _backgroundImages =
        {
            "Assets/bg1.jpg",
            "Assets/bg2.jpg",
            "Assets/bg3.jpg",
            "Assets/bg4.jpg",
            "Assets/bg5.jpg"
        };
        public SplashScreenPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            LoadRandomBackground();

        }

        private void LoadRandomBackground()
        {
            // Get a random image path from the array
            Random random = new Random();
            string selectedImagePath = _backgroundImages[random.Next(_backgroundImages.Length)];

            // Set the image source
            BackgroundImage.Source = new BitmapImage(new Uri($"ms-appx:///{selectedImagePath}"));
        }
    }
}
