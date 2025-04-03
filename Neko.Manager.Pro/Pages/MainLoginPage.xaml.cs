using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainLoginPage : Page
{
    public string serverAddress = App.ManagerConfig.LastServer;
    public MainLoginPage()
    {
        this.InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
        DataContext = App.ManagerConfig;
    }
    public string Username { get; set; } // 用于绑定用户名
    public bool RememberLogin { get; set; } // 用于绑定记住我选项


    private void ShowNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, bool isPersistent, TimeSpan? duration = null)
    {
        NotificationInfoBar.Title = title;
        NotificationInfoBar.Message = message;
        NotificationInfoBar.Severity = severity;
        NotificationInfoBar.IsOpen = true; // 显示通知

        if (!isPersistent) // 如果不是常驻通知
        {
            if (duration.HasValue) // 检查是否有停留时间
            {
                _ = Task.Delay(duration.Value).ContinueWith(_ =>
                {
                    // 在停留时间结束后关闭 InfoBar
                    NotificationInfoBar.IsOpen = false;
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }

    private void ShowNotification(string title, string message, InfoBarSeverity severity)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Severity = severity,
        };

        NotificationQueue.Show(notification);
    }


    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text;
        var password = "null"; // 从密码框获取密码

        // 调用连接方法
        string returnData = await Connect(username, password);

        // 处理返回数据
        if (returnData == null)
        {
            ShowNotification("登陆取消", "您取消了操作。", InfoBarSeverity.Warning, false, TimeSpan.FromSeconds(5));
            // 用户选择了取消，不进行任何操作
            return;
        }
        else if (returnData == "error")
        {
            // 显示错误提示
            ShowNotification("登陆失败", "请检查日志！", InfoBarSeverity.Error, false, TimeSpan.FromSeconds(3));
        }
        else
        {
            // 登录成功，启动游戏
            ShowNotification("登录成功", "欢迎回来！", InfoBarSeverity.Success, false, TimeSpan.FromSeconds(3));
            await StartGame(returnData);
        }
    }






    private async Task<string> Connect(string username, string password)
    {
        ManagerConfig.Save();

        if (App.ManagerConfig.InstallPath == null)
        {
            ContentDialog contentDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "配置错误",
                Content = "未配置 \"安装路径\"。转到设置配置安装路径。",
                CloseButtonText = "确定"
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            return "error";
        }

        if (!File.Exists(Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe")))
        {
            ContentDialog contentDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "安装错误",
                Content = "无法在安装路径中找到 \"EscapeFromTarkov.exe\"，请检查你的安装路径。",
                CloseButtonText = "确定"
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            return "error";
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ContentDialog contentDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "输入错误",
                Content = "用户名或密码丢失。",
                CloseButtonText = "确定"
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            return "error";
        }

        // 从 ManagerConfig 中读取 LastServer 值
        string serverAddress = App.ManagerConfig.LastServer;
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            ContentDialog contentDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "配置错误",
                Content = "未配置服务器地址，请在设置中配置。",
                CloseButtonText = "确定"
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            return "error";
        }

        // 确保地址包含 http:// 前缀
        if (!serverAddress.StartsWith(@"http://"))
        {
            serverAddress = @"http://" + serverAddress;
        }

        if (serverAddress.EndsWith(@"/") || serverAddress.EndsWith(@"\"))
        {
            serverAddress = serverAddress.Remove(serverAddress.Length - 1, 1);
        }

        // 更新 AddressBox 的值
        App.ManagerConfig.LastServer = serverAddress;


        if (RememberMeCheck.IsChecked == true) // 检查是否勾选 Remember Me
        {

            ManagerConfig.SaveAccountInfo(); // 保存账户信息
        }
        else
        {
            ManagerConfig.Save();
        }

        var returnData = await LoginToServer(username, password);

        // 检查返回值以确定下一步操作
        if (returnData == null)
        {
            // 用户选择了取消，处理取消逻辑

            return null;
        }
        else if (returnData == "error")
        {
            // 处理登录错误
            Debug.WriteLine("登录发生错误。");
        }
        else
        {
            // 登录成功，处理成功逻辑
            Debug.WriteLine("登录成功，返回数据: " + returnData);
        }

        return returnData;
    }


    private async Task<string> LoginToServer(string username, string password)
    {
        TarkovRequesting requesting = new TarkovRequesting(null, serverAddress, false);

        Dictionary<string, string> data = new Dictionary<string, string>
    {
        { "username", username },
        { "email", username },
        { "edition", "Edge Of Darkness" },
        { "password", password },
        { "backendUrl", serverAddress }
    };

        try
        {
            var returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));

            // 如果失败，尝试注册
            if (returnData == "FAILED")
            {
                ContentDialog createAccountDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "账号不存在",
                    Content = "未找到您的帐户，您想用这些凭据注册一个新帐户吗？",
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消"
                };

                ContentDialogResult msgBoxResult = await createAccountDialog.ShowAsync(ContentDialogPlacement.InPlace);
                if (msgBoxResult == ContentDialogResult.Primary)
                {
                    SelectEditionDialog selectWindow = new()
                    {
                        XamlRoot = Content.XamlRoot
                    };
                    await selectWindow.ShowAsync();
                    string edition = selectWindow.edition;

                    if (edition != null)
                        data["edition"] = edition;

                    // 注册
                    returnData = await requesting.PostJsonAsync("/launcher/profile/register", System.Text.Json.JsonSerializer.Serialize(data));

                    // 注册后尝试登录
                    returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));
                }
                else
                {
                    // 用户选择了取消，返回 null
                    return null;
                }
            }
            else if (returnData == "INVALID_PASSWORD")
            {
                ToastNotificationHelper.ShowNotification("登录失败", "密码错误!", "确认", (arg) => { }, "通知", "错误");
                return "error";
            }

            return returnData;
        }
        catch (System.Net.WebException webEx)
        {
            ContentDialog contentDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "登录失败",
                Content = $"无法与服务器通信\n{webEx.Message}",
                CloseButtonText = "确定"
            };

            Loggy.LogToFile("Login Error: " + webEx);
            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            return "error";
        }
        catch (Exception ex)
        {
            ContentDialog contentDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "登录失败",
                Content = $"无法与服务器通信\n{ex.Message}",
                CloseButtonText = "确定"
            };

            Loggy.LogToFile("Login Error: " + ex);
            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            return "error";
        }
    }


    private async Task<string> ShowSelectEditionDialog()
    {
        // 创建并显示选择版本的弹窗
        SelectEditionDialog selectWindow = new SelectEditionDialog();
        ContentDialogResult result = await selectWindow.ShowAsync();

        // 当用户点击了“创建”按钮时，检查是否选择了版本
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(selectWindow.edition))
        {
            return selectWindow.edition;
        }

        return null; // 用户未选择或取消操作
    }



    private async Task StartGame(string token)
    {
        string installPath = App.ManagerConfig.InstallPath; // 获取安装路径
        string backendUrl = App.ManagerConfig.LastServer; // 获取服务器地址

        // 首先进行补丁更新
        var patchRunner = new ProgressReportingPatchRunner(installPath);
        await foreach (var result in patchRunner.PatchFiles())
        {
            if (!result.OK)
            {
                // 如果补丁失败，显示通知并返回
                ShowNotification("补丁错误", $"补丁失败：{result.Status}", InfoBarSeverity.Error);
                return;
            }
        }

        // 构建启动参数
        string arguments = $"-token={token} -config={{\"BackendUrl\":\"{backendUrl}\",\"Version\":\"live\"}}";
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(installPath, "EscapeFromTarkov.exe"),
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        DateTime startTime = DateTime.Now; // 游戏开始时间
        try
        {
            using (Process process = Process.Start(startInfo))
            {
                // 显示常驻通知：游戏启动成功，正在运行
                ShowNotification("游戏启动成功", "Escape From Tarkov 正在运行中...", InfoBarSeverity.Warning);

                using (StreamReader outputReader = process.StandardOutput)
                using (StreamReader errorReader = process.StandardError)
                {
                    string output = await outputReader.ReadToEndAsync();
                    string error = await errorReader.ReadToEndAsync();

                    Debug.WriteLine("Output: " + output);
                    Debug.WriteLine("Error: " + error);
                }

                // 等待游戏进程退出
                await process.WaitForExitAsync();

                TimeSpan gameDuration = DateTime.Now - startTime;
                // 游戏退出后关闭现有通知并显示新的通知
                NotificationInfoBar.IsOpen = false; // 关闭现有通知
                ShowNotification(
                    "游戏结束",
                    $"Escape From Tarkov 已退出。\n本次游戏时间: {gameDuration.Hours}小时 {gameDuration.Minutes}分钟 {gameDuration.Seconds}秒\n当前时间: {DateTime.Now:HH:mm:ss}",
                    InfoBarSeverity.Informational,
                    false,
                    TimeSpan.FromSeconds(5) // 非常驻通知，5秒后关闭
                );
            }

            // 如果配置了启动游戏后关闭应用，则执行关闭操作
            if (App.ManagerConfig.CloseAfterLaunch)
            {
                Application.Current.Exit();
            }
        }
        catch (Exception ex)
        {
            // 游戏启动失败，关闭通知并显示错误提示
            NotificationInfoBar.IsOpen = false;
            await ShowErrorDialog("启动失败", $"启动游戏时发生错误：\n{ex.Message}");
        }
    }





    private async Task ShowErrorDialog(string title, string content)
    {
        ContentDialog contentDialog = new()
        {
            XamlRoot = Content.XamlRoot,
            Title = title,
            Content = content,
            CloseButtonText = "确定"

        };
        await contentDialog.ShowAsync();
    }
}
