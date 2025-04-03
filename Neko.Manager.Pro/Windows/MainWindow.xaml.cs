using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Pages;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.WindowManagement;
using WinUIEx;
using Microsoft.Win32;
using System.Text;
using System.Management;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.IO;
using Microsoft.UI.Dispatching;
using Microsoft.UI;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // todo: make public
        public StackPanel actionPanel;
        public Frame contentFrame;
        public ProgressBar actionProgressBar;
        public ProgressRing actionProgressRing;
        public TextBlock actionTextBlock;
        private VPNChecker vpnChecker;
        private DispatcherTimer notificationTimer;


        public MainWindow()
        {
            this.InitializeComponent();
            ProModeToggle.IsOn = App.ManagerConfig.ProModeEnabled;
            ProModeTextBlock.Foreground = ProModeToggle.IsOn
                ? new SolidColorBrush(Colors.LightSeaGreen)
                : new SolidColorBrush(Colors.DarkOrange);
            SystemBackdrop = new DesktopAcrylicBackdrop();
            this.SITUpdateButton.Click += SITUpdateButton_Click;
            Setting2Page.InstallPathChanged += OnInstallPathChanged;
            GameAOPage.GOAInstallPathChanged += OnGOAInstallPathChanged;

            // Customize Window
            AppWindow.Resize(new(1280, 720));
            AppWindow.SetIcon("ICON.ico");
            Title = "Neko EFT Manager X Pro";
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

            // Initialize App.ManagerConfig if null
            if (App.ManagerConfig == null)
            {
                App.ManagerConfig = new();
                UntilLoaded();
            }

            // Determine which page to navigate to based on ProModeEnabled
            if (App.ManagerConfig.ProModeEnabled)
            {
                // Navigate to the Pro mode page
                NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(item => item.Tag?.ToString() == "ProModePage");
                ContentFrame.Navigate(typeof(GameAOProPage), null, new SuppressNavigationTransitionInfo());
            }
            else
            {
                // Navigate to the standard mode page
                NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(item => item.Tag?.ToString() == "GameAOPage");
                ContentFrame.Navigate(typeof(GameAOPage), null, new SuppressNavigationTransitionInfo());
            }

            DisplayUniqueId();

            // Set up variables to be accessed outside MainWindow
            actionPanel = ActionPanel;
            contentFrame = ContentFrame;
            actionProgressBar = ActionPanelBar;
            actionProgressRing = ActionPanelRing;
            actionTextBlock = ActionPanelText;

            CreateOrUpdateServerHostIp();
            VersionTextBlock.Text = $"Release: {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";
            string permissionMode = PermissionChecker.GetPermissionMode();
            permissionModetext.Text = $"PM：{permissionMode}";

            Closed += OnClosed;
        }


        private static readonly string ConfigDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "Config");

        public GameAOPage GameAOPageS { get; }


        private void ProModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (ProModeToggle.IsOn)
            {
                ProModeTextBlock.Foreground = new SolidColorBrush(Colors.LightSeaGreen); // 开启时颜色
                App.ManagerConfig.ProModeEnabled = true; // 更新配置
                ContentFrame.Navigate(typeof(GameAOProPage));
            }
            else
            {
                ProModeTextBlock.Foreground = new SolidColorBrush(Colors.DarkOrange); // 关闭时颜色
                App.ManagerConfig.ProModeEnabled = false; // 更新配置
                ContentFrame.Navigate(typeof(GameAOPage));
            }

            // 保存配置以确保状态持久化
            ManagerConfig.Save();
        }


        private void CreateOrUpdateServerHostIp()
        {
            string jsonFilePath = Path.Combine(ConfigDirectory, "ServerHostIP.json");

            // 定义默认配置
            var defaultHostConfig = new
            {
                HostIP = "127.0.0.1",             // 默认主机IP
                ServerListenIP = "0.0.0.0",      // 默认服务端监听IP
                ServerListenPort = 6969,         // 默认服务端监听端口
                ServerHostLoginPort = 6969       // 默认服务端主机登录端口
            };

            // 检查文件是否存在
            if (!File.Exists(jsonFilePath))
            {
                // 文件不存在，创建默认配置文件
                File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(defaultHostConfig, Formatting.Indented));

            }
            else
            {
                // 文件存在，不做任何操作

            }
        }

        private string GetCpuId()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
                ManagementObjectCollection results = searcher.Get();

                foreach (ManagementObject obj in results)
                {
                    return obj["ProcessorId"].ToString(); // 获取第一个 CPU 的 ProcessorId
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("无法获取 CPU ID: " + ex.Message);
            }

            return null;
        }

        // 使用 SHA-256 对 CPU ID 进行哈希处理
        private string HashCpuId(string cpuId)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(cpuId);
                byte[] hashBytes = sha256.ComputeHash(bytes);

                // 将字节数组转换为十六进制字符串
                StringBuilder hashString = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hashString.Append(b.ToString("x2"));
                }

                return hashString.ToString();
            }
        }

        // 生成唯一标识符
        public string GenerateUniqueId()
        {
            string cpuId = GetCpuId();
            if (!string.IsNullOrEmpty(cpuId))
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(cpuId);
                    byte[] hashBytes = sha256.ComputeHash(bytes);

                    // 将哈希值转为大整数
                    ulong hashInt = BitConverter.ToUInt64(hashBytes, 0); // 取前8字节作为整数

                    // 取模压缩为10位数字
                    ulong uniqueId = hashInt % 10000000000; // 取模以保证结果为10位数

                    return uniqueId.ToString("D10"); // 返回10位ID
                }
            }
            else
            {
                // 无法获取 CPU ID，返回 null 或使用其他回退机制
                return null;
            }
        }

        // 显示唯一标识符
        public void DisplayUniqueId()
        {
            string uniqueId = GenerateUniqueId();
            if (uniqueId != null)
            {
                UserIdTextBlock.Text = ($"UID: {uniqueId}");
            }
            else
            {
                Console.WriteLine("无法生成 UID");
            }
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            vpnChecker = new VPNChecker();
            CheckForProxyOrVPN();
        }

        private void CheckForProxyOrVPN()
        {
            if (vpnChecker.IsUsingProxyOrVPN())
            {

                ToastNotificationHelper.ShowNotification("错误", "系统开启了代理或者VPN软件，继续使用会导致客户端无法连接至服务端，请关闭代理后再试", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
            }
        }




        private void OnInstallPathChanged()
        {

            // 更新安装路径状态
            

            UpdateNavigationViewState();
            

        }

        private void UpdateNavigationViewState()
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                ContentFrame.Navigate(typeof(Setting2Page));

                if (NavView == null || App.ManagerConfig == null)
                {
                    // 如果为空，直接返回，不执行后续操作
                    return;
                }

                // 检查 NavView 的 SettingsItem 是否为 null
                if (NavView.SettingsItem is NavigationViewItem settings)
                {
                    if (string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
                    {
                        settings.InfoBadge = new InfoBadge()
                        {
                            Value = 1
                        };
                        InstallPathTip.IsOpen = true;

                        // 禁用除设置以外的其他选项
                        SetNavViewItemsEnabled(false);
                    }
                    else
                    {
                        settings.InfoBadge = null;
                        InstallPathTip.IsOpen = false;

                        // 启用所有选项
                        SetNavViewItemsEnabled(true);
                    }
                }
            });

        }

        private void OnGOAInstallPathChanged()
        {

            // 更新安装路径状态


            UpdateuUIState();


        }

        private void UpdateuUIState()
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                ContentFrame.Navigate(typeof(GameAOPage));

                
            });

        }




        async void UntilLoaded()
        {
            while (Content.XamlRoot == null)
            {
                await Task.Delay(100);
            }

            Utils.ShowInfoBarWithLogButton("错误", "读取配置文件时出现错误.", InfoBarSeverity.Error, 30);
        }

        /// <summary>
        /// Look for an update for Neko.EFT.Manager.X
        /// </summary>
        public async void LookForUpdate()
        {
            try
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                string latestVersion = await Utils.utilsHttpClient.GetStringAsync(@"https://gitee.com/Neko17-Offical/neko-manager/raw/master/VERSION");
                latestVersion = latestVersion.Trim();

                Version newVersion = new(latestVersion);

                int compare = currentVersion.CompareTo(newVersion);

                if (compare < 0)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        UpdateInfoBar.Title = "Manager更新";
                        UpdateInfoBar.Message = "Neko EFT Manager X有可用更新："+ latestVersion;
                        UpdateInfoBar.Severity = InfoBarSeverity.Informational;

                        UpdateInfoBar.IsOpen = true;

                        await Task.Delay(TimeSpan.FromSeconds(30));

                        UpdateInfoBar.IsOpen = false;
                    });
                }
            }
            catch (Exception ex)
            {
                Loggy.LogToFile("LookForUpdate: " + ex.Message);
                Utils.ShowInfoBarWithLogButton("错误", "无法获取更新.", InfoBarSeverity.Error);
                return;
            }
        }

        /// <summary>
        /// Used to navigate the NavView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(Setting2Page));

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

        /// <summary>
        /// Used to set the FontFamily on Settings Button as it has no property in the class. Also adds an InfoBadge to make user aware of the page on first launch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationViewItem settings = (NavigationViewItem)NavView.SettingsItem;
            FontFamily fontFamily = (FontFamily)Application.Current.Resources["BenderFont"];

            settings.FontFamily = fontFamily;
            settings.Content = "设置";

            if (App.ManagerConfig?.InstallPath == null || string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                settings.InfoBadge = new InfoBadge()
                {
                    Value = 1
                };
                InstallPathTip.IsOpen = true;

                // 禁用除设置以外的其他选项
                SetNavViewItemsEnabled(false);
            }
            else
            {
                // 启用所有选项
                SetNavViewItemsEnabled(true);
            }
        }

        private void SetNavViewItemsEnabled(bool isEnabled)
        {
            foreach (var menuItem in NavView.MenuItems)
            {
                if (menuItem is NavigationViewItem navItem && navItem.Tag.ToString() != "Settings")
                {
                    navItem.IsEnabled = isEnabled;
                }
            }
        }


        /// <summary>
        /// Navigates the NavView
        /// </summary>
        /// <param name="item"></param>
        private void NavView_Navigate(NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "Play":
                    // 检查是否启用了专业模式
                    if (IsProfessionalModeEnabled()) // 假设这是一个检查专业模式的方法
                    {
                        ContentFrame.Navigate(typeof(GameAOProPage)); // 导航到专业模式页面
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(GameAOPage)); // 导航到普通模式页面
                    }
                    break;


                case "Game":
                    ContentFrame.Navigate(typeof(GameLoginPage));
                    break;
                case "Server":
                    ContentFrame.Navigate(typeof(ServerPage));
                    break;
                case "Tools":
                    ContentFrame.Navigate(typeof(ToolsPage));
                    break;
                case "Mods":
                    ContentFrame.Navigate(typeof(GameModsManagementPage));
                    break;
                case "Admin":
                    ContentFrame.Navigate(typeof(LoginPage));
                    break;
                case "Match":
                    ContentFrame.Navigate(typeof(MatchNPage));
                    break;
                case "Client":
                    ContentFrame.Navigate(typeof(ClientManagerPage));
                    break;
                case "Community":
                    ContentFrame.Navigate(typeof(ModsAToolsCommunityPage));
                    break;
                case "GameVersionManagement":
                    ContentFrame.Navigate(typeof(ResourceDownloadPage));
                    break;
                case "GameVersionLibrary":
                    ContentFrame.Navigate(typeof(GameVersionLibraryPage));
                    break;
                    



            }
        }

        private static bool IsProfessionalModeEnabled()
        {
            // 从配置中获取专业模式启用状态
            return App.ManagerConfig?.ProModeEnabled ?? false; // 假设从 ManagerConfig 中获取状态
        }


        /// <summary>
        /// Handles the Update button on the notification when an update is available
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            //string dir = AppContext.BaseDirectory;

            //if (File.Exists(dir + @"\Neko.EFT.Manager.X.Updater.exe"))
            //{
            //    Process.Start(dir + @"\Neko.EFT.Manager.X.Updater.exe");
            //    Application.Current.Exit();
            //}

            //string url = "https://gitee.com/Neko17-Offical/neko-manager/releases/latest/"; // 打开网页

            //try
            //{
            //    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            //}
            //catch
            //{
            //    ContentDialog contentDialog = new()
            //    {
            //        XamlRoot = Content.XamlRoot,
            //        Title = "错误",
            //        Content = "无法打开更新页面.",
            //        CloseButtonText = "确定"
            //    };
            //}
        }

        /// <summary>
        /// Handles the window closing event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnClosed(object sender, WindowEventArgs args)
        {
            if (AkiServer.State == AkiServer.RunningState.RUNNING)
                AkiServer.Stop();
        }

        private void SITUpdateButton_Click(object sender, RoutedEventArgs e)
        {

            Utils.SITUpdateButton_Click(sender, e);

        }

        private void InstallPathTip_CloseButtonClick(TeachingTip sender, object args)
        {
            // 导航到设置页面
            ContentFrame.Navigate(typeof(SettingsPage));

            // 获取 NavigationView 控件
            var navView = this.NavView;

            if (navView != null)
            {
                // 选中设置选项
                var settingsItem = navView.SettingsItem as NavigationViewItem;
                if (settingsItem != null)
                {
                    navView.SelectedItem = settingsItem;

                    // 清除通知徽标
                    if (settingsItem.InfoBadge != null)
                    {
                        settingsItem.InfoBadge = null;
                    }
                }
            }
        }

    }
}
