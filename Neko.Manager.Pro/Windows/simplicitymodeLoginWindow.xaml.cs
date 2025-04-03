using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;
using Neko.EFT.Manager.X.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Windows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SimplicityModeLoginWindow : Window
{
    public SimplicityModeLoginWindow()
    {
        this.InitializeComponent();
        
        AppWindow.SetIcon("ICON.ico");
        Title = "Neko EFT Launcher X";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        WindowManager manager = WindowManager.Get(this);
        manager.MinHeight = 480;
        manager.MaxHeight = 480;
        manager.MinWidth = 860;
        manager.MaxWidth = 860;
        manager.IsResizable = false;
        manager.IsMaximizable = false;
        this.CenterOnScreen();
        RootGrid.Loaded += RootGrid_Loaded;
    }


    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(MainLoginPage));
    }

    private void MainLoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(MainLoginPage));
    }
    // 处理 "Settings" 按钮点击事件
    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
       
    }

    // 处理 "Minimize" 按钮点击事件
    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        // 将窗口最小化
       
    }

    // 处理 "Close" 按钮点击事件
    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        // 关闭窗口
        this.Close();
    }

    // 处理 "Login" 按钮点击事件
    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(MainLoginPage));
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(MainLoginSettingsPage));
    }

    
}
