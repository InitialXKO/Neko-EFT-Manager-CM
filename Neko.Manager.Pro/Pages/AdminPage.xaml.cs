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
using System.Security.Principal;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Thickness = Microsoft.UI.Xaml.Thickness;
using CornerRadius = Microsoft.UI.Xaml.CornerRadius;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AdminPage : Page
    {
        private DispatcherTimer timer;
        private string BackgroundImage = "ms-appx:///Assets/bg9.jpg"; // 默认背景图像
        public static string sitCfg = "com.stayintarkov.cfg";
        public static string path = @"\BepInEx\config\";
        //public static string SITPort = "56566";
        public static string SITPort = "62144";
        public static string newPort = SITPort;
        public static string ServerAddress;
        
        private List<ServerConfigAdmin> serverConfigs;
        private AppConfig config;
        private bool isLoading = false;
        public string akiversion;
        public string eftversion;
        public AdminPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的

            config = AppConfig.Load();

            // 设置 ComboBox 的选择为上次保存的服务器信息
            //foreach (ComboBoxItem item in ServerComboBox.Items)
            //{
            //    if (item.Content.ToString() == config.SelectedServer)
            //    {
            //        ServerComboBox.SelectedItem = item;
            //        break;
            //    }
            //}
            //LoadServers();
            //m_cancellationTokenSource = new CancellationTokenSource();
            //GetMatches();
            this.PasswordBox.Background.Opacity = 0.8;
            this.UsernameBox.Background.Opacity = 0.8;
            //this.AddressBox.Background.Opacity = 0.8;
            this.NoticeMC.Background.Opacity = 0.8;
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


            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ConnectButton.IsEnabled = false;
            }

            Loaded += async (sender, e) =>
            {
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
            LoadAccountData();
            //this.ServerComboBox.PlaceholderText = "正在获取可用服务器";
            //this.ServerComboBox.IsHitTestVisible = false;
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            await UpdateServerStatus();
            ServerAddress = LoginPage.ServerAddress;
            //await LoadServerConfigs();



        }

        private async void LoadAccountData()
        {
            
            string jsonResponse = await NekoNetTools.GetProfile(PlayPage.ServerAddress);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var accounts = JsonConvert.DeserializeObject<List<Account>>(jsonResponse);

                foreach (var account in accounts)
                {
                    Border accountBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Gray),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(10),
                        Margin = new Thickness(5)
                    };

                    StackPanel accountPanel = new StackPanel();

                    accountPanel.Children.Add(new TextBlock { Text = $"账户名: {account.username}", FontSize = 16 });
                    accountPanel.Children.Add(new TextBlock { Text = $"昵称: {account.nickname}", FontSize = 16 });
                    accountPanel.Children.Add(new TextBlock { Text = $"阵营: {account.side}", FontSize = 16 });
                    accountPanel.Children.Add(new TextBlock { Text = $"当前等级: {account.currlvl}", FontSize = 16 });
                    accountPanel.Children.Add(new TextBlock { Text = $"当前经验值: {account.currexp}", FontSize = 16 });
                    accountPanel.Children.Add(new TextBlock { Text = $"下一等级所需经验: {account.nextlvl}", FontSize = 16 });
                    accountPanel.Children.Add(new TextBlock { Text = $"最高等级: {account.maxlvl}", FontSize = 16 });

                    accountBorder.Child = accountPanel;
                    AccountDataPanel.Children.Add(accountBorder);
                }
            }
        }



        //private async Task LoadServerConfigs()
        //{
        //    MainWindow window = App.m_window as MainWindow;

        //    //ProgressRing.IsActive = true; // 显示进度指示器
        //    AddressBox.Text = "正在获取...";
        //    InfoBar LoadinginfoBar = await Utils.ShowInfoBar("提示", "正在获取可用的服务器信息", InfoBarSeverity.Informational);

        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            // 使用您的个人访问令牌替换 "your_personal_access_token"
        //            string personalAccessToken = "246294b20d4833f90c89b7d63fa4173d";
        //            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);

        //            // 获取 JSON 文件的 URL
        //            string apiUrl = "https://gitee.com/api/v5/repos/Neko17-Offical/neko-server-list-repo/contents/ServerList/serverlist.json"; // 替换为实际的 API URL

        //            var response = await client.GetAsync(apiUrl);
        //            response.EnsureSuccessStatusCode(); // 检查响应状态代码

        //            var responseBody = await response.Content.ReadAsStringAsync();
        //            var fileContent = JsonConvert.DeserializeObject<GiteeFileResponseAdmin>(responseBody);

        //            var fileContentDecoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(fileContent.content));
        //            serverConfigs = JsonConvert.DeserializeObject<List<ServerConfigAdmin>>(fileContentDecoded);

        //            isLoading = true;
        //            ServerComboBox.ItemsSource = serverConfigs;
        //            ServerComboBox.DisplayMemberPath = "name";
        //            isLoading = false;
        //        }
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        await Utils.ShowInfoBar("错误", "网络错误：" + ex.Message, InfoBarSeverity.Error);
        //    }
        //    catch (Exception ex)
        //    {
        //        await Utils.ShowInfoBar("错误", "未知错误：" + ex.Message, InfoBarSeverity.Error);
        //    }
        //    finally
        //    {
        //        ProgressRing.IsActive = false; // 隐藏进度指示器
        //        Utils.HideInfoBar(LoadinginfoBar, window);
        //        await Utils.ShowInfoBar("完成", "已获取到可用的服务器", InfoBarSeverity.Success);
        //        AddressBox.Text = "服务器可用,请选择...";
        //        this.ServerComboBox.PlaceholderText = "服务器可用,请选择..";
        //        this.ServerComboBox.IsHitTestVisible = true;

        //    }
        //}

        //private async void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{

        //    if (isLoading || ServerComboBox.SelectedItem == null)
        //        return;

        //    ServerConfig selectedItem = (ServerConfig)ServerComboBox.SelectedItem;
        //    config.SelectedServer = selectedItem.name;
        //    config.Save();

        //    ServerAddress = selectedItem.serverAddress;
        //    newPort = selectedItem.newPort;

        //    AddressBox.Text = selectedItem.name;

        //    await UpdateServerStatus();

        //}




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


        //private async Task ShowNoticeMain()
        //{
        //    try
        //    {
        //        HttpClient client = new HttpClient();
        //        string notice = await client.GetStringAsync("https://gitee.com/Neko17-Offical/neko-manager/raw/master/NOTICE-MAIN");
        //        NoticeM.Text = notice;
        //    }
        //    catch (Exception ex)
        //    {
        //        // 处理异常，例如打印错误信息

        //        await Utils.ShowInfoBar("网络错误", $"获取公告失败，请检查你的网络.", InfoBarSeverity.Error);

        //    }
        //}





        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0 || string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"先填写所有字段."
                });
                ConnectButton.IsEnabled = false;
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
                ConnectButton.IsEnabled = false;
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

            // 检查是否存在所需文件
            if (!File.Exists(Path.Combine(App.ManagerConfig.InstallPath, @"BepInEx\plugins\StayInTarkov.dll")))
            {
                await ShowErrorDialog("安装错误", "无法找到 \"StayInTarkov.dll\"。连接前请安装 SIT.");
                return "error";
            }

            if (!File.Exists(Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe")))
            {
                await ShowErrorDialog("安装错误", "无法在安装路径中找到 \"EscapeFromTarkov.exe，请检查你的安装路径");
                return "error";
            }

            // 检查输入是否有效
            if (string.IsNullOrEmpty(AddressBox.Text) || string.IsNullOrEmpty(UsernameBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
            {
                await ShowErrorDialog("输入错误", "地址、用户名或密码丢失.");
                return "error";
            }

            // 格式化地址
            string serverAddress = AddressBox.Text;
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
                var returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));

                // If failed, attempt to register
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
                        // Register
                        returnData = await requesting.PostJsonAsync("/launcher/profile/register", System.Text.Json.JsonSerializer.Serialize(data));
                        // Login attempt after register
                        returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));
                        await Utils.ShowInfoBar("登录信息", returnData, InfoBarSeverity.Error);

                    }
                    else
                    {
                        return null;
                    }
                }
                else if (returnData == "INVALID_PASSWORD")
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
                long latency = await NekoNetTools.GetPing(LoginPage.ServerAddress);
                stopwatch.Stop();
                string response = await NekoNetTools.SendGetRequest(LoginPage.ServerAddress);
                if (response != null)
                {
                    JObject jsonObject = JObject.Parse(response);
                    akiversion = jsonObject["akiversion"].ToString();
                    eftversion = jsonObject["eftversion"].ToString();
                    this.ServerVersion.Text = $"服务器运行版本:  {akiversion}";
                    this.ServerEFTVersion.Text = $"可用客户端版本:  {eftversion}";
                }
                else
                {
                    await Utils.ShowInfoBar("错误", "发生错误：", InfoBarSeverity.Error);
                }


                // 在这里可以继续处理解析后的JSON数据
                if (latency >= 0) // 如果延迟值有效，表示服务器在线
                {
                    return (true, latency);
                }
                else
                {
                    return (false, -1); // 服务器离线，返回 -1 作为延迟值
                }


            }
            catch (Exception ex)
            {
                // 处理异常情况
                Console.WriteLine($"检查服务器状态时发生错误：{ex.Message}");
                return (false, -1); // 服务器离线，返回 -1 作为延迟值
            }


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
                AddressBox.Text = LoginPage.ServerName;


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
                ServerStatusTextBlock.Text = $"服务器状态：在线    延迟：{latency}ms";
                ServerStatusTextBlock.Foreground = new SolidColorBrush(Colors.LightGreen);
                ConnectButton.IsEnabled = true;
                try
                {
                    string returnData = await Connect();

                    if (returnData == "error" || string.IsNullOrEmpty(returnData))
                    {
                        // 处理连接错误或返回数据为空的情况
                        return;
                    }

                    await Utils.ShowInfoBar("注册成功", $"成功完成注册登录至 {AddressBox.Text}", InfoBarSeverity.Success);

                    // 导航到 GameStartPage 并传递 returnData 和 serverAddress
                    //Frame.Navigate(typeof(GameStartPage), new Tuple<string, string, string>(returnData, ServerAddress, newPort));
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


    }

}
public class ServerConfigAdmin
{
    public string name { get; set; }
    public string serverAddress { get; set; }
    public string newPort { get; set; }
}
public class GiteeFileResponseAdmin
{
    public string content { get; set; }
}

public class Account
{
    public string username { get; set; }
    public string nickname { get; set; }
    public string side { get; set; }
    public int currlvl { get; set; }
    public int currexp { get; set; }
    public int prevexp { get; set; }
    public int nextlvl { get; set; }
    public int maxlvl { get; set; }
    public AkiData akiData { get; set; }
}

public class AkiData
{
    public string version { get; set; }
    public List<Mod> mods { get; set; }
}

public class Mod
{
    public string author { get; set; }
    public long dateAdded { get; set; }
    public string name { get; set; }
    public string version { get; set; }
}
