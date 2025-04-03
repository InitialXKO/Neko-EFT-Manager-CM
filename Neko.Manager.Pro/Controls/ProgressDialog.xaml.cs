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
            // ��ʾȡ��ȷ�϶Ի���
            var confirmDialog = new ContentDialog
            {
                Title = "ȡ����װ",
                Content = "��ȷ��Ҫȡ����װģ����",
                PrimaryButtonText = "ȡ��",
                SecondaryButtonText = "ȷ��"
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
            {
                // �û�ȷ��ȡ����װ���رս��ȶԻ���
                this.Hide();
            }
        }
    }
}
