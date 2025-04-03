using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using System.Collections.Generic;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class ModsAToolsCommunityPage : Page
    {
        private DownloadManager _downloadManager;
        private ObservableCollection<DownloadNotification> _downloadNotifications;

        public ModsAToolsCommunityPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            InitializeWebView();
            _downloadManager = new DownloadManager();
            _downloadManager.DownloadProgressChanged += OnDownloadProgressChanged;
            _downloadManager.DownloadCompleted += OnDownloadCompleted;
            WebView.NavigationCompleted += WebView_NavigationCompleted;

            _downloadNotifications = new ObservableCollection<DownloadNotification>();
            DownloadNotifications.ItemsSource = _downloadNotifications;
        }

        private async void InitializeWebView()
        {
            // 确保 WebView2 初始化完成
            await WebView.EnsureCoreWebView2Async();

            // 设置网页源
            WebView.CoreWebView2.Navigate("https://sns.oddba.cn");

            // 注入 JavaScript 来调整网页显示
            WebView.CoreWebView2.NavigationCompleted += async (sender, args) =>
            {
                // 等待网页完全加载后注入 JavaScript
                await WebView.CoreWebView2.ExecuteScriptAsync(@"
                    document.querySelector('meta[name=viewport]').setAttribute('content', 'width=500, initial-scale=0.7');
                ");
            };

            // 处理下载事件
            WebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
        }

        private async void CoreWebView2_DownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs args)
        {
            // 获取下载链接
            var downloadUri = args.DownloadOperation.Uri;

            // 取消默认下载行为
            args.Cancel = true;

            // 使用文件保存对话框让用户选择保存路径
            var savePicker = new FileSavePicker();
            InitializeWithWindow.Initialize(savePicker, Process.GetCurrentProcess().MainWindowHandle);

            var fileName = GetFileNameFromUrl(downloadUri);
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("All Files", new List<string> { "." });
            savePicker.SuggestedFileName = fileName;

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // 创建下载通知
                var downloadNotification = new DownloadNotification
                {
                    FileName = file.Name,
                    Progress = 0,
                    Status = "Downloading..."
                };
                _downloadNotifications.Add(downloadNotification);

                // 使用下载管理器下载文件
                var destinationPath = file.Path;
                await _downloadManager.DownloadFileAsync(downloadUri, destinationPath, downloadNotification);
            }
        }

        private string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "downloadedFile";
            }

            // 检查文件名是否合法并移除非法字符
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }

        private void OnDownloadProgressChanged(string url, long bytesDownloaded, long totalBytes, DownloadNotification notification)
        {
            var progress = totalBytes > 0 ? (double)bytesDownloaded / totalBytes * 100 : 0;
            notification.Progress = progress;
            notification.Status = $"Downloading... ({bytesDownloaded} / {totalBytes} bytes)";

            // 更新通知
            DownloadNotificationHelper.ShowOrUpdateDownloadNotification(notification.FileName, progress, notification.Status);
        }

        private async void OnDownloadCompleted(string url, bool success, string errorMessage, DownloadNotification notification)
        {
            if (success)
            {
                notification.Status = "Download completed";
                DownloadNotificationHelper.ShowDownloadCompletedNotification(notification.FileName);
            }
            else
            {
                notification.Status = $"Download failed: {errorMessage}";
                DownloadNotificationHelper.ShowDownloadFailedNotification(notification.FileName, errorMessage);
            }

            // 移除通知
            await Task.Delay(5000); // 等待 5 秒以便用户查看通知
            _downloadNotifications.Remove(notification);
        }




        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            AddressBar.Text = WebView.Source.ToString();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CanGoBack)
            {
                WebView.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (WebView.CanGoForward)
            {
                WebView.GoForward();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            WebView.Reload();
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(AddressBar.Text))
            {
                WebView.Source = new Uri(AddressBar.Text);
            }
        }
    }
}
