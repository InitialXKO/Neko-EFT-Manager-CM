using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Windows;
using SIT.Manager.Pages;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages.ServerManager
{
    /// <summary>
    /// ServerPage for handling SPT-AKI Server execution and console output.
    /// </summary>
    public sealed partial class NekoServerPage : Page
    {
        MainWindow? window = App.m_window as MainWindow;

        public NekoServerPage()
        {

            if (string.IsNullOrEmpty(App.ManagerConfig.AkiServerPath))
            {
                ToastNotificationHelper.ShowNotification("错误", $"未配置 \"服务端路径\"。请到设置配置安装路径。", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
                // 阻止进一步初始化
                return;
            }

            if (string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                ToastNotificationHelper.ShowNotification("错误", $"未配置 \"客户端路径\"。请到设置配置安装路径。", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
                // 阻止进一步初始化
                return;
            }

            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            this.ServerConsole.Background.Opacity = 0.75;

            DataContext = App.ManagerConfig;

            AkiServer.OutputDataReceived += AkiServer_OutputDataReceived;
            AkiServer.RunningStateChanged += AkiServer_RunningStateChanged;
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AkiServer.DetermineExeName();
            }
            catch (Exception ex)
            {
                AddConsole(ex.Message);
                return;
            }

            if (AkiServer.State == AkiServer.RunningState.NOT_RUNNING)
            {
                if (AkiServer.IsUnhandledInstanceRunning())
                {
                    AddConsole("SPT 正在运行。请关闭任何正在运行的 SPT 实例.");
                    return;
                }

                if (!File.Exists(AkiServer.FilePath))
                {
                    AddConsole("未找到 SPT-AKI。请在启动服务器之前在'设置'选项中配置 SPT-AKI 路径.");
                    return;
                }

                AddConsole("启动服务端...");

                try
                {
                    AkiServer.Start();
                }
                catch (Exception ex)
                {
                    AddConsole(ex.Message);
                }
            }
            else
            {
                AddConsole("正在停止...");

                try
                {
                    AkiServer.Stop();
                }
                catch (Exception ex)
                {
                    AddConsole(ex.Message);
                }
            }
        }


        private void AddConsole(string text)
        {
            if (text == null)
                return;

            Paragraph paragraph = new();
            Run run = new();

            // 清除 ANSI 转义序列
            run.Text = Regex.Replace(text, @"(\[\d{1,2}m)|(\[\d{1}[a-zA-Z])|(\[\d{1};\d{1}[a-zA-Z])", "");

            // 检查文本是否包含特定的错误信息，并设置相应的颜色
            if (text.Contains("[ERROR]"))
            {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (text.Contains("webserver已于"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("websocket启动于"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("服务端正在运行, 玩的开心!!"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("Profile:"))
            {
                run.Foreground = new SolidColorBrush(Colors.Orange);
            }
            if (text.Contains("Error"))
            {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }



            paragraph.Inlines.Add(run);
            ConsoleLog.Blocks.Add(paragraph);
        }


        private void AkiServer_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            window.DispatcherQueue.TryEnqueue(() => AddConsole(e.Data));
        }

        private void AkiServer_RunningStateChanged(AkiServer.RunningState runningState)
        {
            window.DispatcherQueue.TryEnqueue(() =>
            {
                switch (runningState)
                {
                    case AkiServer.RunningState.RUNNING:
                        {
                            AddConsole("服务端已启动!");
                            StartServerButtonSymbolIcon.Symbol = Symbol.Stop;
                            StartServerButtonTextBlock.Text = "停止服务端";
                        }
                        break;
                    case AkiServer.RunningState.NOT_RUNNING:
                        {
                            AddConsole("服务端已停止!");
                            StartServerButtonSymbolIcon.Symbol = Symbol.Play;
                            StartServerButtonTextBlock.Text = "启动服务端";
                        }
                        break;
                    case AkiServer.RunningState.STOPPED_UNEXPECTEDLY:
                        {
                            AddConsole("服务器意外停止！检查控制台是否有错误.");
                            StartServerButtonSymbolIcon.Symbol = Symbol.Play;
                            StartServerButtonTextBlock.Text = "启动服务端";
                        }
                        break;
                }
            });
        }
        private async void ServerconfigButton_ClickAsync(object sender, RoutedEventArgs e)
        {
           
            Frame.Navigate(typeof(NekoSetServerConfigPage));
        }

        private async void ServerManagerButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            SPTServerManager SPTServerManager = new SPTServerManager();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            SPTServerManager.Activate();
            
        }
        private void ConsoleLog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ConsoleLogScroller.ScrollToVerticalOffset(ConsoleLogScroller.ScrollableHeight);
        }
    }
}
