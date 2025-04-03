using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using CommunityToolkit.WinUI;
using WinRT.Interop;
using System.Diagnostics;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class ResourceDownloadPage : Page
    {
        // 存储版本信息的集合
        public List<VersionInfo> VersionInfos { get; set; }

        // 当前选中的服务端版本
        public VersionInfo SelectedServerVersion { get; set; }

        public ResourceDownloadPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            LoadVersions();
        }

        // 从远程加载 JSON 数据
        private async void LoadVersions()
        {
            // 假设 JSON 数据存储在 GitHub 或其他服务器
            string url = "https://gitee.com/Neko17-Offical/eftdownloadinfo/raw/master/downloadversioninfo.json"; // 替换为实际的 URL
            VersionInfos = await VersionInfoService.GetVersionsAsync(url);

            // 设置服务端版本的选项，绑定 VersionInfo 对象
            ServerVersionComboBox.ItemsSource = VersionInfos;
        }

        // 当选择服务端版本时，自动更新客户端版本并显示已选的服务端版本
        private void ServerVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedVersion = ServerVersionComboBox.SelectedItem as VersionInfo;
            if (selectedVersion != null)
            {
                // 更新客户端版本显示
                ClientVersionTextBlock.Text = $"客户端版本: {selectedVersion.ClientVersion}";
                // 显示已选的服务端版本
                SelectedServerVersionTextBlock.Text = $"已选服务端版本: {selectedVersion.ServerVersion}";
                // 更新下载链接
                DownloadServerResourcesButton.Tag = selectedVersion.ServerDownloadLink;
                DownloadClientResourcesButton.Tag = selectedVersion.ClientDownloadLink;
                SelectedServerVersionTextBlock.Text = $"已选服务端版本: {selectedVersion.ServerVersion}";
                SelectedClientVersionTextBlock.Text = $"已选客户端版本: {selectedVersion.ClientVersion}";
            }
            else
            {
                ClientVersionTextBlock.Text = "客户端版本: 无匹配版本";
                SelectedServerVersionTextBlock.Text = "已选服务端版本: 未选择";
            }
        }

        // 下载服务端资源
        // 下载服务端资源，调用外部下载组件
        private async void DownloadServerResources_Click(object sender, RoutedEventArgs e)
        {
            var downloadLink = (string)DownloadServerResourcesButton.Tag;
            if (!string.IsNullOrEmpty(downloadLink))
            {
                // 启动 DownloaderComponents.exe，传递下载链接
                DownloadProgressBar.Visibility = Visibility.Visible;
                await StartDownloadComponentAsync(downloadLink);
            }
        }

        // 下载客户端资源，调用外部下载组件
        private async void DownloadClientResources_Click(object sender, RoutedEventArgs e)
        {
            var downloadLink = (string)DownloadClientResourcesButton.Tag;
            if (!string.IsNullOrEmpty(downloadLink))
            {
                // 启动 DownloaderComponents.exe，传递下载链接
                DownloadProgressBar.Visibility = Visibility.Visible;
                await StartDownloadComponentAsync(downloadLink);
            }
        }


        // 弹出文件保存对话框
        private async Task<StorageFile> PickSaveFileAsync(string defaultFileName)
        {
            // 获取当前应用程序的主窗口
            var window = App.m_window;  // 通过 App.Current 访问 MainWindow

            // 获取窗口句柄
            var hwnd = WindowNative.GetWindowHandle(window);

            // 初始化 FileSavePicker
            var savePicker = new FileSavePicker();

            // 使用窗口句柄初始化 FileSavePicker
            InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.SuggestedFileName = defaultFileName;
            savePicker.FileTypeChoices.Add("All Files", new List<string> { ".exe", ".zip", ".tar", ".gz" });

            // 弹出文件保存对话框
            var file = await savePicker.PickSaveFileAsync();
            return file;
        }
        // 启动下载组件并传递下载链接和保存路径
        private async Task StartDownloadComponentAsync(string downloadLink)
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(appDirectory, "DownloaderComponents.exe");

            // 检查文件是否存在
            if (!File.Exists(exePath))
            {
                ToastNotificationHelper.ShowNotification("错误", "下载组件未找到!", "确认", null, "通知", "错误");
                return;
            }

            // 获取线程数，假设默认设置为 4
            int threadCount = 4;
            string arguments = $"{downloadLink} {threadCount}";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = true, // 允许控制台程序显示自己的窗口
                CreateNoWindow = false  // 显示控制台窗口
            };


            try
            {
                // 启动控制台进程
                var process = Process.Start(processStartInfo);

                if (process != null)
                {
                    // 异步读取标准输出
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string errorOutput = await process.StandardError.ReadToEndAsync();

                    // 输出到调试窗口
                    System.Diagnostics.Debug.WriteLine(output);
                    System.Diagnostics.Debug.WriteLine(errorOutput);

                    // 等待进程退出
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                // 捕获启动进程时的异常
                ToastNotificationHelper.ShowNotification("错误", $"启动下载组件时发生错误: {ex.Message}", "确认", null, "通知", "错误");
            }

            // 下载完成后，隐藏进度条并显示提示
            DownloadProgressBar.Visibility = Visibility.Collapsed;
            //ToastNotificationHelper.ShowNotification("下载", $"资源下载已完成 ", "确认", null, "通知", "提示");
        }

        
    }

    // 工具类，用于显示信息条
    public static class Utilsx
    {
        public static void ShowInfoBars(string title, string message)
        {
            // 在应用程序中显示信息条，或者使用其他方法来通知用户
            System.Diagnostics.Debug.WriteLine($"{title}: {message}");
        }
    }
}
