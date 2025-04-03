using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;
using PInvoke = Windows.Win32.PInvoke;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using Windows.Graphics.Display;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using WinUIEx;
using Microsoft.UI.Xaml.Media.Imaging;

internal static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
    public const int WS_EX_LAYERED = 0x00080000;
    public const uint LWA_ALPHA = 0x00000002;
    public static readonly IntPtr HWND_TOP = IntPtr.Zero;
    public const uint SWP_SHOWWINDOW = 0x0040;
}

namespace Neko.EFT.Manager.X.Windows
{
    public sealed partial class SplashScreenWindow : Window
    {

        private readonly string[] _backgroundImages =
        {
            "Assets/acg/001.jpg",
            "Assets/acg/002.jpg",
            "Assets/acg/004.jpg",
            "Assets/acg/005.jpg",
            "Assets/acg/003.jpg",
            "Assets/acg/006.jpg",
            "Assets/acg/bg01.jpg",
            "Assets/acg/bg02.jpg",
            "Assets/acg/bg03.jpg",
            "Assets/bg1.jpg",
            "Assets/bg2.jpg",
            "Assets/bg3.jpg",
            "Assets/bg4.jpg",
            "Assets/bg5.jpg"
        };
        public SplashScreenWindow()
        {
            this.InitializeComponent();
            LoadRandomBackground();
            // 获取窗口句柄

            AppWindow.Resize(new(1000, 400));
            var hwnd = WindowNative.GetWindowHandle(this);
            Debug.WriteLine($"Window Handle: {hwnd}");
            this.CenterOnScreen();
            // 设置窗口样式
            SetWindowStyles(hwnd);
        }

        private void LoadRandomBackground()
        {
            // Get a random image path from the array
            Random random = new Random();
            string selectedImagePath = _backgroundImages[random.Next(_backgroundImages.Length)];

            // Set the image source
            BackgroundImage.ImageSource = new BitmapImage(new Uri($"ms-appx:///{selectedImagePath}"));
        }
        public void UpdateStatus(string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StatusSTextBlock.Text = message;
            });
        }
        private void SetWindowStyles(IntPtr hwnd)
        {
            try
            {
                // 获取当前窗口样式
                var style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
                // 设置无边框样式
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, (style & ~NativeMethods.WS_OVERLAPPEDWINDOW));

                // 设置透明背景
                var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_LAYERED);
                // 注释掉透明背景设置，避免与背景显示冲突
                // bool result = NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 255, NativeMethods.LWA_ALPHA);
                // Debug.WriteLine($"SetLayeredWindowAttributes Result: {result}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in SetWindowStyles: {ex.Message}");
            }
        }


    }
}
