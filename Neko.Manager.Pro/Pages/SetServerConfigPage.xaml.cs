using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Newtonsoft.Json.Linq;
using SharpCompress.Common;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using Windows.UI.Core;
using Windows.Networking.Connectivity;
using Windows.Networking;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SIT.Manager.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetServerConfigPage : Page
    {
        public SetServerConfigPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            string akipath = @"\Aki_Data\Server\configs\";
            string akiCfg = @"http.json";
            string akiconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;
            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopiCfg = @"coopConfig.json";
            string sicoopconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopiCfg;
            string sicoopSITiCfg = @"SITConfig.json";
            string sicoopSITconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopSITiCfg;

            // Different versions of Neko has different names
            if (!File.Exists(akiconfigPath))
            {
                akiCfg = "http.json";
                akiconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;
            }

            if (File.Exists(akiconfigPath))
            {
                string jsonContent = System.IO.File.ReadAllText(akiconfigPath);
                JObject json = JObject.Parse(jsonContent);
                string serverip = json["ip"].ToString();
                ServerIPTextBox.Text = serverip;

                string serverport = json["port"].ToString();
                ServerPortTextBox.Text = serverport;
            }


            if (!File.Exists(sicoopconfigPath))
            {
                sicoopiCfg = "coopConfig.json";
                sicoopconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;
            }

            //if (File.Exists(sicoopconfigPath))
            //{
            //    string jsonContent = System.IO.File.ReadAllText(sicoopconfigPath);
            //    JObject json = JObject.Parse(jsonContent);
            //    string SITexternalIP = json["externalIP"].ToString();
            //    SITCoopIPTextBox.Text = SITexternalIP;

            //    string SITCoopPort = json["webSocketPort"].ToString();
            //    SITCoopPortTextBox.Text = SITCoopPort;
            //}
            if (File.Exists(sicoopSITconfigPath))
            {
                string jsonContent = System.IO.File.ReadAllText(sicoopSITconfigPath);
                JObject json = JObject.Parse(jsonContent);

                ShowPlayerNameCheckBox.IsChecked = json["showPlayerNameTags"].ToObject<bool>();
                ShowPlayerNameWhenVisibleCheckBox.IsChecked = json["showPlayerNameTagsOnlyWhenVisible"].ToObject<bool>();
            }
            this.Unloaded += SetServerConfigPage_Unloaded;
        }

        private async void SaveServerIPButton_Click(object sender, RoutedEventArgs e)
        {
            string akipath = @"\Aki_Data\Server\configs\";
            string akiCfg = @"http.json";
            string akiconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;

            // Different versions of Neko has different names
            if (!File.Exists(akiconfigPath))
            {
                akiCfg = "http.json";
                akiconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;
            }

            if (!File.Exists(akiconfigPath))
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到'{akiCfg}'。确保已选择AKI服务端安装路径.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }

            string jsonContent = System.IO.File.ReadAllText(akiconfigPath);
            JObject json = JObject.Parse(jsonContent);

            string newIp = ServerIPTextBox.Text.Trim();  // 获取用户输入的新的 IP 值

            json["ip"] = newIp;  // 将 IP 的值更新为用户输入的值

            System.IO.File.WriteAllText(akiconfigPath, json.ToString());  // 将更新后的内容写回文件
            await Utils.ShowInfoBar("服务端IP更改", $"服务端 IP 已修改为 '{newIp}'");
        }

        private async void SaveServerPortButton_Click(object sender, RoutedEventArgs e)
        {
            string akipath = @"\Aki_Data\Server\configs\";
            string akiCfg = @"http.json";
            string akiconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;

            // Different versions of Neko has different names
            if (!File.Exists(akiconfigPath))
            {
                akiCfg = "http.json";
                akiconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;
            }

            if (!File.Exists(akiconfigPath))
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到'{akiCfg}'。确保已选择AKI服务端安装路径.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }

            string jsonContent = System.IO.File.ReadAllText(akiconfigPath);
            JObject json = JObject.Parse(jsonContent);

            string newPort = ServerPortTextBox.Text.Trim();  // 获取用户输入的新的 IP 值

            json["port"] = newPort;  // 将 IP 的值更新为用户输入的值

            System.IO.File.WriteAllText(akiconfigPath, json.ToString());  // 将更新后的内容写回文件
            await Utils.ShowInfoBar("服务端端口更改", $"服务器 端口 已修改为 '{newPort}'");
        }

        private async void GetIP_Click(object sender, RoutedEventArgs e)
        {
            string localIP = await GetLocalIPv4AsyncForActiveConnection();

            if (!string.IsNullOrEmpty(localIP))
            {
                ServerIPTextBox.Text = localIP;
                await Utils.ShowInfoBar("获取本机IP", $"已获取到当前正在使用的IP '{localIP}'");
            }
            else
            {
                // 无法获取IP时的处理逻辑
            }
        }

        private async Task<string> GetLocalIPv4AsyncForActiveConnection()
        {
            string localIP = string.Empty;

            try
            {
                ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();

                if (profile != null)
                {
                    NetworkAdapter adapter = profile.NetworkAdapter;

                    if (adapter != null)
                    {
                        var hostnames = NetworkInformation.GetHostNames();

                        foreach (var hn in hostnames)
                        {
                            if (hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId == adapter.NetworkAdapterId)
                            {
                                if (hn.Type == HostNameType.Ipv4)
                                {
                                    localIP = hn.CanonicalName;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex = new Exception();
            }

            return localIP;
        }





        private async void SetServerConfigPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // 检查是否已保存修改
            string akipath = @"\Aki_Data\Server\configs\";
            string akiCfg = @"http.json";
            string akiconfigPath = App.ManagerConfig.AkiServerPath + akipath + akiCfg;
            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopiCfg = @"coopConfig.json";
            string sicoopconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopiCfg;

            if (File.Exists(akiconfigPath))
            {
                string jsonContent = System.IO.File.ReadAllText(akiconfigPath);
                JObject json = JObject.Parse(jsonContent);
                string serverip = json["ip"].ToString();
                string serverport = json["port"].ToString();

                if (ServerIPTextBox.Text != serverip || ServerPortTextBox.Text != serverport)
                {
                    ContentDialog saveDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "保存更改",
                        Content = "你还没有保存更改。你想在离开页面之前保存吗？",
                        PrimaryButtonText = "保存",
                        CloseButtonText = "不保存"
                    };

                    ContentDialogResult result = await saveDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // 如果用户选择保存，那么保存更改
                        SaveServerIPButton_Click(null, null);
                        SaveServerPortButton_Click(null, null);
                        
                    }
                }
            }

            if (File.Exists(sicoopconfigPath))
            {
                string jsonContent = System.IO.File.ReadAllText(sicoopconfigPath);
                JObject json = JObject.Parse(jsonContent);
                string SITexternalIP = json["externalIP"].ToString();
                string SITCoopPort = json["webSocketPort"].ToString();

                if (SITCoopIPTextBox.Text != SITexternalIP || SITCoopPortTextBox.Text != SITCoopPort)
                {
                    ContentDialog saveDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "保存更改",
                        Content = "你还没有保存更改。你想在离开页面之前保存吗？",
                        PrimaryButtonText = "保存",
                        CloseButtonText = "不保存"
                    };

                    ContentDialogResult result = await saveDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // 如果用户选择保存，那么保存更改
                        SaveSITCoopIPButton_Click(null, null);
                        SaveSITCoopPortButton_Click(null, null);

                    }
                }
            }

        }
        private async void SaveSITCoopIPButton_Click(object sender, RoutedEventArgs e)
        {
            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopiCfg = @"coopConfig.json";
            string sicoopconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopiCfg;
            if (!File.Exists(sicoopconfigPath))
            {
                sicoopiCfg = "coopConfig.json";
                sicoopconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopiCfg;
            }

            if (!File.Exists(sicoopconfigPath))
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到'{sicoopiCfg}'。确保已在服务端安装SITCoop MOD，并已启动服务端一次.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }

            string jsonContent = System.IO.File.ReadAllText(sicoopconfigPath);
            JObject json = JObject.Parse(jsonContent);

            string newExIP = SITCoopIPTextBox.Text.Trim();  // 获取用户输入的新的 IP 值

            json["externalIP"] = newExIP;  // 将 IP 的值更新为用户输入的值

            System.IO.File.WriteAllText(sicoopconfigPath, json.ToString());  // 将更新后的内容写回文件
            await Utils.ShowInfoBar("SITcoop IP更改", $"SITcoop MOD 外部IP已修改为 '{newExIP}'");

        }

        private async void SaveSITCoopPortButton_Click(object sender, RoutedEventArgs e)
        {
            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopiCfg = @"coopConfig.json";
            string sicoopconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopiCfg;
            if (!File.Exists(sicoopconfigPath))
            {
                sicoopiCfg = "coopConfig.json";
                sicoopconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopiCfg;
            }

            if (!File.Exists(sicoopconfigPath))
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到'{sicoopiCfg}'。确保已在服务端安装SITCoop MOD，并已启动服务端一次.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }

            string jsonContent = System.IO.File.ReadAllText(sicoopconfigPath);
            JObject json = JObject.Parse(jsonContent);

            string newExPort = SITCoopPortTextBox.Text.Trim();  // 获取用户输入的新的 IP 值

            json["webSocketPort"] = newExPort;  // 将 IP 的值更新为用户输入的值

            System.IO.File.WriteAllText(sicoopconfigPath, json.ToString());  // 将更新后的内容写回文件
            await Utils.ShowInfoBar("SITcoop端口更改", $"SITCoop端口已修改为 '{newExPort}'");

        }

        private void ShowPlayerNameCheckBox_Checked(object sender, RoutedEventArgs e)
        {

            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopSITiCfg = @"SITConfig.json";
            string sicoopSITconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopSITiCfg;
            // 读取JSON文件
            string jsonContent = System.IO.File.ReadAllText(sicoopSITconfigPath);
            JObject json = JObject.Parse(jsonContent);

            // 修改值
            json["showPlayerNameTags"] = true;

            // 保存回文件
            System.IO.File.WriteAllText(sicoopSITconfigPath, json.ToString());
        }

        private void ShowPlayerNameCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopSITiCfg = @"SITConfig.json";
            string sicoopSITconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopSITiCfg;
            // 读取JSON文件
            string jsonContent = System.IO.File.ReadAllText(sicoopSITconfigPath);
            JObject json = JObject.Parse(jsonContent);

            // 修改值
            json["showPlayerNameTags"] = false;

            // 保存回文件
            System.IO.File.WriteAllText(sicoopSITconfigPath, json.ToString());
        }

        private void ShowPlayerNameWhenVisibleCheckBox_Checked(object sender, RoutedEventArgs e)
        {

            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopSITiCfg = @"SITConfig.json";
            string sicoopSITconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopSITiCfg;
            // 读取JSON文件
            string jsonContent = System.IO.File.ReadAllText(sicoopSITconfigPath);
            JObject json = JObject.Parse(jsonContent);

            // 修改值
            json["showPlayerNameTagsOnlyWhenVisible"] = true;

            // 保存回文件
            System.IO.File.WriteAllText(sicoopSITconfigPath, json.ToString());
        }

        private void ShowPlayerNameWhenVisibleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            string sicooppath = @"\user\mods\SITCoop\config\";
            string sicoopSITiCfg = @"SITConfig.json";
            string sicoopSITconfigPath = App.ManagerConfig.AkiServerPath + sicooppath + sicoopSITiCfg;
            // 读取JSON文件
            string jsonContent = System.IO.File.ReadAllText(sicoopSITconfigPath);
            JObject json = JObject.Parse(jsonContent);

            // 修改值
            json["showPlayerNameTagsOnlyWhenVisible"] = false;

            // 保存回文件
            System.IO.File.WriteAllText(sicoopSITconfigPath, json.ToString());
        }
        

    }
}
