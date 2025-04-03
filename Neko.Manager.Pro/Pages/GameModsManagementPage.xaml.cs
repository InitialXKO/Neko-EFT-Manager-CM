using System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Pages.ServerManager;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class GameModsManagementPage : Page
    {
        public Frame contentFrame;
        public GameModsManagementPage()
        {
            this.InitializeComponent();

            NotificationService.Instance.Initialize(NotificationQueue);
            contentFrame = ContentFrame;
            this.Loaded += GameLoginPage_Loaded;
        }

        private void GameLoginPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

            ContentFrame.Navigate(typeof(ModsPage), null, new SuppressNavigationTransitionInfo());

            // 选中默认加载页面对应的NavigationViewItem
            foreach (var item in ModsMNavView.MenuItems)
            {
                if (item is NavigationViewItem navigationItem && navigationItem.Tag.ToString() == "Client")
                {
                    navigationItem.IsSelected = true;
                    break;
                }
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.SelectedItem as NavigationViewItem;
            if (selectedItem != null)
            {
                var tag = selectedItem.Tag.ToString();
                switch (tag)
                {
                    case "Server":
                        ContentFrame.Navigate(typeof(ServerModsPage));
                        break;
                    case "Client":
                        ContentFrame.Navigate(typeof(ModsPage));
                        break;
                    
                }
            }
        }

        public void ShowInfoNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, bool isPersistent, TimeSpan? duration = null)
        {
            NotificationInfoBar.Title = title;
            NotificationInfoBar.Message = message;
            NotificationInfoBar.Severity = severity;
            NotificationInfoBar.IsOpen = true; // 显示通知

            if (!isPersistent) // 如果不是常驻通知
            {
                if (duration.HasValue) // 检查是否有停留时间
                {
                    _ = Task.Delay(duration.Value).ContinueWith(_ =>
                    {
                        // 在停留时间结束后关闭 InfoBar
                        NotificationInfoBar.IsOpen = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }
        public void ShowInfoNotification(string title, string message, InfoBarSeverity severity)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Severity = severity,
            };

            NotificationQueue.Show(notification);
        }
    }
}
