using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class MatchNPage : Page
    {
        public Frame contentFrame;
        public MatchNPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            ContentFrame.Navigate(typeof(FikaMatchPage), null, new SuppressNavigationTransitionInfo());
            contentFrame = ContentFrame;
            this.Loaded += MatchNPage_Loaded;
        }

        private void MatchNPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // 默认加载页面
            ContentFrame.Navigate(typeof(FikaMatchPage), null, new SuppressNavigationTransitionInfo());

            // 选中默认加载页面对应的NavigationViewItem
            foreach (var item in MatchNavView.MenuItems)
            {
                if (item is NavigationViewItem navigationItem && navigationItem.Tag.ToString() == "FikaMatch")
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
                    case "SITMatch":
                        ContentFrame.Navigate(typeof(MatchPage));
                        break;
                    case "FikaMatch":
                        ContentFrame.Navigate(typeof(FikaMatchPage));
                        break;
                }
            }
        }
    }
}
