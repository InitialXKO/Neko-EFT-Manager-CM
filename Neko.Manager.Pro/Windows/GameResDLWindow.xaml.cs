using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Neko.EFT.Manager.X.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Pages.ServerManager;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using Neko.EFT.Manager.X.Pages.GameResManager;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameResDLWindow : Window
    {
        public StackPanel actionPanel;
        public Frame contentFrame;
        public ProgressBar actionProgressBar;
        public ProgressRing actionProgressRing;
        public TextBlock actionTextBlock;
        public DispatcherQueue DispatcherQueue { get; }
        public GameResDLWindow()
        {
            this.InitializeComponent();
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            SystemBackdrop = new DesktopAcrylicBackdrop();
            AppWindow.Resize(new(1280, 720));
            AppWindow.SetIcon("ICON.ico");
            Title = "Neko-SPT Server Manager";
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            WindowManager manager = WindowManager.Get(this);
            manager.MinHeight = 650;
            manager.MaxHeight = 650;
            manager.MinWidth = 1300;
            manager.MaxWidth = 1300;
            manager.IsResizable = false;
            manager.IsMaximizable = false;

            this.CenterOnScreen();
            NavView.SelectedItem = NavView.MenuItems.FirstOrDefault();
            ContentFrame.Navigate(typeof(GameResDLManagerPage), null, new SuppressNavigationTransitionInfo());

            // Set up variables to be accessed outside MainWindow
            actionPanel = ActionPanel;
            contentFrame = ContentFrame;
            actionProgressBar = ActionPanelBar;
            actionProgressRing = ActionPanelRing;
            actionTextBlock = ActionPanelText;

            if (App.ManagerConfig == null)
            {
                App.ManagerConfig = new();

                UntilLoaded();
            }
            // Create task to prevent the UI thread from freezing on startup?
            if (App.ManagerConfig.LookForUpdates == true)
            {
                //Task.Run(() =>
                //{
                //    LookForUpdate();
                //});
            }


            Closed += OnClosed;
        }

        public void RefreshCurrentPage()
        {
            ContentFrame.Navigate(typeof(GameResDLManagerPage));
        }
        async void UntilLoaded()
        {
            while (Content.XamlRoot == null)
            {
                await Task.Delay(100);
            }

            Utils.ShowInfoBarWithLogButton("错误", "读取配置文件时出现错误.", InfoBarSeverity.Error, 30);
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage));

                NavigationViewItem settings = (NavigationViewItem)NavView.SettingsItem;
                if (settings.InfoBadge != null)
                {
                    settings.InfoBadge = null;
                }
            }
            else
            {
                // find NavigationViewItem with Content that equals InvokedItem
                var item = sender.MenuItems.OfType<NavigationViewItem>().First(x => (string)x.Content == (string)args.InvokedItem);
                NavView_Navigate(item);
            }
        }

        private async Task ShowErrorDialog(string title, string content)
        {
            ContentDialog contentDialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        }
        private async void NavView_Navigate(NavigationViewItem item)
        {
            Debug.WriteLine($"Navigating to: {item.Tag}");
            switch (item.Tag)
            {
                //case "Play":
                //    ContentFrame.Navigate(typeof(PlayPage));
                //    break;
                case "GameRes":
                    ContentFrame.Navigate(typeof(GameResDLManagerPage));
                    break;
                case "GameSupport":
                    ContentFrame.Navigate(typeof(GameSupportManagerPage));
                    break;
                //case "ConfigServer":
                //    ContentFrame.Navigate(typeof(NekoSetServerConfigPage));
                //    break;
                //case "ServerMods":
                //    ContentFrame.Navigate(typeof(ServerModsPage));
                //    break;
                //case "ProfileManager":

                    //await ShowErrorDialog("提示", "该功能由于致命问题暂时无法使用.");
                    

            }
        }

        public void NavigateTo(Type pageType)
        {
            if (contentFrame != null)
            {
                contentFrame.Navigate(pageType);
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            //NavigationViewItem settings = (NavigationViewItem)NavView.SettingsItem;
            //FontFamily fontFamily = (FontFamily)Application.Current.Resources["BenderFont"];

            //settings.FontFamily = fontFamily;
            //settings.Content = "设置";
            //if (App.ManagerConfig?.AkiServerPath == null)
            //{
            //    settings.InfoBadge = new()
            //    {
            //        Value = 1
            //    };
            //    InstallPathTip.IsOpen = true;
            //}
        }
        private void OnClosed(object sender, WindowEventArgs args)
        {
            if (AkiServer.State == AkiServer.RunningState.RUNNING)
                AkiServer.Stop();
        }
    }
}
