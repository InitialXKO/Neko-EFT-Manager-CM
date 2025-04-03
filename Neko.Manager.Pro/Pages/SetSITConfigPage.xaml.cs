using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Neko.EFT.Manager.X.Classes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetSITConfigPage : Page

    {
        private bool isSaved = true;  // 添加一个标志来跟踪是否有未保存的更改
        public SetSITConfigPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            string path = @"\BepInEx\config\";
            string sitCfg = @"sit.Core.cfg";

            // Different versions of Neko has different names
            if (!File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
            {
                sitCfg = "com.sit.core.cfg";
            }

            if (File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
            {
                string filePath = App.ManagerConfig.InstallPath + path + sitCfg;
                string[] lines = System.IO.File.ReadAllLines(filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("SITPort = "))
                    {
                        string currentPort = lines[i].Split('=')[1].Trim();  // 获取当前的 SITPort 值
                        SITPortTextBox.Text = currentPort;  // 将 SITPort 的值显示在 SITPortTextBox 中
                    }
                    else if (lines[i].Contains("PlayerStateTickRateInMS = "))
                    {
                        string currentPSTR = lines[i].Split('=')[1].Trim();  // 获取当前的 PlayerStateTickRateInMS 值
                        PSTRTextBox.Text = currentPSTR;  // 将 PlayerStateTickRateInMS 的值显示在 PSTRTextBox 中
                    }
                }
            }
            this.Unloaded += Page_Unloaded;
        }

        private async void SaveSITPortButton_Click(object sender, RoutedEventArgs e)
        {
            string path = @"\BepInEx\config\";
            string sitCfg = @"sit.Core.cfg";

            // Different versions of Neko has different names
            if (!File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
            {
                sitCfg = "com.sit.core.cfg";
            }

            if (File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
            {
                string filePath = App.ManagerConfig.InstallPath + path + sitCfg;
                string[] lines = File.ReadAllLines(filePath);

                string newPort = SITPortTextBox.Text.Trim();  // 获取用户输入的新的 SITPort 值

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("SITPort = "))
                    {
                        lines[i] = "SITPort = " + newPort;  // 将 SITPort 的值更新为用户输入的值
                        break;
                    }
                }
                File.WriteAllLines(filePath, lines);  // 将更新后的内容写回文件
                await Utils.ShowInfoBar("SIT端口", $"SIT端口已修改为 '{newPort}'");
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到'{sitCfg}'。确保已安装 Neko，并已启动游戏一次.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            }
        }

        private async void SavePSTRButton_Click(object sender, RoutedEventArgs e)
        {
            string path = @"\BepInEx\config\";
            string sitCfg = @"sit.Core.cfg";

            // Different versions of Neko has different names
            if (!File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
            {
                sitCfg = "com.sit.core.cfg";
            }

            if (File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
            {
                string filePath = App.ManagerConfig.InstallPath + path + sitCfg;
                string[] lines = File.ReadAllLines(filePath);

                string newPSTR = PSTRTextBox.Text.Trim();  // 获取用户输入的新的 PlayerStateTickRateInMS 值

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("PlayerStateTickRateInMS = "))
                    {
                        lines[i] = "PlayerStateTickRateInMS = " + newPSTR;  // 将 PlayerStateTickRateInMS 的值更新为用户输入的值
                        break;
                    }
                }
                File.WriteAllLines(filePath, lines);  // 将更新后的内容写回文件
                await Utils.ShowInfoBar("PSTR修改", $"玩家状态刷新间隔时间已修改为 '{newPSTR}'");
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到'{sitCfg}'。确保已安装 SIT组件，并已启动游戏一次.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            }
        }
        private void SITPortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isSaved = false;  // 当文本框的内容改变时，设置 isSaved 为 false
        }

        private void PSTRTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isSaved = false;  // 当文本框的内容改变时，设置 isSaved 为 false
        }

        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // 检查是否已保存
            if (!isSaved)
            {
                // 如果未保存，弹出提示框
                ContentDialog contentDialog = new()
                {
                    XamlRoot = this.XamlRoot,
                    Title = "保存更改",
                    Content = "你还没有保存更改。你想在离开页面之前保存吗？",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "不保存"
                };

                // 显示提示框
                ContentDialogResult result = await contentDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // 如果用户选择保存，那么保存更改
                    SaveSITPortButton_Click(null, null);
                    SavePSTRButton_Click(null, null);

                }

                // 如果用户选择确定，那么离开页面；否则，取消离开
               
            }
        }
    }
}
