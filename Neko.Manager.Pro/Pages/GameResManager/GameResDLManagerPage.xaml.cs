using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System;

namespace Neko.EFT.Manager.X.Pages.GameResManager
{
    public sealed partial class GameResDLManagerPage : Page
    {
        private static readonly string ResourceUrl = "https://gitee.com/Neko17-Offical/game-resources-lib/raw/master/GameResourcesLib.json";
        public ObservableCollection<GameResource> GameResources { get; set; } = new();

        public GameResDLManagerPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            LoadGameResourcesAsync();
        }

        /// <summary>
        /// 加载游戏资源数据，并自动更新 UI
        /// </summary>
        private async Task LoadGameResourcesAsync()
        {

            ProgressRingLoading.IsActive = true; // 显示加载动画
            try
            {
                using HttpClient client = new();
                string json = await client.GetStringAsync(ResourceUrl);
                var resourceList = JsonSerializer.Deserialize<GameResourceList>(json);

                if (resourceList != null && resourceList.GameResources.Count > 0)
                {
                    GameResources.Clear();
                    foreach (var resource in resourceList.GameResources)
                    {
                        GameResources.Add(resource);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] 资源加载失败: {ex.Message}");
            }
            finally
            {
                ProgressRingLoading.IsActive = false; // 关闭加载动画
            }
        }

        /// <summary>
        /// 查看资源详情
        /// </summary>
        private async void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GameResource resource)
            {
                string details =
                    $"📌 服务器版本: {resource.ServerVersion}\n" +
                    $"📌 客户端版本: {resource.ClientVersion}\n\n" +
                    $"{resource.Description}\n\n" +
                    $"📁 客户端大小: {resource.ClientFileSize}\n" +
                    $"🖥️ 服务端大小: {resource.ServerFileSize}\n" +
                    $"📅 更新时间: {resource.LastUpdated}";

                ContentDialog dialog = new()
                {
                    Title = "资源详情",
                    Content = details,
                    CloseButtonText = "关闭",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }



        /// <summary>
        /// 下载资源
        /// </summary>
        /// <summary>
        /// 下载客户端资源
        /// </summary>
        /// <summary>
        /// 下载客户端资源
        /// </summary>
        private async void DownloadClient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GameResource resource)
            {
                ContentDialog dialog = new()
                {
                    Title = "下载客户端",
                    Content = $"是否下载客户端 {resource.ClientVersion} ？\n大小: {resource.ClientFileSize}",
                    PrimaryButtonText = "下载",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri(resource.ClientDownloadUrl));
                }
            }
        }

        /// <summary>
        /// 下载服务端资源
        /// </summary>
        private async void DownloadServer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is GameResource resource)
            {
                ContentDialog dialog = new()
                {
                    Title = "下载服务端",
                    Content = $"是否下载服务端 {resource.ServerVersion} ？\n大小: {resource.ServerFileSize}",
                    PrimaryButtonText = "下载",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await Launcher.LaunchUriAsync(new Uri(resource.ServerDownloadUrl));
                }
            }
        }



    }

    public class GameResourceList
    {
        public List<GameResource> GameResources { get; set; } = new();
    }

    public class GameResource
    {
        public string ServerVersion { get; set; }
        public string ClientVersion { get; set; }
        public string Description { get; set; }
        public string ClientDownloadUrl { get; set; }  // 客户端下载链接
        public string ServerDownloadUrl { get; set; }  // 服务端下载链接
        public string ClientFileSize { get; set; }     // 客户端文件大小
        public string ServerFileSize { get; set; }     // 服务端文件大小
        public string LastUpdated { get; set; }
    }


}
