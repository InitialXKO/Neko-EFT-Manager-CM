using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static Neko.EFT.Manager.X.Classes.Utils;
using System.Windows;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using Windows.Storage;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Windows.UI;
using Newtonsoft.Json.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Linq;
using Neko.EFT.Manager.X.Pages;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using CommunityToolkit.WinUI;
using System.Timers;
using Microsoft.UI.Xaml.Navigation;
using Neko.EFT.Manager.X.Pages.ServerManager;
using Neko.EFT.Manager.X.Windows;
using WinUIEx;
using DispatcherExtensions = CommunityToolkit.WinUI.DispatcherQueueExtensions;
using JsonSerializer = System.Text.Json.JsonSerializer;
using CommunityToolkit.WinUI.Behaviors;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayPage : Page
    {
        private DispatcherTimer timer;
        private string BackgroundImage = "ms-appx:///Assets/bg9.jpg"; // 默认背景图像
        public static string sitCfg = "com.stayintarkov.cfg";
        public static string path = @"\BepInEx\config\";
        //public static string SITPort = "56566";
        public static string SITPort = "62144";
        public static string newPort = SITPort;
        public static string ServerAddress;
        //private List<ServerConfig> serverConfigs;
        private List<ServerConfig> serverConfigs;

        private AppConfig config;
        private bool isLoading = false;
        public string akiversion;
        public string eftversion;
        public static string ServerName;
        public static string Username;
        public static string Password;
        private FileSystemWatcher _configFileWatcher;
        private FileSystemWatcher _hostIpFileWatcher;
        private System.Timers.Timer _debounceTimer;

        public PlayPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            config = AppConfig.Load();

            // 设置 ComboBox 的选择为上次保存的服务器信息
            foreach (ComboBoxItem item in ServerComboBox.Items)
            {
                if (item.Content.ToString() == config.SelectedServer)
                {
                    ServerComboBox.SelectedItem = item;
                    break;
                }
            }
            //LoadServers();
            //m_cancellationTokenSource = new CancellationTokenSource();
            //GetMatches();
            //this.PasswordBox.Background.Opacity = 0.8;
            //this.UsernameBox.Background.Opacity = 0.8;
            //this.AddressBox.Background.Opacity = 0.8;
            //this.NoticeMC.Background.Opacity = 0.8;
            //this.NoticeMC2.Background.Opacity = 0.8;
            this.PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            //this.RememberMeCheck.Checked += RememberMeCheck_Clicked;
            //this.RememberMeCheck.Unchecked += RememberMeCheck_Clicked;
            //ShowNoticeMain();
            //ShowUpdateNoticMain();
            DataContext = App.ManagerConfig;
            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(60); // 每隔15秒执行一次
            //timer.Tick += Timer_Tick;

            // 开始定时器
            //timer.Start();
            _debounceTimer = new System.Timers.Timer(500); // 设置500毫秒的抖动时间
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
            _debounceTimer.AutoReset = false; // 禁止自动重置

            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ConnectButton.IsEnabled = false;
            }

            Loaded += async (sender, e) =>
            {
                this.ConnectButton.IsHitTestVisible = false;
                await InitializeAsync();
                

            };
        }

        //private async void Timer_Tick(object sender, object e)
        //{
        //    // 定时器触发时执行更新服务器状态操作
        //    await UpdateServerStatus();
            
        //}

        private async Task InitializeAsync()
        {
            
            this.ServerComboBox.PlaceholderText = "正在获取可用服务器";
            this.ServerComboBox.IsHitTestVisible = false;
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            await LoadServerConfigs();
            InitializeFileWatchers();
            var response = await NekoNetTools.GetPlayerPresenceInfo(PlayPage.ServerAddress);
            
            Debug.WriteLine(response);

        }




        private void ShowNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, bool isPersistent, TimeSpan? duration = null)
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

        private void ShowNotification(string title, string message, InfoBarSeverity severity)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Severity = severity,
            };

            NotificationQueue.Show(notification);
        }


        private void InitializeFileWatchers()
        {
            string ConfigDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "Config");

            // 配置文件路径
            string configFilePath = Path.Combine(ConfigDirectory, "local_server_config.json");
            string hostIpFilePath = Path.Combine(ConfigDirectory, "ServerHostIP.json");

            // 检查文件是否存在
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException("找不到配置文件", configFilePath);
            }

            if (!File.Exists(hostIpFilePath))
            {
                throw new FileNotFoundException("找不到主机IP文件", hostIpFilePath);
            }

            // 初始化用于监控配置文件的 FileSystemWatcher
            _configFileWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(configFilePath),
                Filter = Path.GetFileName(configFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            // 初始化用于监控主机IP文件的 FileSystemWatcher
            _hostIpFileWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(hostIpFilePath),
                Filter = Path.GetFileName(hostIpFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            // 注册事件处理
            _configFileWatcher.Changed += OnConfigFileChanged;
            _configFileWatcher.Renamed += OnConfigFileChanged;
            _configFileWatcher.EnableRaisingEvents = true; // 启用文件监控

            _hostIpFileWatcher.Changed += OnHostIpFileChanged;
            _hostIpFileWatcher.Renamed += OnHostIpFileChanged;
            _hostIpFileWatcher.EnableRaisingEvents = true; // 启用文件监控
        }

        //private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        //{
        //    // 切换到 UI 线程
        //    await DispatcherQueue.EnqueueAsync(async () =>
        //    {
        //        // 文件更改时触发
        //        await LoadServerConfigs();
        //    });
        //}
        private bool _configFileChanged = false;
        private bool _hostIpFileChanged = false;
        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            // 每次文件更改事件触发时重启定时器
            _configFileChanged = true;
            _debounceTimer.Stop();
            _debounceTimer.Start();
            ToastNotificationHelper.ShowNotification("设置", $"联机服务器已由N2N MX 托管。\n {"你可查看服务器状态并登入服务器。"} \n ", "确认", (arg) =>
            {
                // 执行其他操作...
            }, "通知", "设置");
        }
        private void OnHostIpFileChanged(object sender, FileSystemEventArgs e)
        {
            // 每次文件更改事件触发时重启定时器
            _hostIpFileChanged = true;
            _debounceTimer.Stop();
            _debounceTimer.Start();
            ToastNotificationHelper.ShowNotification("设置", $"联机服务器已由N2N MX 配置。\n {"你可查看服务器配置并启动服务端。"} \n ", "确认", (arg) =>
            {
                // 执行其他操作...
            }, "通知", "设置");
        }

        private async 
        Task
SaveServerConfig()
        {
            try
            {
                string ConfigDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "Config");
                string serverPath = App.ManagerConfig.InstallPath;
                if (string.IsNullOrEmpty(serverPath))
                {
                    return;
                }

                // 读取 ServerHostIP.json 文件内容
                string serverHostIPFilePath = Path.Combine(ConfigDirectory, "ServerHostIP.json");
                if (!File.Exists(serverHostIPFilePath))
                {
                    throw new FileNotFoundException("ServerHostIP.json 文件不存在", serverHostIPFilePath);
                }

                string hostConfigJson = await File.ReadAllTextAsync(serverHostIPFilePath);
                var hostConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(hostConfigJson);

                // 读取 http.json 文件内容
                string configFilePath = Path.Combine(serverPath, @"Aki_Data\Server\configs\http.json");
                if (!File.Exists(configFilePath))
                {
                    configFilePath = Path.Combine(serverPath, @"SPT_Data\Server\configs\http.json");
                }

                if (!File.Exists(configFilePath))
                {
                    throw new FileNotFoundException("http.json 文件不存在", configFilePath);
                }

                string jsonContent = await File.ReadAllTextAsync(configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                // 将 ServerHostIP.json 中的值更新到 http.json 中对应的字段
                config["backendIp"] = hostConfig["HostIP"];
                config["ip"] = hostConfig["ServerListenIP"];
                config["port"] = hostConfig["ServerListenPort"];
                config["backendPort"] = hostConfig["ServerHostLoginPort"];

                // 保存更新后的内容到 http.json 文件
                jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving server config: {ex.Message}");
            }
        }



        private async void OnDebounceTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                if (_configFileChanged)
                {
                    _configFileChanged = false;
                    await LoadServerConfigs();
                    Console.WriteLine("服务器配置已重新加载。");
                }

                if (_hostIpFileChanged)
                {
                    _hostIpFileChanged = false;
                    await SaveServerConfig();
                    await Task.Delay(TimeSpan.FromMilliseconds(1.5));

                    try
                    {
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            SPTServerManager sptServerManager = new SPTServerManager();
                            sptServerManager.Activate();
                        });
                    }
                    catch (OperationCanceledException ex)
                    {
                        // 处理异常
                    }
                }
            });
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _debounceTimer.Dispose(); // 在页面离开时释放定时器
        }



        private async Task LoadServerConfigs()
        {
            MainWindow window = App.m_window as MainWindow;

            ProgressRing.IsActive = true; // 显示进度指示器
            AddressBox.Text = "正在获取...";
            //var LoadinginfoBar = await Utils.ShowInfoBar("提示", "正在获取可用的服务器信息", InfoBarSeverity.Informational);
            ShowNotification("提示", "正在获取可用的服务器信息...", InfoBarSeverity.Warning);

            try
            {
                var config = AppConfig.Load();
                var serverSource = ServerConfigManager.LoadServerSources()
                    .FirstOrDefault(s => s.Url == config.CurrentServerSourceUrl);

                if (serverSource == null)
                {
                    // 如果没有找到当前服务器源配置，则创建一个本地服务器源并保存到配置文件中
                    serverSource = new ServerSourceConfig
                    {
                        Name = "本地服务器源",
                        Url = "local_server_config.json" // 注意：这里是相对路径
                    };
                    ServerConfigManager.SaveServerSources(new List<ServerSourceConfig> { serverSource });
                }
                string BaseDirectory = AppContext.BaseDirectory;
                string ConfigDirectory = Path.Combine(BaseDirectory, "UserData", "Config");
                string fullPath = Path.Combine(ConfigDirectory, serverSource.Url);

                // 检查 URL 是否为有效的网络地址
                if (Uri.IsWellFormedUriString(serverSource.Url, UriKind.Absolute) && !File.Exists(fullPath))
                {
                    // 如果是有效的 URL，则从网络加载配置
                    serverConfigs = await ServerConfigManager.LoadServerConfigs(serverSource);
                }
                else
                {
                    // 如果是本地文件路径，则直接从文件加载配置
                    if (File.Exists(fullPath))
                    {
                        var json = File.ReadAllText(fullPath);
                        serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(json);
                    }
                    else
                    {
                        throw new FileNotFoundException("本地配置文件未找到", fullPath);
                    }
                }

                if (serverConfigs == null || serverConfigs.Count == 0)
                {
                    throw new Exception("没有可用的服务器配置");
                }

                isLoading = true;
                ServerComboBox.ItemsSource = serverConfigs;
                ServerComboBox.DisplayMemberPath = "name";

                // 加载配置文件并检查上次选择的服务器
                if (!string.IsNullOrEmpty(config.SelectedServer))
                {
                    var previouslySelectedServer = serverConfigs
                        .FirstOrDefault(s => s.name == config.SelectedServer);

                    if (previouslySelectedServer != null)
                    {
                        ServerComboBox.SelectedItem = previouslySelectedServer;
                        ServerAddress = previouslySelectedServer.serverAddress;
                        newPort = previouslySelectedServer.newPort;
                        AddressBox.Text = previouslySelectedServer.name;
                        ServerName = previouslySelectedServer.name;
                        ConnectButton.IsHitTestVisible = true;
                        ConnectButton.IsEnabled = true;
                        await UpdateServerStatus();
                    }
                }

                isLoading = false;
            }
            catch (HttpRequestException ex)
            {
                //await Utils.ShowInfoBar("错误", "网络错误：" + ex.Message, InfoBarSeverity.Error);
                ShowNotification("错误", "网络错误：" + ex.Message, InfoBarSeverity.Error, false, TimeSpan.FromSeconds(5));
                ServerComboBox.IsHitTestVisible = true;
            }
            catch (Exception ex)
            {
                //await Utils.ShowInfoBar("错误", "未知错误：" + ex.Message, InfoBarSeverity.Error);
                ShowNotification("错误", "未知错误：" + ex.Message, InfoBarSeverity.Error, false, TimeSpan.FromSeconds(5));
                ServerComboBox.IsHitTestVisible = true;
            }
            finally
            {
                ProgressRing.IsActive = false; // 隐藏进度指示器
                //Utils.HideInfoBar(LoadinginfoBar, window);
                //await Utils.ShowInfoBar("完成", "已获取到可用的服务器", InfoBarSeverity.Success);
                ShowNotification("完成", "已获取到可用的服务器", InfoBarSeverity.Success, false, TimeSpan.FromSeconds(5));
                ServerComboBox.IsHitTestVisible = true;
                ServerComboBox.PlaceholderText = "完成，请选择服务器";
                await UpdateServerStatus();
            }
        }




        private async void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading || ServerComboBox.SelectedItem == null)
                return;

            ServerConfig selectedItem = (ServerConfig)ServerComboBox.SelectedItem;
            var config = AppConfig.Load();
            config.SelectedServer = selectedItem.name;
            config.Save();

            ServerAddress = selectedItem.serverAddress;
            newPort = selectedItem.newPort;

            AddressBox.Text = selectedItem.name;
            ServerName = selectedItem.name;
            this.ConnectButton.IsHitTestVisible = true;
            await UpdateServerStatus();
        }



        private CancellationTokenSource m_cancellationTokenSource;
        private Task GetMatchesTask { get; set; }
        private bool StopAllTasks = false;
        private Dictionary<string, object>[] m_Matches { get; set; }

        private async Task UpdateSITPort()
        {
            try
            {
                if (App.ManagerConfig.InstallPath == null)
                {
                    await Utils.ShowInfoBar("错误", "游戏目录为空，请选择正确的游戏目录。", InfoBarSeverity.Error);
                    return;
                }

                string sitConfigFilePath = Path.Combine(App.ManagerConfig.InstallPath + path + sitCfg);

                if (!File.Exists(sitConfigFilePath))
                {
                    await Utils.ShowInfoBar("错误", "找不到 SIT 配置文件，请检查游戏目录是否正确。", InfoBarSeverity.Error);
                    return;
                }

                string[] lines = await File.ReadAllLinesAsync(sitConfigFilePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("SITPort = "))
                    {
                        lines[i] = "SITPort = " + newPort;  // 更新 SITPort 的值为新端口
                        break;
                    }
                }

                await File.WriteAllLinesAsync(sitConfigFilePath, lines);  // 将更新后的内容写回文件
                await Utils.ShowInfoBar("端口变更", $"当前 SIT 端口已更改为 '{newPort}'");
            }
            catch (Exception ex)
            {
                await Utils.ShowInfoBar("错误", $"更新 SIT 端口时发生错误：{ex.Message}", InfoBarSeverity.Error);
            }
        }


        private async Task ShowNoticeMain()
        {
            try
            {
                HttpClient client = new HttpClient();
                string notice = await client.GetStringAsync("https://gitee.com/Neko17-Offical/neko-manager/raw/master/NOTICE-MAIN");
                NoticeM.Text = notice;
            }
            catch (Exception ex)
            {
                // 处理异常，例如打印错误信息

                await Utils.ShowInfoBar("网络错误", $"获取公告失败，请检查你的网络.", InfoBarSeverity.Error);

            }
        }


        


        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0 || string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"先填写所有字段."
                });
                
            }
            else if (AddressBox.Text.Length > 0)
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"尝试连接 {AddressBox.Text} 并启动游戏."
                });
                ConnectButton.IsEnabled = true;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0 || string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"填写所有内容."
                });
                
            }
            else if (AddressBox.Text.Length > 0)
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"尝试连接 {AddressBox.Text} 并启动游戏."
                });
                ConnectButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Connect to a server
        /// </summary>
        private async Task<string> Connect()
        {
            // 保存配置
            ManagerConfig.Save((bool)RememberMeCheck.IsChecked);

            // 检查安装路径是否存在
            if (string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                await ShowErrorDialog("配置错误", "未配置 \"安装路径\"。转到设置配置安装路径。");
                return "error";
            }

            //// 检查是否存在所需文件
            //if (!File.Exists(Path.Combine(App.ManagerConfig.InstallPath, @"BepInEx\plugins\StayInTarkov.dll")))
            //{
            //    await ShowErrorDialog("安装错误", "无法找到 \"StayInTarkov.dll\"。连接前请安装 SIT.");
            //    return "error";
            //}

            if (!File.Exists(Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe")))
            {
                await ShowErrorDialog("安装错误", "无法在安装路径中找到 \"EscapeFromTarkov.exe，请检查你的安装路径");
                return "error";
            }

            // 检查输入是否有效
            //if (string.IsNullOrEmpty(AddressBox.Text) || string.IsNullOrEmpty(UsernameBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
            //{
            //    await ShowErrorDialog("输入错误", "地址、用户名或密码丢失.");
            //    return "error";
            //}

            // 格式化地址
            string serverAddress = ServerAddress;
            if (!serverAddress.StartsWith("http://"))
            {
                serverAddress = "http://" + serverAddress;
            }

            if (serverAddress.EndsWith("/") || serverAddress.EndsWith("\\"))
            {
                serverAddress = serverAddress.Remove(serverAddress.Length - 1, 1);
            }

            // 获取版本信息并检查更新
            //await CheckAndUpdateVersion();

            // 执行登录操作
            string returnData = await LoginToServer();
            return returnData;
        }

        private async Task CheckAndUpdateVersion()
        {
            if (App.ManagerConfig.Neko_lookForSITUpdates)
            {
                string latestVersion = await Task.Run(() => Utils.GetGiteeReleaseVersion());
                string locationVersion = Utils.GetLocationVersion();

                if (latestVersion != locationVersion)
                {
                    await ShowErrorDialog("需要更新", "您的SIT组件与云端版本不符.");

                    UpdateResult updateResult = await Utils.SITUpdateVoid();

                    switch (updateResult)
                    {
                        case UpdateResult.Success:
                            // 成功消息
                            //await Utils.ShowInfoBar("更新成功", "Neko 组件已成功更新。", InfoBarSeverity.Success);
                            break;
                        case UpdateResult.NetworkError:
                            // 网络错误消息

                            await Utils.ShowInfoBar("获取更新失败", "网络错误，请稍后再试。", InfoBarSeverity.Error);
                            break;
                        case UpdateResult.OtherError:
                            // 其他错误消息
                            await Utils.ShowInfoBar("获取更新失败", "发生其他错误，请检查日志。", InfoBarSeverity.Error);
                            break;
                        default:
                            break;
                    }
                }
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



        /// <summary>
        /// Login to a server
        /// </summary>
        /// <returns>string</returns>
        private async Task<string> LoginToServer()
        {
            Username = UsernameBox.Text;
            Password = PasswordBox.Password;
            TarkovRequesting requesting = new TarkovRequesting(null, ServerAddress, false);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "username", UsernameBox.Text },
                { "email", UsernameBox.Text },
                { "edition", "Edge Of Darkness" },
                { "password", PasswordBox.Password },
                { "backendUrl", ServerAddress }
            };

            try
            {
                //var returnData = await requesting.PostJsonAsync("/launcher/profile/login-Neko", System.Text.Json.JsonSerializer.Serialize(data));
                var returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));

                // If failed, attempt to register
                //if (returnData == "FAILED")
                //{
                //    ContentDialog contentDialog = new()
                //    {
                //        XamlRoot = Content.XamlRoot,
                //        Title = "登录失败",
                //        Content = $"未找到您的账号，请联系管理员。",
                //        CloseButtonText = "确定"
                //    };

                //    await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                //    return "error";

                //}

                if (returnData == "FAILED")
                {
                    ContentDialog createAccountDialog = new()
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "账号不存在",
                        Content = "未找到您的帐户，您想用这些凭据注册一个新帐户吗？",
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = "确定",
                        CloseButtonText = "取消"
                    };

                    ContentDialogResult msgBoxResult = await createAccountDialog.ShowAsync(ContentDialogPlacement.InPlace);
                    if (msgBoxResult == ContentDialogResult.Primary)
                    {
                        SelectEditionDialog selectWindow = new()
                        {
                            XamlRoot = Content.XamlRoot
                        };
                        await selectWindow.ShowAsync();
                        string edition = selectWindow.edition;

                        if (edition != null)
                            data["edition"] = edition;

                        returnData = await requesting.PostJsonAsync("/launcher/profile/register", System.Text.Json.JsonSerializer.Serialize(data));
                        returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));
                    }
                    else
                    {
                        return null;
                    }
                }
                else if(returnData == "INVALID_PASSWORD")
                {
                    await Utils.ShowInfoBar("登录失败", $"密码错误!", InfoBarSeverity.Error);
                    return "error";
                }
                else if (returnData.Contains("UNHANDLE"))
                {
                    await Utils.ShowInfoBar("错误", "不匹配的服务端!", InfoBarSeverity.Error);
                    return "error";
                }


                return returnData;
            }
            catch (System.Net.WebException webEx)
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "登录失败",
                    Content = $"无法与服务器通信\n{webEx.Message}",
                    CloseButtonText = "确定"
                };

                Loggy.LogToFile("Login Error: " + webEx);

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return "error";
            }
            catch (Exception ex)
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "登录失败",
                    Content = $"无法与服务器通信\n{ex.Message}",
                    CloseButtonText = "确定"
                };

                Loggy.LogToFile("Login Error: " + ex);

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return "error";
            }
        }


        private async Task<(bool isServerOnline, long ping)> CheckServerStatus()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                long latency = await NekoNetTools.GetPing(ServerAddress);
                stopwatch.Stop();

                using (var cts = new CancellationTokenSource(4000)) // 设置4秒超时
                {
                    // 获取服务器版本
                    akiversion = await GetServerVersion(cts.Token);
                    // 获取客户端版本
                    eftversion = await GetClientVersion(cts.Token);

                    this.ServerVersion.Text = $"服务器运行版本:  {akiversion}";
                    this.ServerEFTVersion.Text = $"可用客户端版本:  {eftversion}";

                    if (latency >= 0) // 如果延迟值有效，表示服务器在线
                    {
                        return (true, latency);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("检查服务器状态时发生超时。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查服务器状态时发生错误：{ex.Message}");
            }

            return (false, -1);
        }

        private async Task<string> GetServerVersion(CancellationToken cancellationToken)
        {
            string response = await NekoNetTools.GetServerVersion(ServerAddress).WithCancellation(cancellationToken);
            Debug.WriteLine("Server Version: " + response);
            return response?.Trim('"') ?? "Unknown"; // 去除引号
        }

        private async Task<string> GetClientVersion(CancellationToken cancellationToken)
        {
            string response = await NekoNetTools.GetEFTVersion(ServerAddress).WithCancellation(cancellationToken);
            Debug.WriteLine("Compatible Tarkov Version: " + response);
            return response?.Trim('"') ?? "Unknown"; // 去除引号
        }

        private async Task UpdateServerStatus()
        {
            (bool isServerOnline, long latency) = await CheckServerStatus(); // 修改此处的 latency 类型为 long

            if (isServerOnline)
            {
                Color customColor = ConvertHexToColor("#27AE60");

                // 使用该颜色创建 SolidColorBrush
                SolidColorBrush customBrush = new SolidColorBrush(customColor);

                // 设置前景色
                ServerStatusTextBlock.Text = $"服务器状态：在线    延迟：{latency}ms";
                ServerStatusTextBlock.Foreground = customBrush;
            }
            else
            {
                ServerStatusTextBlock.Text = "服务器状态：离线";
                ServerStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        public static Color ConvertHexToColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = 255;
            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            return Color.FromArgb(a, r, g, b);
        }


        /// <summary>
        /// Handling Connect button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectButton.IsEnabled = false;
            ServerStatusTextBlock.Text = "正在检查服务器可用性...";
            ServerStatusTextBlock.Foreground = new SolidColorBrush(Colors.Yellow);
            (bool isServerOnline, long latency) = await CheckServerStatus();

            if (isServerOnline)
            {
                Color customColor = ConvertHexToColor("#27AE60");

                // 使用该颜色创建 SolidColorBrush
                SolidColorBrush customBrush = new SolidColorBrush(customColor);
                ServerStatusTextBlock.Text = $"服务器状态：在线    延迟：{latency}ms";
                ServerStatusTextBlock.Foreground = customBrush;
                ConnectButton.IsEnabled = true;
                try
                {
                    string returnData = await Connect();

                    if (returnData == "error" || string.IsNullOrEmpty(returnData))
                    {
                        // 处理连接错误或返回数据为空的情况
                        return;
                    }

                    ShowNotification("登录成功", $"成功登录至 {AddressBox.Text}", InfoBarSeverity.Success, false, TimeSpan.FromSeconds(5));
                    //await Utils.ShowInfoBar("登录成功", $"成功登录至 {AddressBox.Text}", InfoBarSeverity.Success);

                    // 导航到 GameStartPage 并传递 returnData 和 serverAddress
                    Frame.Navigate(typeof(GameStartPage), new Tuple<string, string, string, string, string>(returnData, ServerAddress, newPort, UsernameBox.Text, PasswordBox.Password));
                }
                catch (Exception ex)
                {
                    // 处理异常情况，例如显示错误消息或记录错误日志
                    await Utils.ShowInfoBar("错误", "发生错误：" + ex.Message, InfoBarSeverity.Error);
                }
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "登录失败",
                    Content = $"服务器不可用，请联系管理员",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);

                ServerStatusTextBlock.Text = "服务器状态：离线";
                ServerStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                ConnectButton.IsEnabled = true;
            }
        }

        private async void N2NMXButton_Click(object sender, RoutedEventArgs e)
        {
            VntManagementWindow vntManagementWindow = new VntManagementWindow();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            vntManagementWindow.Activate();

        }
    }

}
//public class ServerConfig
//{
//    public string name { get; set; }
//    public string serverAddress { get; set; }
//    public string newPort { get; set; }
//}

