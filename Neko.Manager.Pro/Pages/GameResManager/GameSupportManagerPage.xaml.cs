using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages.GameResManager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameSupportManagerPage : Page
    {
        public GameSupportManagerPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            //this.StackPanelTools.Background.Opacity = 0.9;
        }

        private async void OpenEFTFolderButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (App.ManagerConfig.AkiServerPath == null)
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = "未配置 \"安装路径\"。转到设置配置安装路径.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }
            else
            {
                if (Directory.Exists(App.ManagerConfig.AkiServerPath))
                    Process.Start("explorer.exe", App.ManagerConfig.AkiServerPath);
            }
        }

        private async void OpenServerModFolderButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (App.ManagerConfig.AkiServerPath == null)
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = "安装路径未配置。转到设置配置安装路径.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }

            string dirPath = App.ManagerConfig.AkiServerPath + @"\user\mods\";
            if (Directory.Exists(dirPath))
                Process.Start("explorer.exe", dirPath);
            else
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到指定路径。是否已初始服务端？",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }
        }

        private async void OpenSITConfigButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string path = @"\user\mods\SITCoop\config\";
            string sitCfg = @"coopConfig.json";

            // Different versions of Neko has different names
            if (!File.Exists(App.ManagerConfig.AkiServerPath + path + sitCfg))
            {
                sitCfg = "coopConfig.json";
            }

            if (!File.Exists(App.ManagerConfig.AkiServerPath + path + sitCfg))
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到'{sitCfg}'。确保已安装 SIT，并已启动游戏一次.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }

            Process.Start("explorer.exe", App.ManagerConfig.AkiServerPath + path + sitCfg);
        }

        private async void OpenEFTLogButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "错误",
                Content = $"该功能正在开发中，暂不可用.",
                CloseButtonText = "确定"
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            return;
        }
        private async void OpenlinkButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string url = "https://awa.neko17s.xyz/index.php/2024/01/09/%e9%80%83%e7%a6%bb%e5%a1%94%e7%a7%91%e5%a4%ab%e7%a6%bb%e7%ba%bf%e7%89%88%e5%b1%80%e5%9f%9f%e7%bd%91%e8%81%94%e6%9c%ba%e8%b5%84%e6%ba%90%e4%b8%8b%e8%bd%bd%ef%bc%88%e6%8c%81%e7%bb%ad%e6%9b%b4%e6%96%b0/"; // 打开网页

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "错误",
                    Content = "无法打开更新页面.",
                    CloseButtonText = "确定"
                };
            }




            //    // 创建一个新的窗口
            //    var newWindow = new Window();
            //    newWindow.Title = "EFT资源";

            //    // 创建一个 TabView 控件来容纳两个页面
            //    var tabView = new TabView();

            //    // 使用 HttpClient 来获取 txt 文件内容
            //    var httpClient = new HttpClient();
            //    var txt1 = await httpClient.GetStringAsync("https://gitee.com/Neko17-Offical/tkfclient-downlaod/raw/master/link.txt");
            //    var txt2 = await httpClient.GetStringAsync("https://gitee.com/Neko17-Offical/stayintarkov-patcher/raw/master/link.txt");

            //    // 为每个 txt 文件创建一个 TabViewItem
            //    var tabViewItem1 = CreateTabViewItem(txt1, "EFT游戏本体");
            //    var tabViewItem2 = CreateTabViewItem(txt2, "多人补丁资源");

            //    // 将 TabViewItem 添加到 TabView 控件中
            //    tabView.TabItems.Add(tabViewItem1);
            //    tabView.TabItems.Add(tabViewItem2);

            //    // 将 TabView 控件添加到新窗口的内容中
            //    newWindow.Content = tabView;

            //    // 显示新窗口
            //    newWindow.SystemBackdrop = new DesktopAcrylicBackdrop();
            //    newWindow.Activate();
            //    newWindow.SetIsResizable(false);
            //    newWindow.SetIsMaximizable(false);
            //}

            //private TabViewItem CreateTabViewItem(string txt, string header)
            //{
            //    // 创建一个 StackPanel 控件来容纳链接
            //    var stackPanel = new StackPanel
            //    {
            //        HorizontalAlignment = HorizontalAlignment.Stretch,
            //        VerticalAlignment = VerticalAlignment.Stretch,

            //    };

            //    // 将 txt 文件内容按行分割
            //    var lines = txt.Split('\n');

            //    // 为每两行创建一个 TextBlock 和一个 HyperlinkButton 控件
            //    for (int i = 0; i < lines.Length; i += 2)
            //    {
            //        if (i + 1 >= lines.Length) break;

            //        var title = lines[i];
            //        var href = lines[i + 1];

            //        // 创建一个 TextBlock 控件来显示标题
            //        var textBlock = new TextBlock
            //        {
            //            Text = title,
            //            HorizontalAlignment = HorizontalAlignment.Center,
            //            VerticalAlignment = VerticalAlignment.Center
            //        };
            //        stackPanel.Children.Add(textBlock);

            //        // 创建一个 HyperlinkButton 控件来显示链接
            //        var hyperlinkButton = new HyperlinkButton
            //        {
            //            NavigateUri = new Uri(href),
            //            Content = href,  // 将超链接的显示内容设置为链接本身
            //            HorizontalAlignment = HorizontalAlignment.Center,
            //            VerticalAlignment = VerticalAlignment.Center
            //        };
            //        stackPanel.Children.Add(hyperlinkButton);
            //    }

            //    // 创建一个 ScrollViewer 控件来使内容可以滚动
            //    var scrollViewer = new ScrollViewer
            //    {
            //        Content = stackPanel
            //    };

            //    // 创建一个 TabViewItem 控件并设置其 Header 和 Content
            //    var tabViewItem = new TabViewItem
            //    {
            //        Header = header,
            //        Content = scrollViewer
            //    };

            //    return tabViewItem;
        }
    }
}
