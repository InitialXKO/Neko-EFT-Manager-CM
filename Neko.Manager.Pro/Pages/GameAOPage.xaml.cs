using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Neko.EFT.Manager.X.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using System.Reflection;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI;
using OpenUrl = Windows.System;
using System.Linq;
using Microsoft.UI.Input;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using UnityEngine.Networking.NetworkSystem;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// ServerPage for handling SPT-AKI Server execution and console output.
    /// </summary>
    public sealed partial class GameAOPage : Page
    {
        MainWindow? window = App.m_window as MainWindow;
        

        public GameAOPage()
        {

            if (string.IsNullOrEmpty(App.ManagerConfig.AkiServerPath))
            {
                ToastNotificationHelper.ShowNotification("错误", $"未配置 \"服务端路径\"。请到设置配置安装路径。", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
                // 阻止进一步初始化
                return;
            }

            if (string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                ToastNotificationHelper.ShowNotification("错误", $"未配置 \"客户端路径\"。请到设置配置安装路径。", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
                // 阻止进一步初始化
                return;
            }
            
            
            InitializeComponent();
            
            BindInputEvents();

            if (App.ManagerConfig.LookForUpdates == true)
            {
                Task.Run(() =>
                {
                    LookForUpdate();
                });
            }
            DataContext = App.ManagerConfig;

            AkiServer.OutputDataReceived += AkiServer_OutputDataReceived;
            AkiServer.RunningStateChanged += AkiServer_RunningStateChanged;
            
            //LoadServerConfig();


            //UpdateConnectButtonState();
            this.Loaded += GameAOPage_Loaded;

        }

        private void GameAOPage_Loaded(object sender, RoutedEventArgs e)
        {

            // 在页面加载完成后调用 UpdateConnectButtonState 方法
            LoadCarouselImages();
            UpdateConnectButtonState();
            LoadLoginAddress();
            LoadServerVersion();
            LoadClientVersion();
            //UpdateUI(); // 确保在页面加载时更新 UI

        }


        private async void LoadLoginAddress()
        {
            try
            {
                string serverPath = App.ManagerConfig.AkiServerPath;
                if (string.IsNullOrEmpty(serverPath))
                {
                    return;
                }

                // 配置文件路径
                string configFilePath = Path.Combine(serverPath, @"Aki_Data\Server\configs\http.json");
                if (!File.Exists(configFilePath))
                {
                    configFilePath = Path.Combine(serverPath, @"SPT_Data\Server\configs\http.json");
                }

                if (!File.Exists(configFilePath))
                {
                    Debug.WriteLine("Config file not found.");
                    return;
                }

                // 读取配置文件
                string jsonContent = File.ReadAllText(configFilePath);
                dynamic config = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);

                string backendIp = config.backendIp;
                string backendPort = config.backendPort;

                if (!string.IsNullOrEmpty(backendIp) && !string.IsNullOrEmpty(backendPort))
                {
                    string loginAddress = $"http://{backendIp}:{backendPort}";
                    LoginAddressHyperlinkButton.Content = loginAddress;
                    

                    // 添加复制功能
                    LoginAddressHyperlinkButton.Click += async (s, e) =>
                    {
                        try
                        {
                            DataPackage dataPackage = new()
                            {
                                RequestedOperation = DataPackageOperation.Copy,
                            };

                            dataPackage.SetText(LoginAddressHyperlinkButton.Content.ToString());
                            Clipboard.SetContent(dataPackage);
                            Debug.WriteLine("Login address copied to clipboard.");
                        }
                        catch (Exception copyEx)
                        {
                            Debug.WriteLine($"Error copying login address: {copyEx.Message}");
                        }
                    };
                }
                else
                {
                    Debug.WriteLine("Invalid IP or Port in config file.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading login address: {ex.Message}");
            }
        }


        private void LoadCarouselImages()
        {
            // 图片列表
            var images = new List<string>
        {
            "ms-appx:///Assets/acg/bg1.jpg",
            "ms-appx:///Assets/acg/bg2.jpg",
            "ms-appx:///Assets/acg/bg3.jpg",
            "ms-appx:///Assets/acg/bg4.jpg",
            "ms-appx:///Assets/acg/bg5.jpg",
            "ms-appx:///Assets/acg/001.jpg",
            "ms-appx:///Assets/acg/002.jpg",
            "ms-appx:///Assets/acg/003.jpg",
            "ms-appx:///Assets/acg/004.jpg",
            "ms-appx:///Assets/acg/005.jpg",
            "ms-appx:///Assets/acg/006.jpg",
            "ms-appx:///Assets/acg/bg01.jpg",
            "ms-appx:///Assets/acg/bg02.jpg",
            "ms-appx:///Assets/acg/bg03.jpg"
        };

            ImageCarousel.ItemsSource = images;
        }

        private async void LoadClientVersion()
        {
            try
            {
                string clientPath = App.ManagerConfig.InstallPath;
                if (string.IsNullOrEmpty(clientPath))
                {
                    return;
                }

                string exeFilePath = Path.Combine(clientPath, "EscapeFromTarkov.exe");
                if (!File.Exists(exeFilePath))
                {
                    return;
                }

                string serverPath = App.ManagerConfig.AkiServerPath;
                if (string.IsNullOrEmpty(serverPath))
                {
                    return;
                }

                string configFilePath = Path.Combine(serverPath, @"Aki_Data\Server\configs\core.json");
                if (!File.Exists(configFilePath))
                {
                    configFilePath = Path.Combine(serverPath, @"SPT_Data\Server\configs\core.json");
                }

                if (!File.Exists(configFilePath))
                {
                    return;
                }

                // 获取 exe 文件版本信息

                string jsonContent = await File.ReadAllTextAsync(configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                var versionInfo = FileVersionInfo.GetVersionInfo(exeFilePath);
                string exeVersion = versionInfo.FileVersion;

                // 显示到界面
                ClientVersionText.Text = $" 当前客户端版本: {exeVersion}";

                // 检查版本匹配
                string compatibleVersion = config["compatibleTarkovVersion"]?.ToString();
                if (!string.IsNullOrEmpty(compatibleVersion) && exeVersion != compatibleVersion)
                {
                    ClientVersionText.Text = $"当前客户端版本: {exeVersion} （警告：客户端版本不匹配，可能导致无法启动）";
                    ClientVersionText.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    ClientVersionText.Text = $"当前客户端版本: {exeVersion}";
                    ClientVersionText.Foreground = new SolidColorBrush(Colors.LightSeaGreen);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading server config: {ex.Message}");
            }
        }


        private async void LoadServerVersion()
        {
            try
            {
                string serverPath = App.ManagerConfig.AkiServerPath;
                if (string.IsNullOrEmpty(serverPath))
                {
                    return;
                }

                // 检查目录文件夹的唯一性
                string[] possibleFolders = { "Aki_Data", "SPT_Data" };
                List<string> foundFolders = new List<string>();

                foreach (var folder in possibleFolders)
                {
                    string fullPath = Path.Combine(serverPath, folder);
                    if (Directory.Exists(fullPath))
                    {
                        foundFolders.Add(fullPath);
                    }
                }

                if (foundFolders.Count == 0)
                {
                    Debug.WriteLine("未找到有效的服务端目录。");
                    return;
                }
                else if (foundFolders.Count > 1)
                {
                    Debug.WriteLine($"检测到多个服务端目录，请保留一个并删除其他目录：{string.Join(", ", foundFolders)}");
                    return;
                }

                // 确定唯一的服务端目录
                string selectedFolder = foundFolders[0];
                string configFilePath = Path.Combine(selectedFolder, @"Server\configs\core.json");

                if (!File.Exists(configFilePath))
                {
                    Debug.WriteLine("未找到配置文件 core.json。");
                    return;
                }

                // 读取配置文件并解析版本信息
                string jsonContent = await File.ReadAllTextAsync(configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                // 根据文件夹名称动态获取版本号键
                string versionKey = selectedFolder.EndsWith("Aki_Data") ? "akiVersion" : "sptVersion";
                string serverVersion = config.ContainsKey(versionKey) ? config[versionKey]?.ToString() : "未知版本";
                string tarkovVersion = config.ContainsKey("compatibleTarkovVersion") ? config["compatibleTarkovVersion"]?.ToString() : "未知版本";

                // 显示版本信息
                ServerVersionText.Text = $" {serverVersion}";
                CompatibleTarkovVersionText.Text = $" {tarkovVersion}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading server config: {ex.Message}");
            }
        }


        public async void LookForUpdate()
        {
            try
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                string latestVersion = await Utils.utilsHttpClient.GetStringAsync(@"https://gitee.com/Neko17-Offical/neko-manager/raw/master/VERSION");
                latestVersion = latestVersion.Trim();

                Version newVersion = new(latestVersion);
                int compare = currentVersion.CompareTo(newVersion);

                if (compare < 0)
                {
                    // 使用 ShowNotification 显示更新通知
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        ShowNotification("发现更新", $"Neko EFT Manager Pro 有可用更新！         版本：{latestVersion}", InfoBarSeverity.Warning, false, TimeSpan.FromSeconds(200));
                    });
                }
            }
            catch (Exception ex)
            {
                Loggy.LogToFile("LookForUpdate: " + ex.Message);
                Utils.ShowInfoBarWithLogButton("错误", "无法获取更新.", InfoBarSeverity.Error);
            }
        }
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://sns.oddba.cn/132504.html"); // 替换为更新页面的URL
            await Launcher.LaunchUriAsync(uri);
        }

        private void ShowNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, bool isPersistent, TimeSpan? duration = null)
        {
            UpdateNotificationInfoBar.Title = title;
            UpdateNotificationInfoBar.Message = message;
            UpdateNotificationInfoBar.Severity = severity;
            UpdateNotificationInfoBar.IsOpen = true; // 显示通知

            if (!isPersistent) // 如果不是常驻通知
            {
                if (duration.HasValue) // 检查是否有停留时间
                {
                    _ = Task.Delay(duration.Value).ContinueWith(_ =>
                    {
                        // 在停留时间结束后关闭 InfoBar
                        UpdateNotificationInfoBar.IsOpen = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        private void ShowInfoNotification(string title, string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity, bool isPersistent, TimeSpan? duration = null)
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

            UpdateNotificationQueue.Show(notification);
        }
        private void ShowInfoNotification(string title, string message, InfoBarSeverity severity)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Severity = severity,
            };

            NotificationQueue.Show(notification);
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
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

            if (AkiServer.State == AkiServer.RunningState.NOT_RUNNING)
            {
                if (AkiServer.IsUnhandledInstanceRunning())
                {
                    AddConsole("SPT 正在运行。请关闭任何正在运行的 SPT 实例.");
                    UpdateConnectButtonState();
                    return;
                }

                if (!File.Exists(AkiServer.FilePath))
                {
                    AddConsole("未找到 SPT。请在启动服务器之前在'设置'选项中配置 SPT 路径.");
                    UpdateConnectButtonState();
                    return;
                }

                AddConsole("启动服务端...");

                try
                {
                    AkiServer.Start();
                    UpdateConnectButtonState();
                }
                catch (Exception ex)
                {
                    AddConsole(ex.Message);
                }
            }
            else
            {
                AddConsole("正在停止...");

                try
                {
                    AkiServer.Stop();
                    UpdateConnectButtonState();
                }
                catch (Exception ex)
                {
                    AddConsole(ex.Message);
                }
            }
        }


        private Dictionary<Paragraph, string> paragraphTags = new();

        private void AddConsole(string text)
        {
            if (text == null)
                return;

            // 检测进度条格式
            var progressMatch = Regex.Match(text, @"->\s*(\d+)\s*/\s*(\d+)");
            if (progressMatch.Success)
            {
                // 提取当前进度和总值
                int current = int.Parse(progressMatch.Groups[1].Value);
                int total = int.Parse(progressMatch.Groups[2].Value);
                double percentage = (double)current / total * 100;

                // 查找已有进度条段落
                Paragraph progressParagraph = null;

                foreach (var item in ConsoleLog.Blocks.OfType<Paragraph>())
                {
                    if (paragraphTags.ContainsKey(item) && paragraphTags[item] == "Progress")
                    {
                        progressParagraph = item;
                        break;
                    }
                }

                // 如果没有找到现有进度条段落，创建一个新的
                if (progressParagraph == null)
                {
                    progressParagraph = new Paragraph();
                    ConsoleLog.Blocks.Add(progressParagraph);
                    paragraphTags[progressParagraph] = "Progress"; // 标记此段落为进度条
                }

                // 清除现有进度条文本
                progressParagraph.Inlines.Clear();

                // 更新进度条文本
                progressParagraph.Inlines.Add(new Run
                {
                    Text = $"正在导入数据库: {current} / {total} ({percentage:F1}%)",
                    Foreground = new SolidColorBrush(Colors.DarkCyan)
                });

                return;
            }

            // 默认文本处理
            Paragraph defaultParagraph = new();
            Run defaultRun = new()
            {
                Text = Regex.Replace(text, @"(\[\d{1,2}m)|(\[\d{1}[a-zA-Z])|(\[\d{1};\d{1}[a-zA-Z])", "")
            };

            // 根据特定关键词设置颜色
            if (text.Contains("[ERROR]")) defaultRun.Foreground = new SolidColorBrush(Colors.Red);
            if (text.Contains("webserver已于")) defaultRun.Foreground = new SolidColorBrush(Colors.Green);
            if (text.Contains("websocket启动于")) defaultRun.Foreground = new SolidColorBrush(Colors.Green);
            if (text.Contains("服务端正在运行, 玩的开心!!")) defaultRun.Foreground = new SolidColorBrush(Colors.Green);
            if (text.Contains("Profile:")) defaultRun.Foreground = new SolidColorBrush(Colors.Orange);
            if (text.Contains("[WS] 玩家：")) defaultRun.Foreground = new SolidColorBrush(Colors.Orange);
            if (text.Contains("Error")) defaultRun.Foreground = new SolidColorBrush(Colors.Red);
            if (text.Contains("服务端已启动!")) defaultRun.Foreground = new SolidColorBrush(Colors.Green);
            if (text.Contains("服务端已停止!")) defaultRun.Foreground = new SolidColorBrush(Colors.Red);
            if (text.Contains("服务器意外停止！检查控制台是否有错误.")) defaultRun.Foreground = new SolidColorBrush(Colors.Red);
            if (text.Contains("点击启动等待初始化完成自动启动游戏，过程全程自动化，除了注册账号您无需做任何操作。"))
                defaultRun.Foreground = new SolidColorBrush(Colors.SeaGreen);

            defaultParagraph.Inlines.Add(defaultRun);
            ConsoleLog.Blocks.Add(defaultParagraph);
        }




        private void AkiServer_OutputDataReceived(object sender, DataReceivedEventArgs e)

        {
            var window = App.MainWindow;
            if (window == null)
            {
                Debug.WriteLine("window is null");
                return;
            }
            window.DispatcherQueue.TryEnqueue(() => AddConsole(e.Data));
        }

        private void AkiServer_RunningStateChanged(AkiServer.RunningState runningState)
        {
            var window = App.MainWindow;
            if (window == null)
            {
                Debug.WriteLine("window is null");
                return;
            }

            window.DispatcherQueue.TryEnqueue(() =>
            {
                switch (runningState)
                {
                    case AkiServer.RunningState.RUNNING:
                        {
                            AddConsole("服务端已启动!");
                            StartServerButtonSymbolIcon.Symbol = Symbol.Stop;
                            StartServerButtonTextBlock.Text = "停止服务端";
                        }
                        break;
                    case AkiServer.RunningState.NOT_RUNNING:
                        {
                            AddConsole("服务端已停止!");
                            StartServerButtonSymbolIcon.Symbol = Symbol.Play;
                            StartServerButtonTextBlock.Text = "仅启动服务端";
                        }
                        break;
                    case AkiServer.RunningState.STOPPED_UNEXPECTEDLY:
                        {
                            AddConsole("服务器意外停止！检查控制台是否有错误.");
                            StartServerButtonSymbolIcon.Symbol = Symbol.Play;
                            StartServerButtonTextBlock.Text = "仅启动服务端";
                        }
                        break;
                }
            });
        }

        

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateConnectButtonState();

        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateConnectButtonState();
        }



        private const string FileSelectorExecutable = "FileSelector.exe";
        private const string ClientArgument = "client";
        private const string ServerArgument = "server";

        

        public static event Action GOAInstallPathChanged;

        private void SelectClientPath_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                var process = StartFileSelector(ClientArgument);

                // 等待文件选择器完成
                process.WaitForExit();

                // 重新加载配置，以确保获取到最新的路径
                ManagerConfig.Load();
                var selectedPath = App.ManagerConfig.InstallPath; // 假设路径已经在文件选择器中更新
                if (!string.IsNullOrEmpty(selectedPath) && File.Exists(Path.Combine(selectedPath, "EscapeFromTarkov.exe")))
                {
                    Utils.CheckEFTVersion(selectedPath);
                    Utils.CheckSITVersion(selectedPath);
                    ManagerConfig.SaveAccountInfo();
                    ToastNotificationHelper.ShowNotification("设置", $"EFT客户端路径已更改至：\n {selectedPath} \n ", "确认", null, "通知", "提示");
                    GOAInstallPathChanged?.Invoke();
                }
                else
                {
                    ToastNotificationHelper.ShowNotification("错误", $"所选文件夹无效。确保路径包含游戏可执行文件：\n 目录未选择 \n ", "确认", null, "通知", "错误");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationHelper.ShowNotification("错误", $"启动文件选择器时发生错误: {ex.Message}", "确认", null, "通知", "警告");
            }
        }

        private async void SelectServerPath_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                var process = StartFileSelector(ServerArgument);

                // 等待文件选择器完成
                await process.WaitForExitAsync();

                // 重新加载配置，以确保获取到最新的路径
                ManagerConfig.Load();
                string akiServerPath = App.ManagerConfig.AkiServerPath; // 假设路径已经在文件选择器中更新

                // 检查路径下的可执行文件
                string akiServerExePath = Path.Combine(akiServerPath, "Aki.Server.exe");
                string sptServerExePath = Path.Combine(akiServerPath, "SPT.Server.exe");
                string serverExePath = Path.Combine(akiServerPath, "Server.exe");

                bool akiServerExists = File.Exists(akiServerExePath);
                bool sptServerExists = File.Exists(sptServerExePath);
                bool serverExists = File.Exists(serverExePath);

                if (akiServerExists && sptServerExists && serverExists)
                {
                    ToastNotificationHelper.ShowNotification("错误", "所选文件夹包含多个服务端版本，请检查路径是否正确", "确认", null, "通知", "错误");
                }
                else if (akiServerExists || sptServerExists || serverExists)
                {
                    ManagerConfig.SaveAccountInfo(); // 可能不需要在这里保存，因为路径已经在文件选择器中更新
                    ToastNotificationHelper.ShowNotification("设置", $"SPT安装目录已更改至：\n {akiServerPath} \n ", "确认", null, "通知", "提示");
                    GOAInstallPathChanged?.Invoke();

                }
                else
                {
                    ToastNotificationHelper.ShowNotification("错误", $"所选文件夹无效。确保路径包含可用的服务端可执行文件：\n {akiServerPath} \n ", "确认", null, "通知", "错误");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationHelper.ShowNotification("错误", $"选择目录时发生错误: {ex.Message}", "确认", null, "通知", "警告");
            }
        }


        private static Process StartFileSelector(string argument)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileSelectorExecutable),
                Arguments = argument,
                UseShellExecute = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            return Process.Start(processStartInfo);
        }


        private void UpdateConnectButtonState()
        {
            // 检查所有必填字段是否填写完整
            bool isAllFieldsFilled = !string.IsNullOrEmpty(AddressBox.Text) &&
                                     !string.IsNullOrEmpty(UsernameBox.Text) &&
                                     !string.IsNullOrEmpty(App.ManagerConfig.InstallPath);
                                     //!string.IsNullOrEmpty(ServerIPBox.Text) &&
                                     //!string.IsNullOrEmpty(ServerLoginIPBox.Text) &&
                                     //!string.IsNullOrEmpty(ServerPortBox.Text) &&
                                     //!string.IsNullOrEmpty(ServerLoginPortBox.Text);

            if (!isAllFieldsFilled)
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = "先填写所有内容."
                });
                ConnectButton.IsEnabled = false;
            }
            else
            {
                ToolTipService.SetToolTip(ConnectButton, new ToolTip()
                {
                    Content = $"尝试连接 {AddressBox.Text} 并启动游戏."
                });
                ConnectButton.IsEnabled = true;
            }

            // 更新启动/连接按钮文本
            if (AkiServer.State == AkiServer.RunningState.NOT_RUNNING)
            {
                //ConnectButton_Text.Text = "一键启动";
            }
            else
            {
                //ConnectButton_Text.Text = "连接游戏";
            }
        }


        // 绑定所有输入框的事件处理程序
        private void BindInputEvents()
        {
            AddressBox.TextChanged += InputBox_TextChanged;
            UsernameBox.TextChanged += InputBox_TextChanged;
            //PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            //ServerIPBox.TextChanged += ServerIPBox_TextChanged;
            //ServerLoginIPBox.TextChanged += ServerLoginIPBox_TextChanged;
            //ServerPortBox.TextChanged += ServerPortBox_TextChanged;
            //ServerLoginPortBox.TextChanged += ServerLoginPortBox_TextChanged;
        }



        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // 判断服务端是否正在运行
            if (AkiServer.State != AkiServer.RunningState.RUNNING)
            {
                // 服务端未运行，启动服务端
                if (!CheckVersionCompatibility())
                {
                    ShowInfoNotification(
                        "游戏未能启动",
                        "Escape From Tarkov 未能启动。\n当前客户端与服务端所需的客户端版本不匹配，请更新版本后重试。",
                        InfoBarSeverity.Error,
                        false,
                        TimeSpan.FromSeconds(5)
                    );

                    ToastNotificationHelper.ShowNotification("版本不匹配", "客户端与服务端版本不匹配，请更新版本后重试。", "确认", (arg) => { }, "通知", "错误");
                    return;
                }

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
                    AddConsole("未找到 SPT。请在启动服务器之前在'设置'选项中配置 SPT 路径.");
                    return;
                }

                AddConsole("启动服务端...");
                ShowInfoNotification("正在启动服务端...", "正在等待服务端完成启动，请耐心等待...", InfoBarSeverity.Informational, false, TimeSpan.FromSeconds(5));

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

                AddConsole("服务端已启动，等待初始化完成...");
                UpdateConnectButtonState();
                ToastNotificationHelper.ShowNotification("服务端已启动", "服务端已启动，等待初始化完成...", "确认", (arg) => { }, "通知", "测试");

                // 设置超时时间（例如90秒）
                TimeSpan timeout = TimeSpan.FromSeconds(90);
                DateTime startTime = DateTime.Now;
                var serverInitializedTaskSource = new TaskCompletionSource<bool>();

                void OnServerOutputReceived(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null && e.Data.Contains("服务端正在运行, 玩的开心!!"))
                    {
                        serverInitializedTaskSource.SetResult(true);
                    }
                }

                AkiServer.OutputDataReceived += OnServerOutputReceived;
                var delayTask = Task.Delay(timeout);
                var completedTask = await Task.WhenAny(serverInitializedTaskSource.Task, delayTask);
                AkiServer.OutputDataReceived -= OnServerOutputReceived;

                if (completedTask == delayTask)
                {
                    ToastNotificationHelper.ShowNotification("错误", "未能启动游戏：服务端超时未响应", "确认", (arg) => { }, "通知", "错误");
                    return;
                }

                ToastNotificationHelper.ShowNotification("初始化完成", "服务端初始化完成，开始连接...", "确认", (arg) => { }, "通知", "完成");
            }

            // **使用 LoginHelper 进行登录**
            string installPath = App.ManagerConfig.AkiServerPath; // 服务器文件路径
            string serverAddress = AddressBox.Text;  // 服务器地址
            string username = UsernameBox.Text;      // 用户名
            string password = PasswordBox.Password;  // 密码

            var loginHelper = new LoginHelper(installPath, serverAddress, username, password);

            // **登录服务器**
            string token = await loginHelper.Connect();
            if (token == "error" || string.IsNullOrEmpty(token))
            {
                ShowInfoNotification("登录失败", "无法登录至服务器，请检查您的用户名或密码。", InfoBarSeverity.Error, false, TimeSpan.FromSeconds(5));
                return;
            }

            ToastNotificationHelper.ShowNotification("登录完成", $"成功登录至服务器：\n {serverAddress} \n 游戏即将启动", "确认", (arg) => { }, "通知", "成功");

            // **启动游戏**
            await loginHelper.StartGame(token);
        }




        //private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        //{


        //    // 判断服务端是否正在运行
        //    if (AkiServer.State != AkiServer.RunningState.RUNNING)
        //    {
        //        // 服务端未运行，启动服务端


        //        if (!CheckVersionCompatibility())
        //        {
        //            TimeSpan gameDuration = DateTime.Now - DateTime.Now;
        //            // 游戏退出后关闭现有通知并显示新的通知
        //            NotificationInfoBar.IsOpen = false; // 关闭现有通知
        //            ShowInfoNotification(
        //                "游戏未能启动",
        //                $"Escape From Tarkov 未能启动。\n当前客户端与服务端所需的客户端版本不匹配，请更新版本后重试。",
        //                InfoBarSeverity.Error,
        //                false,
        //                TimeSpan.FromSeconds(5) // 非常驻通知，5秒后关闭
        //            );

        //            ToastNotificationHelper.ShowNotification("版本不匹配", $"客户端与服务端版本不匹配，请更新版本后重试。", "确认", (arg) =>
        //            {
        //                // 执行其他操作...
        //            }, "通知", "错误");
        //            return;
        //        }
        //        try
        //        {
        //            AkiServer.DetermineExeName();
        //        }
        //        catch (Exception ex)
        //        {
        //            AddConsole(ex.Message);
        //            return;
        //        }

        //        if (AkiServer.IsUnhandledInstanceRunning())
        //        {
        //            AddConsole("SPT 正在运行。请关闭任何正在运行的 SPT 实例.");
        //            return;
        //        }

        //        if (!File.Exists(AkiServer.FilePath))
        //        {
        //            AddConsole("未找到 SPT。请在启动服务器之前在'设置'选项中配置 SPT 路径.");
        //            return;
        //        }

        //        AddConsole("启动服务端...");

        //        NotificationInfoBar.IsOpen = false; // 关闭现有通知
        //        ShowInfoNotification(
        //            "正在启动服务端...",
        //            $"正在等待服务端完成启动，请耐心等待...",
        //            InfoBarSeverity.Informational,
        //            false,
        //            TimeSpan.FromSeconds(5) // 非常驻通知，5秒后关闭
        //        );

        //        try
        //        {
        //            AkiServer.Start();
        //        }
        //        catch (Exception ex)
        //        {
        //            AddConsole(ex.Message);
        //            return;
        //        }

        //        // 等待服务端启动
        //        while (AkiServer.State != AkiServer.RunningState.RUNNING)
        //        {
        //            await Task.Delay(500);
        //        }

        //        // 确认服务端已启动
        //        AddConsole("服务端已启动，等待初始化完成...");
        //        UpdateConnectButtonState();
        //        ToastNotificationHelper.ShowNotification("服务端已启动", $"服务端已启动，等待初始化完成...", "确认", (arg) =>
        //        {
        //            // 执行其他操作...
        //        }, "通知", "测试");

        //        // 设置超时时间（例如90秒）
        //        TimeSpan timeout = TimeSpan.FromSeconds(90);
        //        DateTime startTime = DateTime.Now;

        //        // 使用 TaskCompletionSource 来等待服务端输出 "服务端初始化完成"
        //        var serverInitializedTaskSource = new TaskCompletionSource<bool>();

        //        void OnServerOutputReceived(object sender, DataReceivedEventArgs e)
        //        {
        //            if (e.Data != null && e.Data.Contains("服务端正在运行, 玩的开心!!"))
        //            {
        //                serverInitializedTaskSource.SetResult(true);
        //            }
        //        }

        //        // 订阅服务端输出事件
        //        AkiServer.OutputDataReceived += OnServerOutputReceived;

        //        // 等待服务端输出 "服务端初始化完成" 或超时
        //        var delayTask = Task.Delay(timeout);
        //        var completedTask = await Task.WhenAny(serverInitializedTaskSource.Task, delayTask);

        //        // 取消订阅服务端输出事件
        //        AkiServer.OutputDataReceived -= OnServerOutputReceived;

        //        if (completedTask == delayTask)
        //        {
        //            ToastNotificationHelper.ShowNotification("错误", $"未能启动游戏：服务端超时未响应", "确认", (arg) =>
        //            {
        //                // 执行其他操作...
        //            }, "通知", "错误");
        //            return;
        //        }


        //        // 检查版本匹配


        //        // 确认服务端已初始化完成
        //        ToastNotificationHelper.ShowNotification("初始化完成", $"服务端初始化完成，开始连接...", "确认", (arg) =>
        //        {
        //            // 执行其他操作...
        //        }, "通知", "完成");
        //    }

        //    // 服务端已运行或已启动并初始化完成，执行连接操作
        //    UpdateConnectButtonState();

        //    string returnData = await Connect();

        //    if (returnData == "error" || string.IsNullOrEmpty(returnData))
        //    {
        //        return;
        //    }

        //    ToastNotificationHelper.ShowNotification("登录完成", $"成功登录至服务器：\n {AddressBox.Text} \n 游戏即将启动", "确认", (arg) =>
        //    {
        //        // 执行其他操作...
        //    }, "通知", "错误");

        //    // 启动游戏
        //    await StartGame(returnData);
        //}






        //private async Task<string> Connect()
        //{
        //    ManagerConfig.Save((bool)RememberMeCheck.IsChecked);

        //    if (App.ManagerConfig.InstallPath == null)
        //    {
        //        ContentDialog contentDialog = new()
        //        {
        //            XamlRoot = Content.XamlRoot,
        //            Title = "配置错误",
        //            Content = "未配置 \"安装路径\"。转到设置配置安装路径。",
        //            CloseButtonText = "确定"
        //        };

        //        await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        //        return "error";
        //    }

        //    if (!File.Exists(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov.exe"))
        //    {
        //        ContentDialog contentDialog = new()
        //        {
        //            XamlRoot = Content.XamlRoot,
        //            Title = "安装错误",
        //            Content = "无法在安装路径中找到 \"EscapeFromTarkov.exe，请检查你的安装路径",
        //            CloseButtonText = "确定"
        //        };

        //        await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        //        return "error";
        //    }

        //    if (AddressBox.Text.Length == 0 || UsernameBox.Text.Length == 0 || PasswordBox.Password.Length == 0)
        //    {
        //        ContentDialog contentDialog = new()
        //        {
        //            XamlRoot = Content.XamlRoot,
        //            Title = "输入错误",
        //            Content = "地址、用户名或密码丢失.",
        //            CloseButtonText = "确定"
        //        };

        //        await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        //        return "error";
        //    }

        //    if (!AddressBox.Text.Contains(@"http://"))
        //    {
        //        AddressBox.Text = @"http://" + AddressBox.Text;
        //    }

        //    if (AddressBox.Text.EndsWith(@"/") || AddressBox.Text.EndsWith(@"\"))
        //    {
        //        AddressBox.Text = AddressBox.Text.Remove(AddressBox.Text.Length - 1, 1);
        //    }

        //    string returnData = await LoginToServer();
        //    return returnData;
        //}

        //private async Task<string> LoginToServer()
        //{
        //    TarkovRequesting requesting = new TarkovRequesting(null, AddressBox.Text, false);

        //    Dictionary<string, string> data = new Dictionary<string, string>
        //    {
        //        { "username", UsernameBox.Text },
        //        { "email", UsernameBox.Text },
        //        { "edition", "Edge Of Darkness" },
        //        { "password", PasswordBox.Password },
        //        { "backendUrl", AddressBox.Text }
        //    };

        //    try
        //    {
        //        var returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));

        //        if (returnData == "FAILED")
        //        {
        //            ContentDialog createAccountDialog = new()
        //            {
        //                XamlRoot = Content.XamlRoot,
        //                Title = "账号不存在",
        //                Content = "未找到您的帐户，您想用这些凭据注册一个新帐户吗？",
        //                IsPrimaryButtonEnabled = true,
        //                PrimaryButtonText = "确定",
        //                CloseButtonText = "取消"
        //            };

        //            ContentDialogResult msgBoxResult = await createAccountDialog.ShowAsync(ContentDialogPlacement.InPlace);
        //            if (msgBoxResult == ContentDialogResult.Primary)
        //            {
        //                SelectEditionDialog selectWindow = new()
        //                {
        //                    XamlRoot = Content.XamlRoot
        //                };
        //                await selectWindow.ShowAsync();
        //                string edition = selectWindow.edition;

        //                if (edition != null)
        //                    data["edition"] = edition;

        //                returnData = await requesting.PostJsonAsync("/launcher/profile/register", System.Text.Json.JsonSerializer.Serialize(data));
        //                returnData = await requesting.PostJsonAsync("/launcher/profile/login", System.Text.Json.JsonSerializer.Serialize(data));
        //            }
        //            else
        //            {
        //                return null;
        //            }
        //        }
        //        else if (returnData == "INVALID_PASSWORD")
        //        {
        //            ToastNotificationHelper.ShowNotification("登录失败", $"密码错误!", "确认", (arg) =>
        //            {
        //                // 执行其他操作...
        //            }, "通知", "错误");
        //            return "error";
        //        }

        //        return returnData;
        //    }
        //    catch (System.Net.WebException webEx)
        //    {
        //        ContentDialog contentDialog = new()
        //        {
        //            XamlRoot = Content.XamlRoot,
        //            Title = "登录失败",
        //            Content = $"无法与服务器通信\n{webEx.Message}",
        //            CloseButtonText = "确定"
        //        };

        //        Loggy.LogToFile("Login Error: " + webEx);

        //        await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        //        return "error";
        //    }
        //    catch (Exception ex)
        //    {
        //        ContentDialog contentDialog = new()
        //        {
        //            XamlRoot = Content.XamlRoot,
        //            Title = "登录失败",
        //            Content = $"无法与服务器通信\n{ex.Message}",
        //            CloseButtonText = "确定"
        //        };

        //        Loggy.LogToFile("Login Error: " + ex);

        //        await contentDialog.ShowAsync(ContentDialogPlacement.InPlace);
        //        return "error";
        //    }
        //}


        //private async Task StartGame(string token)
        //{
        //    try
        //    {

        //        NotificationInfoBar.IsOpen = false; // 关闭现有通知
        //        ShowInfoNotification(
        //            "游戏正在启动.....",
        //            $"正在修补客户端，请耐心等待...",
        //            InfoBarSeverity.Informational,
        //            false,
        //            TimeSpan.FromSeconds(5) // 非常驻通知，5秒后关闭
        //        );




        //        var patchRunner = new ProgressReportingPatchRunner(App.ManagerConfig.InstallPath);
        //        await foreach (var result in patchRunner.PatchFiles())
        //        {
        //            if (!result.OK)
        //            {
        //                string errorMessage = result.Status switch
        //                {
        //                    PatchResultType.InputLengthMismatch => "补丁失败：输入文件长度与预期值不匹配，请检查你的游戏版本是否匹配。",
        //                    PatchResultType.InputChecksumMismatch => "补丁失败：输入文件校验和与预期值不匹配，请检查你的游戏文件是否已损坏。",
        //                    PatchResultType.OutputChecksumMismatch => "补丁失败：输出文件校验和与预期值不匹配，补丁过程中可能产生了问题，请重新安装。",
        //                    _ => $"补丁失败：未知错误（{result.Status}）。"
        //                };


        //                NotificationInfoBar.IsOpen = false; // 关闭现有通知
        //                ShowInfoNotification(
        //                    "游戏启动失败",
        //                    $"[({result.Status})]",
        //                    InfoBarSeverity.Error,
        //                    false,
        //                    TimeSpan.FromSeconds(5) // 非常驻通知，5秒后关闭
        //                );

        //                ToastNotificationHelper.ShowNotification("补丁错误", errorMessage, "确认", (arg) =>
        //                {
        //                    // 执行其他操作...
        //                }, "通知", "错误");

        //                return;
        //            }
        //        }

        //        //string arguments = $"-token={token} -config={{\"BackendUrl\":\"{AddressBox.Text}\",\"Version\":\"live\"}}";

        //        string arguments = $"-token={token} -config={{'BackendUrl':'{AddressBox.Text}','MatchingVersion':'live','Version':'live'}}";

        //        var startInfo = new ProcessStartInfo
        //        {
        //            FileName = Path.Combine(App.ManagerConfig.InstallPath, "EscapeFromTarkov.exe"),
        //            Arguments = arguments,
        //            UseShellExecute = false,
        //            RedirectStandardOutput = true,
        //            RedirectStandardError = true,
        //            CreateNoWindow = true
        //        };

        //        DateTime startTime = DateTime.Now; // 游戏开始时间
        //        using (Process process = Process.Start(startInfo))
        //        {
        //            // 显示常驻通知：游戏启动成功，正在运行
        //            ShowInfoNotification("游戏正在运行", "Escape From Tarkov 正在运行中...", InfoBarSeverity.Warning);

        //            using (StreamReader outputReader = process.StandardOutput)
        //            using (StreamReader errorReader = process.StandardError)
        //            {
        //                string output = await outputReader.ReadToEndAsync();
        //                string error = await errorReader.ReadToEndAsync();

        //                Debug.WriteLine("Output: " + output);
        //                Debug.WriteLine("Error: " + error);
        //            }

        //            await process.WaitForExitAsync();

        //            TimeSpan gameDuration = DateTime.Now - startTime;
        //            // 游戏退出后关闭现有通知并显示新的通知
        //            NotificationInfoBar.IsOpen = false; // 关闭现有通知
        //            ShowInfoNotification(
        //                "游戏结束",
        //                $"Escape From Tarkov 已退出。\n本次游戏时间: {gameDuration.Hours}小时 {gameDuration.Minutes}分钟 {gameDuration.Seconds}秒\n当前时间: {DateTime.Now:HH:mm:ss}",
        //                InfoBarSeverity.Informational,
        //                false,
        //                TimeSpan.FromSeconds(5) // 非常驻通知，5秒后关闭
        //            );
        //        }

        //        if (App.ManagerConfig.CloseAfterLaunch)
        //        {
        //            Application.Current.Exit();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ToastNotificationHelper.ShowNotification("错误", $"启动游戏时发生错误：\n {ex.Message} ", "确认", (arg) =>
        //        {
        //            // 执行其他操作...
        //        }, "通知", "错误");
        //    }
        //}



        private async void ServerManagerButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            SPTServerManager SPTServerManager = new SPTServerManager();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            SPTServerManager.Activate();
            
        }
        private void ConsoleLog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ConsoleLogScroller.ScrollToVerticalOffset(ConsoleLogScroller.ScrollableHeight);
        }

        private void CommandInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                string command = CommandInputBox.Text;
                SendCommandToServer(command);
                CommandInputBox.Text = string.Empty; // 清空输入框
            }
        }

        private void SendCommandToServer(string command)
        {
            if (AkiServer.Process == null || AkiServer.Process.HasExited)
            {
                AddConsole("服务端未运行，无法发送命令。");
                return;
            }

            if (AkiServer.Writer == null)
            {
                AddConsole("标准输入流未初始化。");
                return;
            }

            try
            {
                if (AkiServer.Writer.BaseStream.CanWrite)
                {
                    AkiServer.Writer.WriteLine(command);
                    AkiServer.Writer.Flush(); // 确保命令被及时发送
                    AddConsole($"命令已发送: {command}");
                }
                else
                {
                    AddConsole("无法写入到服务端的标准输入流。");
                }
            }
            catch (Exception ex)
            {
                AddConsole($"发送命令时出错: {ex.Message}");
            }
        }


        private bool CheckVersionCompatibility()
        {
            try
            {
                // 获取客户端路径
                string clientPath = App.ManagerConfig.InstallPath;
                if (string.IsNullOrEmpty(clientPath))
                {
                    Debug.WriteLine("客户端路径未设置");
                    return false;
                }

                string exeFilePath = Path.Combine(clientPath, "EscapeFromTarkov.exe");
                if (!File.Exists(exeFilePath))
                {
                    Debug.WriteLine("客户端文件未找到");
                    return false;
                }

                // 获取服务端路径
                string serverPath = App.ManagerConfig.AkiServerPath;
                if (string.IsNullOrEmpty(serverPath))
                {
                    Debug.WriteLine("服务端路径未设置");
                    return false;
                }

                // 获取服务端配置文件路径
                string configFilePath = Path.Combine(serverPath, @"Aki_Data\Server\configs\core.json");
                if (!File.Exists(configFilePath))
                {
                    configFilePath = Path.Combine(serverPath, @"SPT_Data\Server\configs\core.json");
                }

                if (!File.Exists(configFilePath))
                {
                    Debug.WriteLine("服务端配置文件未找到");
                    return false;
                }

                // 获取客户端版本
                var versionInfo = FileVersionInfo.GetVersionInfo(exeFilePath);
                string exeVersion = versionInfo.FileVersion;

                // 获取服务端所需的客户端版本
                string jsonContent = File.ReadAllText(configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                string compatibleVersion = config["compatibleTarkovVersion"]?.ToString();

                if (string.IsNullOrEmpty(compatibleVersion))
                {
                    Debug.WriteLine("服务端未指定兼容的客户端版本");
                    return false;
                }

                // 比较版本
                if (exeVersion == compatibleVersion)
                {
                    Debug.WriteLine($"版本匹配: 客户端版本 {exeVersion}, 服务端要求版本 {compatibleVersion}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"版本不匹配: 客户端版本 {exeVersion}, 服务端要求版本 {compatibleVersion}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查版本兼容性时发生错误: {ex.Message}");
                return false;
            }
        }





    }
}
