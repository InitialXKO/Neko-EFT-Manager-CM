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
using Neko.EFT.Manager.X.Windows;
using System.Collections.Generic;
using CommunityToolkit.WinUI.Notifications;
using System.Collections.ObjectModel;
using WinUICommunity;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ToolsPage : Page
    {
        private readonly Random rnd = new();
        private ObservableCollection<Symbol> strings { get; }


        public ToolsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            strings = new ObservableCollection<Symbol>
    {
        Symbol.AddFriend,
        Symbol.Forward,
        Symbol.Share
    };

            //this.StackPanelTools.Background.Opacity = 0.9;
            //this.rootGrid.Background.Opacity = 0.14;
        }


        


        private async void OpenEFTFolderButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (App.ManagerConfig.InstallPath == null)
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
                if (Directory.Exists(App.ManagerConfig.InstallPath))
                    Process.Start("explorer.exe", App.ManagerConfig.InstallPath);
            }
        }

        private async void LegalGameCheckedButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                // 在按钮点击事件中调用包装器的模拟合法性检查方法
                LegalGameCheckWrapper.SimulateLegalCheck();

                // 根据实际检查结果来更新 UI 的代码，你可以根据需要自行调整
                if (LegalGameCheckWrapper.Checked)
                {
                    // 游戏合法
                    await Utils.ShowInfoBar("确定", "游戏合法.", InfoBarSeverity.Success);
                }
                else
                {
                    // 游戏非法
                    await Utils.ShowInfoBar("错误", "游戏非法.", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                // 处理可能的异常
                await Utils.ShowInfoBar("错误", "游戏检测错误.", InfoBarSeverity.Error);
            }
        }

        private async void OpenBepInExFolderButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (App.ManagerConfig.InstallPath == null)
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

            string dirPath = App.ManagerConfig.InstallPath + @"\BepInEx\plugins\";
            if (Directory.Exists(dirPath))
                Process.Start("explorer.exe", dirPath);
            else
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = $"找不到指定路径。是否已安装 BepInEx？",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }
        }

        private async void OpenSITConfigButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string path = @"\BepInEx\config\";
            string sitCfg = @"com.stayintarkov.cfg";

            // Different versions of Neko has different names
            if (!File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
            {
                sitCfg = "com.stayintarkov.cfg";
            }

            if (!File.Exists(App.ManagerConfig.InstallPath + path + sitCfg))
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

            Process.Start("explorer.exe", App.ManagerConfig.InstallPath + path + sitCfg);
        }



        private async void ServerManagerButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            SPTServerManager SPTServerManager = new SPTServerManager();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            SPTServerManager.Activate();
        }


        private async void OpenEFTLogButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            string logPath = @"%userprofile%\AppData\LocalLow\Battlestate Games\EscapeFromTarkov\Player.log";
            logPath = Environment.ExpandEnvironmentVariables(logPath);
            if (File.Exists(logPath))
            {
                Process.Start("explorer.exe", logPath);
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "配置错误",
                    Content = "log文件未找到.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return;
            }
        }

        private async void InstallSITButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            GithubRelease? selectedVersion;

            SelectSitVersionDialog selectWindow = new()
            {
                XamlRoot = Content.XamlRoot
            };

            ContentDialogResult result = await selectWindow.ShowAsync();

            selectedVersion = selectWindow.version;

            if (selectedVersion == null || result != ContentDialogResult.Primary)
            {
                return;
            }

            await Task.Run(() => Utils.InstallSIT(selectedVersion));
            ManagerConfig.Save();
        }

        private void OpenLocationEditorButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = App.m_window as MainWindow;
            
            window.contentFrame.Navigate(typeof(LocationEditor));
        }

        private async void OpenlinkButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            // 创建一个新的窗口
            var newWindow = new Window();
            newWindow.Title = "EFT资源";

            // 创建一个 TabView 控件来容纳两个页面
            var tabView = new TabView();

            // 使用 HttpClient 来获取 txt 文件内容
            var httpClient = new HttpClient();
            var txt1 = await httpClient.GetStringAsync("https://gitee.com/Neko17-Offical/tkfclient-downlaod/raw/master/link.txt");
            var txt2 = await httpClient.GetStringAsync("https://gitee.com/Neko17-Offical/stayintarkov-patcher/raw/master/link.txt");

            // 为每个 txt 文件创建一个 TabViewItem
            var tabViewItem1 = CreateTabViewItem(txt1, "EFT游戏本体");
            var tabViewItem2 = CreateTabViewItem(txt2, "多人补丁资源");

            // 将 TabViewItem 添加到 TabView 控件中
            tabView.TabItems.Add(tabViewItem1);
            tabView.TabItems.Add(tabViewItem2);

            // 将 TabView 控件添加到新窗口的内容中
            newWindow.Content = tabView;

            // 显示新窗口
            newWindow.SystemBackdrop = new DesktopAcrylicBackdrop();
            newWindow.Activate();
            newWindow.SetIsResizable(false);
            newWindow.SetIsMaximizable(false);
        }

        private TabViewItem CreateTabViewItem(string txt, string header)
        {
            // 创建一个 StackPanel 控件来容纳链接
            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,

            };

            // 将 txt 文件内容按行分割
            var lines = txt.Split('\n');

            // 为每两行创建一个 TextBlock 和一个 HyperlinkButton 控件
            for (int i = 0; i < lines.Length; i += 2)
            {
                if (i + 1 >= lines.Length) break;

                var title = lines[i];
                var href = lines[i + 1];

                // 创建一个 TextBlock 控件来显示标题
                var textBlock = new TextBlock
                {
                    Text = title,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(textBlock);

                // 创建一个 HyperlinkButton 控件来显示链接
                var hyperlinkButton = new HyperlinkButton
                {
                    NavigateUri = new Uri(href),
                    Content = href,  // 将超链接的显示内容设置为链接本身
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stackPanel.Children.Add(hyperlinkButton);
            }

            // 创建一个 ScrollViewer 控件来使内容可以滚动
            var scrollViewer = new ScrollViewer
            {
                Content = stackPanel
            };

            // 创建一个 TabViewItem 控件并设置其 Header 和 Content
            var tabViewItem = new TabViewItem
            {
                Header = header,
                Content = scrollViewer
            };

            return tabViewItem;
        }

        private void CleanTempFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool result = CleanTempFiles();

                if (result)
                {
                    // 清理成功的消息或处理逻辑
                    ShowInfoDialog("清理成功", "临时文件已成功清理。");
                }
                else
                {
                    // 清理失败的消息或处理逻辑
                    ShowErrorDialog("清理失败", "未能完全清理临时文件。");
                }
            }
            catch (Exception ex)
            {
                // 异常处理
                ShowErrorDialog("错误", $"清理临时文件时发生错误: {ex.Message}");
            }
        }

        public bool CleanTempFiles()
        {
            var rootDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), @"Battlestate Games\EscapeFromTarkov"));

            if (!rootDir.Exists)
            {
                return true; // 如果根目录不存在，认为已清理成功
            }

            return RemoveFilesRecursively(rootDir);
        }

        private bool RemoveFilesRecursively(DirectoryInfo directory)
        {
            try
            {
                // 删除当前目录下的文件
                foreach (FileInfo file in directory.GetFiles())
                {
                    file.Delete();
                }

                // 递归删除子目录下的文件
                foreach (DirectoryInfo subDir in directory.GetDirectories())
                {
                    RemoveFilesRecursively(subDir);
                }

                // 删除当前目录
                directory.Delete();

                return true; // 删除成功
            }
            catch (Exception ex)
            {
                // 删除过程中出现异常
                Console.WriteLine($"删除文件时出现异常: {ex.Message}");
                return false; // 删除失败
            }
        }

        private async void ShowInfoDialog(string title, string content)
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

        private async void ShowErrorDialog(string title, string content)
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


        private void TEST2_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TEST3_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TEST8_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void VNT_Click(object sender, RoutedEventArgs e)
        {
            VntManagementWindow vntManagementWindow = new VntManagementWindow();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            vntManagementWindow.Activate();
        }

        private void TEST6_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TEST5_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void TEST4_Click(object sender, RoutedEventArgs e)
        {
            SimplicityModeLoginWindow SimplicityModeLoginWindow = new SimplicityModeLoginWindow();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            SimplicityModeLoginWindow.Activate();
        }


        private async void KillGameprocAsync_Click(object sender, RoutedEventArgs e)
        {
            var messages = new List<string>();

            messages.Add(await KillProcessByNameAsync("EscapeFromTarkov.exe"));
            messages.Add(await KillProcessByNameAsync("Aki.Server.exe"));
            messages.Add(await KillProcessByNameAsync("SPT.Server.exe"));

            string title = "操作结果";
            string combinedMessage = string.Join("\n", messages);
            await ShowMessageAsync(title, combinedMessage);
            ToastNotificationHelper.ShowNotification(title, combinedMessage, "确认", (arg) =>
            {
                // 执行其他操作...
            }, "通知", "错误");
        }

        private async Task<string> KillProcessByNameAsync(string processName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/F /IM {processName}", // 使用 /F 参数强制结束进程
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (var process = Process.Start(psi))
                {
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        return $"成功结束进程 {processName}";
                    }
                    else
                    {
                        return $"无法结束进程 {processName}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"查找进程 {processName} 时发生异常: {ex.Message}";
            }
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = "关闭"
            };

            await dialog.ShowAsync();
        }

        private async void N2NMXButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取当前目录
                string currentDirectory = System.IO.Directory.GetCurrentDirectory();
                string exePath = System.IO.Path.Combine(currentDirectory, "N2NMX.exe");

                // 创建启动进程的信息
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Verb = "runas", // 指定以管理员模式运行
                    UseShellExecute = true // 必须设置为 true，以便使用操作系统的 shell 执行
                };

                // 启动进程
                System.Diagnostics.Process.Start(startInfo);

                // 可选的延迟，用于等待程序启动
                await Task.Delay(TimeSpan.FromSeconds(0.02));
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // 捕获用户取消 UAC 提示时的异常
                ShowErrorDialog("启动失败", $"未能启动 N2NMX.exe：{ex.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorDialog("未知错误", $"发生了未知错误：{ex.Message}");
            }
        }
    }
}
