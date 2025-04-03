using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Diagnostics;
using System.Text.Json;
using static Neko.EFT.Manager.X.Classes.Utils;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System.Text.RegularExpressions;

namespace Neko.EFT.Manager.X.Pages
{

    public sealed partial class OnePage : Page
    {

        private MainWindow? window = App.m_window as MainWindow;
        public OnePage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            this.PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;


            // 订阅服务端输出事件
            AkiServer.OutputDataReceived += AkiServer_OutputDataReceived;
            AkiServer.RunningStateChanged += AkiServer_RunningStateChanged;
            DataContext = App.ManagerConfig;

            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ConnectButton.IsEnabled = false;
            }

            UpdateConnectButtonState();
            LoadServerConfig();
        }



        private void AddConsole(string text)
        {
            if (text == null)
                return;

            Paragraph paragraph = new();
            Run run = new();

            // 清除 ANSI 转义序列
            run.Text = Regex.Replace(text, @"(\[\d{1,2}m)|(\[\d{1}[a-zA-Z])|(\[\d{1};\d{1}[a-zA-Z])", "");

            // 设置文本颜色
            if (text.Contains("[ERROR]"))
            {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }
            else if (text.Contains("webserver已于") || text.Contains("websocket启动于") || text.Contains("服务端正在运行, 玩的开心!!"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            else if (text.Contains("Profile:"))
            {
                run.Foreground = new SolidColorBrush(Colors.Orange);
            }
            else if (text.Contains("Error"))
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
                            // Update UI elements
                            // Example: StartServerButtonSymbolIcon.Symbol = Symbol.Stop;
                            // Example: StartServerButtonTextBlock.Text = "停止服务端";
                        }
                        break;
                    case AkiServer.RunningState.NOT_RUNNING:
                        {
                            AddConsole("服务端已停止!");
                            // Update UI elements
                            // Example: StartServerButtonSymbolIcon.Symbol = Symbol.Play;
                            // Example: StartServerButtonTextBlock.Text = "启动服务端";
                        }
                        break;
                    case AkiServer.RunningState.STOPPED_UNEXPECTEDLY:
                        {
                            AddConsole("服务器意外停止！检查控制台是否有错误.");
                            // Update UI elements
                            // Example: StartServerButtonSymbolIcon.Symbol = Symbol.Play;
                            // Example: StartServerButtonTextBlock.Text = "启动服务端";
                        }
                        break;
                }
            });
        }

        private void ConsoleLog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ConsoleLogScroller.ScrollToVerticalOffset(ConsoleLogScroller.ScrollableHeight);
        }

        private async void LoadServerConfig()
        {
            try
            {
                string serverPath = App.ManagerConfig.InstallPath;
                if (string.IsNullOrEmpty(serverPath))
                {
                    return;
                }

                string configFilePath = Path.Combine(serverPath, @"Aki_Data\Server\configs\http.json");
                if (!File.Exists(configFilePath))
                {
                    configFilePath = Path.Combine(serverPath, @"SPT_Data\Server\configs\http.json");
                }

                if (!File.Exists(configFilePath))
                {
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                ServerIPBox.Text = config["ip"]?.ToString();
                ServerLoginIPBox.Text = config["backendIp"]?.ToString();
                ServerLoginPortBox.Text = config["backendPort"]?.ToString();
                ServerPortBox.Text = config["port"]?.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading server config: {ex.Message}");
            }
        }

        private async void SaveServerConfig()
        {
            try
            {
                string serverPath = App.ManagerConfig.InstallPath;
                if (string.IsNullOrEmpty(serverPath))
                {
                    return;
                }

                string configFilePath = Path.Combine(serverPath, @"Aki_Data\Server\configs\http.json");
                if (!File.Exists(configFilePath))
                {
                    configFilePath = Path.Combine(serverPath, @"SPT_Data\Server\configs\http.json");
                }

                if (!File.Exists(configFilePath))
                {
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                config["ip"] = ServerIPBox.Text;
                config["backendIp"] = ServerLoginIPBox.Text;
                config["port"] = int.Parse(ServerPortBox.Text);
                config["backendPort"] = int.Parse(ServerLoginPortBox.Text);

                jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving server config: {ex.Message}");
            }
        }

        private void ServerIPBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerConfig();
        }

        private void ServerLoginIPBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerConfig();
        }
        private void ServerLoginPortBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerConfig();
        }

        private void ServerPortBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerConfig();
        }


        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateConnectButtonState();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateConnectButtonState();
        }

        

        private async void SelectServerPath_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                App.ManagerConfig.AkiServerPath = folder.Path;
                ServerPathBox.Text = folder.Path;
                UpdateConnectButtonState();
            }
        }

        private async void SelectClientPath_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                App.ManagerConfig.InstallPath = folder.Path;
                ClientPathBox.Text = folder.Path;
                UpdateConnectButtonState();
            }
        }

        //private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    string returnData = await Connect();

        //    if (returnData == "error")
        //    {
        //        return;
        //    }
        //    if (string.IsNullOrEmpty(returnData))
        //    {
        //        return;
        //    }

        //    ToastNotificationHelper.ShowNotification("登录完成", $"成功登录至服务器：\n {AddressBox.Text} \n 游戏即将启动", "确认", (arg) =>
        //    {
        //        // 执行其他操作...
        //    }, "通知", "错误");

        //    string arguments = $"-token={returnData} -config={{\"BackendUrl\":\"{AddressBox.Text}\",\"Version\":\"live\"}}";
        //    Process.Start(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov.exe", arguments);

        //    if (App.ManagerConfig.CloseAfterLaunch)
        //    {
        //        Application.Current.Exit();
        //    }
        //}

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // 启动服务端
            if (AkiServer.State == AkiServer.RunningState.NOT_RUNNING)
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
                    return;
                }
            

            // 等待服务端启动
            while (AkiServer.State != AkiServer.RunningState.RUNNING)
            {
                await Task.Delay(500);
            }

            // 确认服务端已启动
            AddConsole("服务端已启动，等待初始化完成...");

            // 设置超时时间（例如60秒）
            TimeSpan timeout = TimeSpan.FromSeconds(60);
            DateTime startTime = DateTime.Now;

            // 使用 TaskCompletionSource 来等待服务端输出 "服务端初始化完成"
            var serverInitializedTaskSource = new TaskCompletionSource<bool>();

            void OnServerOutputReceived(object sender, DataReceivedEventArgs e)
            {
                if (e.Data != null && e.Data.Contains("服务端正在运行, 玩的开心!!"))
                {
                    serverInitializedTaskSource.SetResult(true);
                }
            }

            // 订阅服务端输出事件
            AkiServer.OutputDataReceived += OnServerOutputReceived;

            // 等待服务端输出 "服务端初始化完成" 或超时
            var delayTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(serverInitializedTaskSource.Task, delayTask);

            // 取消订阅服务端输出事件
            AkiServer.OutputDataReceived -= OnServerOutputReceived;

            if (completedTask == delayTask)
            {
                //AddConsole("服务端超时未响应.");
                ToastNotificationHelper.ShowNotification("错误", $"未能启动游戏：服务端超时未响应", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
                return;
            }

            // 确认服务端已初始化完成
            //AddConsole("服务端初始化完成，开始连接...");

            ToastNotificationHelper.ShowNotification("初始化完成", $"服务端初始化完成，开始连接...", "确认", (arg) =>
            {
                // 执行其他操作...
            }, "通知", "完成");
            }
            else
            {
               
                UpdateConnectButtonState();
                return;
            }
            UpdateConnectButtonState();
            string returnData = await Connect();

            if (returnData == "error")
            {
                return;
            }
            if (string.IsNullOrEmpty(returnData))
            {
                return;
            }

            ToastNotificationHelper.ShowNotification("登录完成", $"成功登录至服务器：\n {AddressBox.Text} \n 游戏即将启动", "确认", (arg) =>
            {
                // 执行其他操作...
            }, "通知", "错误");

            //string arguments = $"-token={returnData} -config={{\"BackendUrl\":\"{AddressBox.Text}\",\"Version\":\"live\"}}";
            //Process.Start(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov.exe", arguments);

            try
            {
                var patchRunner = new ProgressReportingPatchRunner(App.ManagerConfig.InstallPath);
                await foreach (var result in patchRunner.PatchFiles())
                {
                    if (!result.OK)
                    {
                        ToastNotificationHelper.ShowNotification("补丁错误", $"补丁失败：{result.Status}", "确认", (arg) =>
                        {
                            // 执行其他操作...
                        }, "通知", "错误");
                        return;
                    }
                }

                string arguments = $"-token={returnData} -config={{\"BackendUrl\":\"{AddressBox.Text}\",\"Version\":\"live\"}}";
                var startInfo = new ProcessStartInfo
                {
                    FileName = System.IO.Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe"),
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    using (StreamReader outputReader = process.StandardOutput)
                    using (StreamReader errorReader = process.StandardError)
                    {
                        string output = await outputReader.ReadToEndAsync();
                        string error = await errorReader.ReadToEndAsync();

                        Debug.WriteLine("Output: " + output);
                        Debug.WriteLine("Error: " + error);
                    }

                    await process.WaitForExitAsync();
                }

                if (App.ManagerConfig.CloseAfterLaunch)
                {
                    Application.Current.Exit();
                }
            }
            catch (Exception ex)
            {
                ToastNotificationHelper.ShowNotification("错误", $"启动游戏时发生错误：\n {ex.Message} ", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
            }

            if (App.ManagerConfig.CloseAfterLaunch)
            {
                Application.Current.Exit();
            }
        }

        private void StopServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (AkiServer.State != AkiServer.RunningState.NOT_RUNNING)
            {
                // 服务端正在运行，执行停止操作
                AddConsole("服务端正在运行，正在停止...");

                try
                {
                    AkiServer.Stop();
                    AddConsole("服务端已停止。");
                    UpdateConnectButtonState();
                }
                catch (Exception ex)
                {
                    AddConsole($"停止服务端失败：{ex.Message}");
                    UpdateConnectButtonState();
                }
            }
        }


        private void UpdateConnectButtonState()
        {
            // 更新连接按钮的状态
            if (AkiServer.State == AkiServer.RunningState.NOT_RUNNING)
            {
                ConnectButton_Text.Text = "启动";
            }
            else
            {
                ConnectButton_Text.Text = "连接";
            }

            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0 || string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"先填写所有字段."
                });
                ConnectButton.IsEnabled = false;
            }
            else if (AddressBox.Text.Length > 0)
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"尝试连接 {AddressBox.Text} 并启动游戏."
                });
                ConnectButton.IsEnabled = true;
            }

            // 更新停止按钮的状态
            if (AkiServer.State == AkiServer.RunningState.RUNNING)
            {
                StopServerButton.IsEnabled = true;
                ToolTipService.SetToolTip(StopServerButton, new ToolTip()
                {
                    Content = "停止正在运行的服务端."
                });
            }
            else
            {
                StopServerButton.IsEnabled = false;
                ToolTipService.SetToolTip(StopServerButton, new ToolTip()
                {
                    Content = "服务端未运行."
                });
            }
        }



        private async Task<string> Connect()
        {
            ManagerConfig.Save((bool)RememberMeCheck.IsChecked);

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

            if (!File.Exists(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov.exe"))
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "安装错误",
                    Content = "无法在安装路径中找到 \"EscapeFromTarkov.exe，请检查你的安装路径",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return "error";
            }

            if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0)
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "输入错误",
                    Content = "地址、用户名或密码丢失.",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                return "error";
            }

            if (!AddressBox.Text.Contains(@"http://"))
            {
                AddressBox.Text = @"http://" + AddressBox.Text;
            }

            if (AddressBox.Text.EndsWith(@"/") || AddressBox.Text.EndsWith(@"\"))
            {
                AddressBox.Text = AddressBox.Text.Remove(AddressBox.Text.Length - 1, 1);
            }

            string returnData = await LoginToServer();
            return returnData;
        }

        private async Task<string> LoginToServer()
        {
            TarkovRequesting requesting = new TarkovRequesting(null, AddressBox.Text, false);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "username", UsernameBox.Text },
                { "email", UsernameBox.Text },
                { "edition", "Edge Of Darkness" },
                { "password", PasswordBox.Password },
                { "backendUrl", AddressBox.Text }
            };

            try
            {
                var returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));

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

                        returnData = await requesting.PostJsonAsync("/launcher/profile/register", System.Text.Json.JsonSerializer.Serialize(data));
                        returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (returnData == "INVALID_PASSWORD")
                {
                    ToastNotificationHelper.ShowNotification("登录失败", $"密码错误!", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "错误");
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
    }
}
