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
using Windows.UI.Popups;
using Neko.EFT.Manager.X.Classes;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.UI;
using System.Diagnostics;
using Windows.UI;
using System.Globalization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private List<ServerConfig> serverConfigs;
        private bool isLoading;
        private AppConfig config;
        public static string ServerAddress;
        public static string ServerName;
        public static string Password;
        public static string ServerInfo;
        public LoginPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的

            config = AppConfig.Load();

            Loaded += async (sender, e) =>
            {
                await InitializeAsync();

            };
        }



        private async Task InitializeAsync()
        {
            this.ServerComboBox.PlaceholderText = "正在获取可用服务器";
            this.ServerStatus.Text = "正在获取可用服务器信息";
            this.ServerComboBox.IsHitTestVisible = false;
            this.PasswordBox.IsHitTestVisible = false;
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            await LoadServerConfigs();



        }

        private async Task LoadServerConfigs()
        {
            MainWindow window = App.m_window as MainWindow;

            ProgressRing.IsActive = true; // 显示进度指示器
            ServerStatus.Text = "正在获取...";
            InfoBar LoadinginfoBar = await Utils.ShowInfoBar("提示", "正在获取可用的服务器信息", InfoBarSeverity.Informational);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 使用您的个人访问令牌替换 "your_personal_access_token"
                    string personalAccessToken = "246294b20d4833f90c89b7d63fa4173d";
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);

                    // 获取 JSON 文件的 URL
                    string apiUrl = "https://gitee.com/api/v5/repos/Neko17-Offical/neko-server-list-repo/contents/ServerList/serverlist.json"; // 替换为实际的 API URL

                    var response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode(); // 检查响应状态代码

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var fileContent = JsonConvert.DeserializeObject<GiteeFileResponse>(responseBody);

                    var fileContentDecoded = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(fileContent.content));
                    serverConfigs = JsonConvert.DeserializeObject<List<ServerConfig>>(fileContentDecoded);

                    isLoading = true;
                    ServerComboBox.ItemsSource = serverConfigs;
                    ServerComboBox.DisplayMemberPath = "name";

                    // 加载配置文件并检查上次选择的服务器
                    var appConfig = AppConfig.Load();
                    if (!string.IsNullOrEmpty(appConfig.SelectedServer))
                    {
                        var previouslySelectedServer = serverConfigs.FirstOrDefault(s => s.name == appConfig.SelectedServer);
                        if (previouslySelectedServer != null)
                        {
                            ServerComboBox.SelectedItem = previouslySelectedServer;
                            ServerAddress = previouslySelectedServer.serverAddress;
                            //newPort = previouslySelectedServer.newPort;
                            ServerStatus.Text = previouslySelectedServer.name;
                            ServerName = previouslySelectedServer.name;
                            this.SubmitButton.IsHitTestVisible = true;
                            await UpdateServerStatus();
                        }
                    }

                    isLoading = false;
                }
            }
            catch (HttpRequestException ex)
            {
                await Utils.ShowInfoBar("错误", "网络错误：" + ex.Message, InfoBarSeverity.Error);
            }
            catch (Exception ex)
            {
                await Utils.ShowInfoBar("错误", "未知错误：" + ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                ProgressRing.IsActive = false; // 隐藏进度指示器
                Utils.HideInfoBar(LoadinginfoBar, window);
                await Utils.ShowInfoBar("完成", "已获取到可用的服务器", InfoBarSeverity.Success);
                //AddressBox.Text = "服务器可用,请选择...";
                this.ServerComboBox.PlaceholderText = "服务器可用,请选择..";
                this.ServerComboBox.IsHitTestVisible = true;
            }
        }


        private async void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (isLoading || ServerComboBox.SelectedItem == null)

                return;

            ServerConfig selectedItem = (ServerConfig)ServerComboBox.SelectedItem;
            config.SelectedServer = selectedItem.name;
            config.Save();

            ServerAddress = selectedItem.serverAddress;

            ServerStatus.Text = selectedItem.name;

            this.SubmitButton.IsEnabled = true;
            this.PasswordBox.IsHitTestVisible = true;
            ServerName = selectedItem.name;
            await UpdateServerStatus();

        }

        public class ServerConfig
        {
            public string name { get; set; }
            public string serverAddress { get; set; }
            public int newPort { get; set; }
        }

        public class GiteeFileResponse
        {
            public string content { get; set; }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {


            
            string enteredPassword = PasswordBox.Password;
            // 假设正确的密码是 "password123"
            string correctPassword = Password;

            if (enteredPassword == correctPassword)
            {
                // 导航到主应用程序界面
                Frame.Navigate(typeof(AdminPage));
            }
            else
            {
                // 显示密码错误消息
                await Utils.ShowInfoBar("错误", "您输入的密码错误.", InfoBarSeverity.Error);
            }
        }


        private async Task<(bool isServerOnline, long ping)> CheckServerStatus()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                long latency = await NekoNetTools.GetPing(ServerAddress);
                stopwatch.Stop();
                string response = await NekoNetTools.SendGetRequest(ServerAddress);

                if (response != null)
                {
                    JObject jsonObject = JObject.Parse(response);
                    Password = jsonObject["password"].ToString();
                    ServerInfo = response;


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

                ServerStatus.Text = $"{ServerName}:  在线    延迟：{latency}ms";
                ServerStatus.Foreground = customBrush;


                await ShowErrorDialog("服务器信息",ServerInfo);
                this.PasswordBox.IsHitTestVisible = true;
            }
            else
            {
                ServerStatus.Text = "服务器状态：离线";
                ServerStatus.Foreground = new SolidColorBrush(Colors.Red);
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


        private async Task ShowErrorDialog(string title, string content)
        {
            ContentDialog contentDialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };

            //contentDialog.Closed += ContentDialog_Closed;

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {

            Frame.Navigate(typeof(PlayPage));
        }
    }
}
