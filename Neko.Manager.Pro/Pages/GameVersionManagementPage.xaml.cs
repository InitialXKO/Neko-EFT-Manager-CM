using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class GameVersionManagementPage : Page
    {
        public GameVersionManagementPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
        }

        private async void AddNewGameVersion_Click(object sender, RoutedEventArgs e)
        {
            // 显示添加新游戏版本的弹框
            await AddNewGameVersionDialog.ShowAsync();
        }

        private async void ChangeGameVersionPath_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var version = button?.DataContext as GameVersion;

            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                version.Path = folder.Path;
            }
        }

        // 点击删除游戏版本
        private void DeleteGameVersion_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var version = button?.DataContext as GameVersion;

            if (version != null)
            {
                //GameVersions.Remove(version);
            }
        }

        private async void SelectClientPath_Click(object sender, RoutedEventArgs e)
        {
            // 弹出文件夹选择对话框，选择客户端路径
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                // 显示选择的客户端路径
                ClientPathTextBlock.Text = folder.Path;
            }
        }

        private async void SelectServerPath_Click(object sender, RoutedEventArgs e)
        {
            // 弹出文件夹选择对话框，选择服务端路径
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                // 显示选择的服务端路径
                ServerPathTextBlock.Text = folder.Path;
            }
        }

        private void AddNewVersion_Confirmed(object sender, ContentDialogButtonClickEventArgs e)
        {
            // 获取输入的游戏名称
            string gameName = GameNameTextBox.Text;
            string clientPath = ClientPathTextBlock.Text;
            string serverPath = ServerPathTextBlock.Text;

            // 这里可以处理保存逻辑，将新游戏版本添加到版本管理列表
            // 例如：添加到绑定的 GameVersions 集合中

            // 关闭对话框
            AddNewGameVersionDialog.Hide();
        }
    }


    // 游戏版本模型
    public class GameVersion
    {
        public string Path { get; set; }
    }
}
