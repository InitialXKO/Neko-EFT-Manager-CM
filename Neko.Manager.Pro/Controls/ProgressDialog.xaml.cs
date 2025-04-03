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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Controls
{
    public sealed partial class ProgressDialog : ContentDialog
    {
        public ProgressDialog()
        {
            this.InitializeComponent();
        }

        private async void OnCancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 显示取消确认对话框
            var confirmDialog = new ContentDialog
            {
                Title = "取消安装",
                Content = "您确定要取消安装模组吗？",
                PrimaryButtonText = "取消",
                SecondaryButtonText = "确定"
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                // 用户确认取消安装，关闭进度对话框
                this.Hide();
            }
        }
    }
}
