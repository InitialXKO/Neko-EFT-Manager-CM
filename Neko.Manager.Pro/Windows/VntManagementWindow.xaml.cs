using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Neko.EFT.Manager.Pro.NekoVPN;
using Neko.EFT.Manager.X.Pages;
using WinUIEx;

namespace Neko.EFT.Manager.X.Windows
{
    public sealed partial class VntManagementWindow : Window
    {
        public VntManagementWindow()
        {
            this.InitializeComponent();
            
            SystemBackdrop = new DesktopAcrylicBackdrop();
            AppWindow.Resize(new(1280, 720));
            AppWindow.SetIcon("ICON.ico");
            Title = "Neko VNT Management";
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            WindowManager manager = WindowManager.Get(this);
            manager.MinHeight = 710;
            manager.MaxHeight = 710;
            manager.MinWidth = 1300;
            manager.MaxWidth = 1300;
            manager.IsResizable = false;
            manager.IsMaximizable = false;

            this.CenterOnScreen();
        }

        // TabSelectionChanged 事件处理
        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 获取当前选中的 Tab
            var selectedTab = MainTabView.SelectedItem as TabViewItem;

            // 根据选中的 Tab 加载不同的页面
            if (selectedTab != null)
            {
                if (selectedTab.Header.ToString() == "联机配置")
                {
                    ConfigManagementPageFrame.Navigate(typeof(VntConfigManagementPage)); // 加载配置管理页面
                }
                if (selectedTab.Header.ToString() == "创建房间")
                {
                    DeviceListPageFrame.Navigate(typeof(NetworkPage)); // 加载配置管理页面
                }

            }
        }
    }
}
