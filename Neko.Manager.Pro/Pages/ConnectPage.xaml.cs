using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualBasic;
using Neko.EFT.Manager.Pro.NekoVPN;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Controls;
using Neko.EFT.Manager.X.Windows;
using Novell.Directory.Ldap.Utilclass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.UI.Core;
using WinUICommunity;
using static Neko.EFT.Manager.X.Classes.Utils;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConnectPage : Page
    {
        private readonly Random rnd = new();
        private ObservableCollection<Symbol> strings { get; }
        public ConnectPage()
        {
            this.InitializeComponent();
            VNTFrame.Content = new NetworkPage();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
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
            strings = new ObservableCollection<Symbol>
    {
        Symbol.AddFriend,
        Symbol.Forward,
        Symbol.Share
    };
            DataContext = App.ManagerConfig;

            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ConnectButton.IsEnabled = false;
            }
            StartFileWatcher(); // 启动文件监视器


        }

       


        private async void StartFileWatcher()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shared_data.txt");
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath))
            {
                Filter = Path.GetFileName(filePath),
                NotifyFilter = NotifyFilters.LastWrite
            };

            watcher.Changed += OnFileChanged;
            watcher.EnableRaisingEvents = true;
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // 读取文件内容并处理
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shared_data.txt");
            string content = File.ReadAllText(filePath);

            if (content.StartsWith("SET_IP:"))
            {
                string ipAddress = content.Substring(7); // 获取 IP 地址
                string completeAddress = $"http://{ipAddress}:6969"; // 完整的地址格式

                await UpdateIpAddressAsync(completeAddress);
                
            }
        }


        private async Task UpdateIpAddressAsync(string ipAddress)
        {

            DispatcherQueue.TryEnqueue(() =>
            {
                AddressBox.Text = ipAddress;
            });

            ToastNotificationHelper.ShowNotification(
                    "通知",
                    $"服务器已由N2N MX托管为：\n {ipAddress} \n ",
                    "确认",
                    (arg) =>
                    {
                        // 执行其他操作...
                    },
                    "通知",
                    "游戏"
                );

        }


        //private void UpdateIpAddress(string ipAddress)
        //{
        //    // 更新 TextBox 显示 IP 地址
        //    ToastNotificationHelper.ShowNotification("通知", $"服务器地址已变更：\n {ipAddress} \n ", "确认", (arg) =>
        //    {
        //        // 执行其他操作...
        //    }, "通知", "设置");
        //    DispatcherQueue.TryEnqueue(() =>
        //    {
        //        AddressBox.Text = ipAddress;
        //    });
       

        //}
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
            ManagerConfig.Save((bool)RememberMeCheck.IsChecked);
            

            if (App.ManagerConfig.InstallPath == null)
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = "未配置 \"安装路径\"。转到设置配置安装路径。",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return "error";
            }

            
            if (!File.Exists(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov.exe"))
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "安装错误",
                    Content = "无法在安装路径中找到 \"EscapeFromTarkov.exe，请检查你的安装路径",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return "error";
            }

            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "输入错误",
                    Content = "地址、用户名或密码丢失.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return "error";
            }

            if (!AddressBox.Text.Contains(@"http://"))
            {
                AddressBox.Text = @"http://" + AddressBox.Text;
            }

            if (AddressBox.Text.EndsWith(@"/") || AddressBox.Text.EndsWith(@"\"))
            {
                AddressBox.Text = AddressBox.Text.Remove(AddressBox.Text.Length - 1, 1);
            }

            string returnData = await LoginToServer();
            return returnData;
        }

        /// <summary>
        /// Login to a server
        /// </summary>
        /// <returns>string</returns>
        private async Task<string> LoginToServer()
        {
            TarkovRequesting requesting = new TarkovRequesting(null, AddressBox.Text, false);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "username", UsernameBox.Text },
                { "email", UsernameBox.Text },
                { "edition", "Edge Of Darkness" },
                { "password", PasswordBox.Password },
                { "backendUrl", AddressBox.Text }
            };

            try
            {
                var returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));


                


                
                Debug.WriteLine(returnData);
                
                //Debug.WriteLine(returninfoData);
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

                    }
                    else
                    {
                        return null;
                    }
                }
                else if (returnData == "INVALID_PASSWORD")
                {
                    //await Utils.ShowInfoBar("登录失败", $"密码错误!", InfoBarSeverity.Error);
                    ToastNotificationHelper.ShowNotification("登录失败", $"密码错误!", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "错误");
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

        /// <summary>
        /// Handling Connect button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string returnData = await Connect();

            if (returnData == "error")
            {
                return;
            }
            if (string.IsNullOrEmpty(returnData))
            {
                return;
            }

            // 导航到 GameStartPage 并传递 returnData 和 serverAddress
            Debug.WriteLine(returnData);
            Frame.Navigate(typeof(GameStartPage), new Tuple<string, string, string, string, string>(returnData, AddressBox.Text, "6970", UsernameBox.Text, PasswordBox.Password));

            //await Utils.ShowInfoBar("登录", $"成功登录至 {AddressBox.Text}", InfoBarSeverity.Success);
            ToastNotificationHelper.ShowNotification("登录完成", $"成功登录至服务器：\n {AddressBox.Text} \n 游戏即将启动", "确认", (arg) =>
            {
                // 执行其他操作...
            }, "通知", "错误");


            //try
            //{
            //    var patchRunner = new ProgressReportingPatchRunner(App.ManagerConfig.InstallPath);
            //    await foreach (var result in patchRunner.PatchFiles())
            //    {
            //        if (!result.OK)
            //        {
            //            ToastNotificationHelper.ShowNotification("补丁错误", $"补丁失败：{result.Status}", "确认", (arg) =>
            //            {
            //                // 执行其他操作...
            //            }, "通知", "错误");
            //            return;
            //        }
            //    }

            //    string arguments = $"-token={returnData} -config={{\"BackendUrl\":\"{AddressBox.Text}\",\"Version\":\"live\"}}";
            //    var startInfo = new ProcessStartInfo
            //    {
            //        FileName = Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe"),
            //        Arguments = arguments,
            //        UseShellExecute = false,
            //        RedirectStandardOutput = true,
            //        RedirectStandardError = true,
            //        CreateNoWindow = true
            //    };

            //    using (Process process = Process.Start(startInfo))
            //    {
            //        using (StreamReader outputReader = process.StandardOutput)
            //        using (StreamReader errorReader = process.StandardError)
            //        {
            //            string output = await outputReader.ReadToEndAsync();
            //            string error = await errorReader.ReadToEndAsync();

            //            Debug.WriteLine("Output: " + output);
            //            Debug.WriteLine("Error: " + error);
            //        }

            //        await process.WaitForExitAsync();
            //    }

            //    if (App.ManagerConfig.CloseAfterLaunch)
            //    {
            //        Application.Current.Exit();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    ToastNotificationHelper.ShowNotification("错误", $"启动游戏时发生错误：\n {ex.Message} ", "确认", (arg) =>
            //    {
            //        // 执行其他操作...
            //    }, "通知", "错误");
            //}


            
        }

        private async void VNTButton_Click(object sender, RoutedEventArgs e)
        {

            VntManagementWindow vntManagementWindow = new VntManagementWindow();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            vntManagementWindow.Activate();


        }


    }
}
