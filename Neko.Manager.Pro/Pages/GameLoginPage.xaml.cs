using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Neko.EFT.Manager.X.Classes;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class GameLoginPage : Page
    {
        public Frame contentFrame;
        public GameLoginPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            contentFrame = ContentFrame;
            this.Loaded += GameLoginPage_Loaded;
        }

        private void GameLoginPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

            ContentFrame.Navigate(typeof(ConnectPage), null, new SuppressNavigationTransitionInfo());

            // 选中默认加载页面对应的NavigationViewItem
            foreach (var item in LoginNavView.MenuItems)
            {
                if (item is NavigationViewItem navigationItem && navigationItem.Tag.ToString() == "ToConnect")
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
                    case "ServerBook":
                        ContentFrame.Navigate(typeof(PlayPage));
                        break;
                    case "ToConnect":
                        ContentFrame.Navigate(typeof(ConnectPage));
                        break;
                    case "VNT":
                        ContentFrame.Navigate(typeof(VntConfigManagementPage));
                        break;
                }
            }
        }
    }
}
