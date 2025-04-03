using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Neko.EFT.Manager.X.Classes;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using Neko.EFT.Manager.X.Classes.CoreModules;
using CommunityToolkit.WinUI.Behaviors;
using static Neko.EFT.Manager.X.Pages.LoginPage;
using System.Timers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YamlDotNet.Core.Tokens;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class GameStartPage : Page
    {
        private string returnData;
        public static string serverAddress; // 声明为静态字段
        public static string EUsername; // 声明为静态字段
        public static string EPassword; // 声明为静态字段
        private string newPort;
        public static string sitCfg = "com.stayintarkov.cfg";
        public static string path = @"\BepInEx\config\";
        public string akiversion;
        public string eftversion;
        public ObservableCollection<ModInfos> ModDataList { get; set; } = new ObservableCollection<ModInfos>();

        private DispatcherTimer _timer;


        public GameStartPage()
        {
            
            var config = AppConfig.Load();
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            StartTimer();
            //MainGrid.Background.Opacity = 0.1;
            //UserInfoPanel.Background.Opacity = 0.91;
            //DownBar.Background.Opacity = 0.91;


            Loaded += async (sender, e) =>
            {
                ModsExpander.IsEnabled = false; // 展开 ModsExpander
                ModsExpander.Header = $"已启用的 Mods (该功能暂未开放)";
                await InitializeAsync();

            };
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await DisplayOnlinePlayers();
            if (e.Parameter is Tuple<string, string, string, string, string> parameters)
            {
                returnData = parameters.Item1;
                serverAddress = parameters.Item2;
                newPort = parameters.Item3;
                EUsername = parameters.Item4;
                EPassword = parameters.Item5;
                
            }

            SessionManager.SessionId = returnData;
        }

        private async Task InitializeAsync()
        {
            TarkovRequesting requesting = new TarkovRequesting(null, serverAddress, false);

            ShowNotification("登录成功", $"成功登录至服务器", InfoBarSeverity.Success, false, TimeSpan.FromSeconds(5));
            await Task.Delay(TimeSpan.FromSeconds(0.2));
            await UpdateServerStatus();
            await DisplayOnlinePlayers();

            var data = new Dictionary<string, string>
    {
        { "username", EUsername },
        { "email", EUsername },
        { "edition", "Edge Of Darkness" },
        { "password", EPassword },
        { "backendUrl", serverAddress }
    };

            // 请求数据并反序列化
            var returninfoDataJson = await requesting.PostJsonAsync("/launcher/profile/info", System.Text.Json.JsonSerializer.Serialize(data));
            Debug.WriteLine(returninfoDataJson);
            var returnInfoData = System.Text.Json.JsonSerializer.Deserialize<ReturnInfoData>(returninfoDataJson);

            if (returnInfoData != null)
            {
                // 显示用户信息
                Username.Text = $"用户名：{returnInfoData.username}";
                Nickname.Text = $"昵称：{returnInfoData.nickname}";
                Level.Text = $"等级：{returnInfoData.currlvl}";
                AccountEdition.Text = $"账号版本：{returnInfoData.edition}";
                ReturnDataTextBlock.Content = $"{returnInfoData.profileId}";

                // 调用 LoadData 初始化 mod 信息
                LoadData(returnInfoData);
                ModsExpander.IsEnabled = false; // 展开 ModsExpander
            }

            string jsonString = System.Text.Json.JsonSerializer.Serialize(returnInfoData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            Debug.WriteLine(jsonString);
        }


        public void LoadData(ReturnInfoData data)
        {
            if (data?.sptData?.mods != null)
            {
                ModDataList.Clear();
                foreach (var mod in data.sptData.mods)
                {
                    ModDataList.Add(mod);
                }
            }

            // 绑定数据到 ModsExpander 的 DataContext
            ModsExpander.DataContext = this;

            ModsExpander.Header = $"已启用的 Mods (该功能暂未开放)";
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // 设置执行间隔
            };
            _timer.Tick += async (sender, e) => await DisplayOnlinePlayers();
            _timer.Start();
        }


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

        public class PlayerPresenceInfo : INotifyPropertyChanged
        {
            private string _nickname;
            public string Nickname
            {
                get => _nickname;
                set { _nickname = value; OnPropertyChanged(); }
            }

            private int _level;
            public int Level
            {
                get => _level;
                set { _level = value; OnPropertyChanged(); }
            }

            private string _activityStatus;
            public string ActivityStatus
            {
                get => _activityStatus;
                set { _activityStatus = value; OnPropertyChanged(); }
            }

            private DateTime _activityStartedTime;
            public DateTime ActivityStartedTime
            {
                get => _activityStartedTime;
                set { _activityStartedTime = value; OnPropertyChanged(); }
            }

            private object _raidInformation;
            public object RaidInformation
            {
                get => _raidInformation;
                set { _raidInformation = value; OnPropertyChanged(); }
            }

            public string ParsedRaidInformation
            {
                get
                {
                    if (RaidInformation is string jsonString)
                    {
                        try
                        {
                            var jsonObject = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                            if (jsonObject != null)
                            {
                                var location = jsonObject.GetValueOrDefault("location", "未知地点");
                                var side = jsonObject.GetValueOrDefault("side", "未知阵营");
                                var time = jsonObject.GetValueOrDefault("time", "未知时间");
                                return $"地图: {location}  阵营: {side}                   时间: {time}";
                            }
                        }
                        catch
                        {
                            return "无效的战局信息";
                        }
                    }

                    return "无信息";
                }
            }


            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }



        public async Task<List<PlayerPresenceInfo>> LoadPlayerPresences()
        {
            var response = await NekoNetTools.GetPlayerPresenceInfo(serverAddress);
            Debug.WriteLine(response);

            // 检查返回数据是否包含 "UNHANDLED"
            if (response.Contains("UNHANDLED") || response == "unhandled" || response == "error")
            {
                Debug.WriteLine("Received error response from server.");

                ShowNotification("错误", $"由于服务器返回了错误，无法获取在线玩家信息。请确保SPT Server为3.10且正确安装Fika。(如您是3.10以下版本请无视）", InfoBarSeverity.Error, false, TimeSpan.FromSeconds(5));
                return new List<PlayerPresenceInfo>
        {
            new PlayerPresenceInfo
            {
                Nickname = "错误",
                Level = 0,
                ActivityStatus = "服务器请求错误",
                RaidInformation = "无信息"
            }
        };
            }

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var players = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(response);
                    var playerInfoList = new List<PlayerPresenceInfo>();

                    if (players != null && players.Count > 0)
                    {
                        foreach (var player in players)
                        {
                            var playerInfo = new PlayerPresenceInfo
                            {
                                Nickname = player["nickname"]?.ToString(),
                                Level = player.TryGetValue("level", out var levelValue) && levelValue is JsonElement levelElement
        ? levelElement.GetInt32()
        : 0,
                                ActivityStartedTime = player.TryGetValue("activityStartedTimestamp", out var timestampValue) && timestampValue is JsonElement timestampElement
        ? DateTimeOffset.FromUnixTimeSeconds(timestampElement.GetInt64()).DateTime
        : DateTime.MinValue,
                                ActivityStatus = GetActivityStatus(player),
                                RaidInformation = player.TryGetValue("raidInformation", out var raidInfoValue) && raidInfoValue is JsonElement raidInfoElement && raidInfoElement.ValueKind != JsonValueKind.Null
        ? raidInfoElement.ToString()
        : "无信息"
                            };


                            Debug.WriteLine(playerInfo.Nickname + " " + playerInfo.Level + " " + playerInfo.ActivityStatus + " " + playerInfo.ActivityStartedTime + " " + playerInfo.RaidInformation);

                            playerInfoList.Add(playerInfo);
                        }
                    }

                    return playerInfoList;
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine("JSON Parsing Error: " + ex.Message);
                    return new List<PlayerPresenceInfo>
            {
                new PlayerPresenceInfo
                {
                    Nickname = "请求错误",
                    Level = 0,
                    ActivityStatus = "请求错误",
                    RaidInformation = "无信息"
                }
            };
                }
            }

            Debug.WriteLine("Response is empty or null.");
            return new List<PlayerPresenceInfo>();
        }





        private static string GetActivityStatus(Dictionary<string, object> player)
        {
            if (player.TryGetValue("activity", out var activityValue))
            {
                Debug.WriteLine($"Activity Value: {activityValue}");
                Debug.WriteLine($"Activity Value Type: {activityValue.GetType()}");

                if (activityValue is JsonElement activityElement)
                {
                    if (activityElement.ValueKind == JsonValueKind.Number)
                    {
                        int activity = activityElement.GetInt32();
                        Debug.WriteLine($"Activity is int: {activity}");
                        return activity switch
                        {
                            1 => "战局中",
                            0 => "主菜单",
                            _ => "未知状态"
                        };
                    }
                    else if (activityElement.ValueKind == JsonValueKind.String)
                    {
                        string activityString = activityElement.GetString();
                        return activityString switch
                        {
                            "IN_MENU" => "主菜单",
                            "IN_RAID" => "战局中",
                            "IN_STASH" => "仓库中",
                            "IN_HIDEOUT" => "藏身处中",
                            "IN_FLEA" => "跳蚤市场中",
                            "服务器请求错误" => "服务器请求错误",
                            _ => "未知状态"
                        };
                    }
                }
            }

            return "未知状态"; // 默认状态
        }




        public async Task DisplayOnlinePlayers()
        {
            var players = await LoadPlayerPresences();

            // 检查 players 列表是否包含错误信息
            if (players.Count == 1 &&
                (players[0].Nickname == "错误" || players[0].ActivityStatus == "服务器请求错误" || players[0].ActivityStatus == "解析错误"))
            {
                // 显示包含错误信息的模板
                OnlinePlayersListView.ItemsSource = players;
                OnlinePlayerCountTextBlock.Text = "在线玩家数：0";
                Debug.WriteLine("在线玩家数：0 (因为返回错误或空数据)");
            }
            else
            {
                // 更新 ListView 的 ItemsSource
                OnlinePlayersListView.ItemsSource = players;

                // 更新在线玩家数
                OnlinePlayerCountTextBlock.Text = $"在线玩家数：{players.Count}";
                Debug.WriteLine("在线玩家数：" + players.Count);
            }
        }




        private async void RefreshOnlinePlayersButton_Click(object sender, RoutedEventArgs e)
        {
            await DisplayOnlinePlayers(); // 调用刷新方法
        }





        

        private async Task UpdateServerStatus()
        {
            (bool isServerOnline, long latency) = await CheckServerStatus(); // 修改此处的 latency 类型为 long

            if (isServerOnline)
            {
                Color customColor = ConvertHexToColor("#27AE60");

                // 使用该颜色创建 SolidColorBrush
                SolidColorBrush customBrush = new SolidColorBrush(customColor);

                // 设置前景色

                ServerStatusTextBlock.Text = $"服务器状态：在线    延迟：{latency}ms";
                ServerStatusTextBlock.Foreground = customBrush;



            }
            else
            {
                ServerStatusTextBlock.Text = "服务器状态：离线";
                ServerStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
        public static Color ConvertHexToColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = 255;
            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            return Color.FromArgb(a, r, g, b);
        }

        private async Task<(bool isServerOnline, long ping)> CheckServerStatus()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                long latency = await NekoNetTools.GetPing(serverAddress);
                stopwatch.Stop();

                using (var cts = new CancellationTokenSource(4000)) // 设置4秒超时
                {
                    // 获取服务器版本
                    akiversion = await GetServerVersion(cts.Token);
                    // 获取客户端版本
                    eftversion = await GetClientVersion(cts.Token);

                    this.ServerVersion.Text = $"服务器运行版本:  {akiversion}";
                    this.ServerEFTVersion.Text = $"可用客户端版本:  {eftversion}";

                    if (latency >= 0) // 如果延迟值有效，表示服务器在线
                    {
                        return (true, latency);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("检查服务器状态时发生超时。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查服务器状态时发生错误：{ex.Message}");
            }

            return (false, -1);
        }

        private async Task<string> GetServerVersion(CancellationToken cancellationToken)
        {
            string response = await NekoNetTools.GetServerVersion(serverAddress).WithCancellation(cancellationToken);
            Debug.WriteLine("Server Version: " + response);
            return response?.Trim('"') ?? "Unknown"; // 去除引号
        }

        private async Task<string> GetClientVersion(CancellationToken cancellationToken)
        {
            string response = await NekoNetTools.GetEFTVersion(serverAddress).WithCancellation(cancellationToken);
            Debug.WriteLine("Compatible Tarkov Version: " + response);
            return response?.Trim('"') ?? "Unknown"; // 去除引号
        }




        private async Task<string> LoginToServer()
        {
            TarkovRequesting requesting = new TarkovRequesting(null, PlayPage.ServerAddress, false);

            Dictionary<string, string> data = new Dictionary<string, string>

            
            {
                { "username", PlayPage.Username },
                { "email", PlayPage.Username },
                { "edition", "Edge Of Darkness" },
                { "password", PlayPage.Password },
                { "backendUrl", PlayPage.ServerAddress }
            };

            try
            {
                //var returnData = await requesting.PostJsonAsync("/launcher/profile/login-Neko", System.Text.Json.JsonSerializer.Serialize(data));
                var returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));
                Debug.WriteLine("Login Response: " + returnData);
                // If failed, attempt to register
                if (returnData == "FAILED")
                {
                    ContentDialog contentDialog = new()
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "登录失败",
                        Content = $"未找到您的账号，请联系管理员。",
                        CloseButtonText = "确定"
                    };

                    await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
                    return "error";

                }
                else if (returnData == "INVALID_PASSWORD")
                {
                    await Utils.ShowInfoBar("登录失败", $"密码错误!", InfoBarSeverity.Error);
                    return "error";
                }
                else if (returnData.Contains("UNHANDLE"))
                {
                    await Utils.ShowInfoBar("错误", "不匹配的服务端!", InfoBarSeverity.Error);
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

        private async Task ShowErrorDialog(string title, string content)
        {
            ContentDialog contentDialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };

            await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        }

        private async Task<string> Connect()
        {
            // 保存配置
            //ManagerConfig.Save((bool)RememberMeCheck.IsChecked);

            // 检查安装路径是否存在
            if (string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                await ShowErrorDialog("配置错误", "未配置 \"安装路径\"。转到设置配置安装路径。");
                return "error";
            }

            //// 检查是否存在所需文件
            //if (!File.Exists(System.IO.Path.Combine(App.ManagerConfig.InstallPath, @"BepInEx\plugins\StayInTarkov.dll")))
            //{
            //    await ShowErrorDialog("安装错误", "无法找到 \"StayInTarkov.dll\"。连接前请安装 SIT.");
            //    return "error";
            //}

            if (!File.Exists(System.IO.Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe")))
            {
                await ShowErrorDialog("安装错误", "无法在安装路径中找到 \"EscapeFromTarkov.exe，请检查你的安装路径");
                return "error";
            }

            // 检查输入是否有效
            //if (string.IsNullOrEmpty(AddressBox.Text) || string.IsNullOrEmpty(UsernameBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
            //{
            //    await ShowErrorDialog("输入错误", "地址、用户名或密码丢失.");
            //    return "error";
            //}

            // 格式化地址
            string serverAddress = PlayPage.ServerAddress;
            if (!serverAddress.StartsWith("http://"))
            {
                serverAddress = "http://" + serverAddress;
            }

            if (serverAddress.EndsWith("/") || serverAddress.EndsWith("\\"))
            {
                serverAddress = serverAddress.Remove(serverAddress.Length - 1, 1);
            }

            // 获取版本信息并检查更新
            //await CheckAndUpdateVersion();

            // 执行登录操作
            string returnData = await LoginToServer();
            return returnData;
        }

        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
            ServerStatusTextBlock.Text = "正在检查服务器可用性...";
            ServerStatusTextBlock.Foreground = new SolidColorBrush(Colors.Yellow);
            (bool isServerOnline, long latency) = await CheckServerStatus();

            if (isServerOnline)
            {
                Color customColor = ConvertHexToColor("#27AE60");
                SolidColorBrush customBrush = new SolidColorBrush(customColor);
                ServerStatusTextBlock.Text = $"服务器状态：在线    延迟：{latency}ms";
                ServerStatusTextBlock.Foreground = customBrush;

                try
                {
                    if (returnData == "error" || string.IsNullOrEmpty(returnData))
                    {
                        // 处理连接错误或返回数据为空的情况
                        ShowNotification("错误", "连接错误或返回数据为空", InfoBarSeverity.Error);
                        return;
                    }

                    var patchRunner = new ProgressReportingPatchRunner(App.ManagerConfig.InstallPath);
                    await foreach (var result in patchRunner.PatchFiles())
                    {
                        if (!result.OK)
                        {
                            ShowNotification("补丁错误", $"补丁失败：{result.Status}", InfoBarSeverity.Error);
                            return;
                        }
                    }

                    string arguments = $"-token={returnData} -config={{'BackendUrl':'{serverAddress}','MatchingVersion':'live','Version':'live'}}";
                    //string arguments = $"-token={returnData} -config={{\"BackendUrl\":\"{serverAddress}\",\"Version\":\"live\"}}";
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = System.IO.Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe"),
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    DateTime startTime = DateTime.Now; // 游戏开始时间
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

                    if (App.ManagerConfig.CloseAfterLaunch)
                    {
                        Application.Current.Exit();
                    }
                }
                catch (Exception ex)
                {
                    ShowNotification("错误", $"启动游戏时发生错误：\n{ex.Message}", InfoBarSeverity.Error);
                }

                // 如果配置为启动后立即关闭应用程序，则退出应用程序
                if (App.ManagerConfig.CloseAfterLaunch)
                {
                    Application.Current.Exit();
                }
            }
            else
            {
                ContentDialog contentDialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "登录失败",
                    Content = "服务器不可用，请检查是否正确打开N2N客户端并正确连接到服务器组网，然后联系管理员",
                    CloseButtonText = "确定"
                };

                await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);

                ServerStatusTextBlock.Text = "服务器状态：离线";
                ServerStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }


        private async void Quit_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ConnectPage));
            //await Utils.ShowInfoBar("警告", $"你已登出服务器.", InfoBarSeverity.Success);
        }

        private async Task UpdateSITPort()
        {
            try
            {
                if (App.ManagerConfig.InstallPath == null)
                {
                    await Utils.ShowInfoBar("错误", "游戏目录为空，请选择正确的游戏目录。", InfoBarSeverity.Error);
                    return;
                }

                string sitConfigFilePath = System.IO.Path.Combine(App.ManagerConfig.InstallPath + path + sitCfg);

                if (!File.Exists(sitConfigFilePath))
                {
                    await Utils.ShowInfoBar("错误", "找不到 SIT 配置文件，请检查游戏目录是否正确。", InfoBarSeverity.Error);
                    return;
                }

                string[] lines = await File.ReadAllLinesAsync(sitConfigFilePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("SITPort = "))
                    {
                        lines[i] = "SITPort = " + newPort;  // 更新 SITPort 的值为新端口
                        break;
                    }
                }

                await File.WriteAllLinesAsync(sitConfigFilePath, lines);  // 将更新后的内容写回文件
                await Utils.ShowInfoBar("端口变更", $"当前 SIT 端口已更改为 '{newPort}'");
            }
            catch (Exception ex)
            {
                await Utils.ShowInfoBar("错误", $"更新 SIT 端口时发生错误：{ex.Message}", InfoBarSeverity.Error);
            }
        }


    }

    public class ServerConfigGame
    {
        public string name { get; set; }
        public string serverAddress { get; set; }
        public string newPort { get; set; }
    }
    public class GiteeFileResponse
    {
        public string content { get; set; }
    }

}


public class ReturnInfoData
{
    public string username { get; set; }
    public string nickname { get; set; }
    public string side { get; set; }
    public int currlvl { get; set; }
    public int currexp { get; set; }
    public int prevexp { get; set; }
    public int nextlvl { get; set; }
    public int maxlvl { get; set; }
    public string edition { get; set; }
    public string profileId { get; set; }
    public SptData sptData { get; set; }
}

public class SptData
{
    public string version { get; set; }
    public Dictionary<string, object> migrations { get; set; }
    public List<ModInfos> mods { get; set; }  // 与 ModInfos 一致
    public List<Gift> receivedGifts { get; set; }
}

public class ModInfos
{
    public string author { get; set; }
    public long dateAdded { get; set; }
    public string name { get; set; }
    public string version { get; set; }

    public string FormattedDateAdded
    {
        get
        {
            // 将时间戳转换为 DateTime
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(dateAdded);
            return dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss"); // 自定义日期格式
        }
    }
}

public class Gift
{
    public string giftId { get; set; }
    public long timestampLastAccepted { get; set; }
    public int current { get; set; }
}



