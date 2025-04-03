using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web.Services.Description;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Neko.EFT.Manager.X.Pages.VntConfigManagementPage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Controls;
public sealed partial class DeviceListDialog : ContentDialog
{
    public DeviceListDialog(List<DeviceInfo> devices)
    {
        this.InitializeComponent(); // 确保 UI 组件初始化
        DeviceListView.ItemsSource = devices; // 绑定数据
    }

    

}
