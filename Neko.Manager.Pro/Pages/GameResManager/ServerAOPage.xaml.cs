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
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using System.Threading;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages.GameResManager
{
    /// <summary>
    /// ServerPage for handling SPT-AKI Server execution and console output.
    /// </summary>
    public sealed partial class ServerAOPage : Page
    {
        MainWindow? window = App.m_window as MainWindow;
        

        public ServerAOPage()
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

            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            //BindInputEvents();
            // 启动后台更新 CPU、内存、磁盘等信息
            Task.Run(() => UpdateSystemStatsAsync());


            DataContext = App.ManagerConfig;

            AkiServer.OutputDataReceived += AkiServer_OutputDataReceived;
            AkiServer.RunningStateChanged += AkiServer_RunningStateChanged;
            
            LoadServerConfig();


            //UpdateConnectButtonState();
            this.Loaded += GameAOPage_Loaded;

        }

        private void GameAOPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 在页面加载完成后调用 UpdateConnectButtonState 方法
            //UpdateConnectButtonState();
            //StartMonitoring();

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

                    // 获取服务端进程名称并去除 .exe 后缀
                    string serverProcessName = Path.GetFileNameWithoutExtension(AkiServer.ExeName);
                    AddConsole($"服务端进程名称: {serverProcessName}");

                    // 传递给监控方法或存储到全局变量
                    MonitorServerPerformance(serverProcessName);
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
                }
                catch (Exception ex)
                {
                    AddConsole(ex.Message);
                }
            }
        }




        private void AddConsole(string text)
        {
            if (text == null)
                return;

            Paragraph paragraph = new();
            Run run = new();

            // 清除 ANSI 转义序列
            run.Text = Regex.Replace(text, @"(\[\d{1,2}m)|(\[\d{1}[a-zA-Z])|(\[\d{1};\d{1}[a-zA-Z])", "");

            // 检查文本是否包含特定的错误信息，并设置相应的颜色
            if (text.Contains("[ERROR]"))
            {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (text.Contains("webserver已于"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("websocket启动于"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("服务端正在运行, 玩的开心!!"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("Profile:"))
            {
                run.Foreground = new SolidColorBrush(Colors.Orange);
            }
            if (text.Contains("[WS] 玩家："))
            {
                run.Foreground = new SolidColorBrush(Colors.Orange);
            }
            if (text.Contains("Error"))
            {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (text.Contains("服务端已启动!"))
            {
                run.Foreground = new SolidColorBrush(Colors.Green);
            }
            if (text.Contains("服务端已停止!"))
            {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (text.Contains("服务器意外停止！检查控制台是否有错误."))
            {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }
            if (text.Contains("点击启动等待初始化完成自动启动游戏，过程全程自动化，除了注册账号您无需做任何操作。"))
            {
                run.Foreground = new SolidColorBrush(Colors.SeaGreen);
            }
            



            paragraph.Inlines.Add(run);
            ConsoleLog.Blocks.Add(paragraph);
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
            //UpdateConnectButtonState();
        }

        private void ServerLoginIPBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerConfig();
            //UpdateConnectButtonState();
        }
        private void ServerLoginPortBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerConfig();
            //UpdateConnectButtonState();
        }

        private void ServerPortBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveServerConfig();
            //UpdateConnectButtonState();
        }


        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //UpdateConnectButtonState();

        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            //UpdateConnectButtonState();
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
                string akiServerExePath = Path.Combine(folder.Path, "Aki.Server.exe");
                string sptServerExePath = Path.Combine(folder.Path, "SPT.Server.exe");
                string serverExePath = Path.Combine(folder.Path, "Server.exe");

                bool akiServerExists = File.Exists(akiServerExePath);
                bool sptServerExists = File.Exists(sptServerExePath);
                bool serverExists = File.Exists(serverExePath);

                int serverCount = (akiServerExists ? 1 : 0) + (sptServerExists ? 1 : 0) + (serverExists ? 1 : 0);

                if (serverCount > 1)
                {
                    ToastNotificationHelper.ShowNotification("错误", $"所选文件夹包含多个服务端版本，请检查路径是否正确", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "错误");
                }
                else if (serverCount == 1)
                {
                    App.ManagerConfig.AkiServerPath = folder.Path;
                    ServerPathBox.Text = folder.Path;
                    ManagerConfig.Save();
                    ToastNotificationHelper.ShowNotification("设置", $"SPT安装目录已更改至：\n {folder.Path} \n ", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "设置");
                    //UpdateConnectButtonState();
                }
                else
                {
                    ToastNotificationHelper.ShowNotification("错误", $"所选文件夹无效。确保路径包含可用的服务端可执行文件：\n {folder.Path} \n ", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "错误");
                }
            }
            else
            {
                ToastNotificationHelper.ShowNotification("错误", $"未选择目录 ", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
            }
        }

        private string _serverProcessName;

        public void MonitorServerPerformance(string serverProcessName)
        {
            _serverProcessName = serverProcessName; // 保存服务端进程名称
        }

        public double GetCpuUsage()
        {
            // 获取服务端进程
            var processes = Process.GetProcessesByName(_serverProcessName);

            if (processes.Length > 0)
            {
                // 如果进程正在运行，获取该进程的 CPU 使用情况
                var cpuCounter = new PerformanceCounter("Process", "% Processor Time", _serverProcessName);
                cpuCounter.NextValue(); // 必须调用一次才能获取正确值
                System.Threading.Thread.Sleep(1000); // 等待一秒钟，之后再次获取
                return cpuCounter.NextValue();
            }
            else
            {
                // 如果服务端进程未运行，返回系统的 CPU 使用情况
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
                return cpuCounter.NextValue();
            }
        }

        public double GetMemoryUsage()
        {
            // 获取服务端进程
            var processes = Process.GetProcessesByName(_serverProcessName);

            if (processes.Length > 0)
            {
                // 如果进程正在运行，获取该进程的内存使用情况
                return processes[0].WorkingSet64 / (1024 * 1024); // 转换为 MB
            }
            else
            {
                // 如果服务端进程未运行，返回系统的内存使用情况
                var memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                return memoryCounter.NextValue();
            }
        }

        public double GetDiskIO()
        {
            // 获取服务端进程
            var processes = Process.GetProcessesByName(_serverProcessName);

            if (processes.Length > 0)
            {
                // 获取特定进程的磁盘 I/O 使用情况
                var diskCounter = new PerformanceCounter("Process", "IO Data Bytes/sec", _serverProcessName);
                diskCounter.NextValue();
                Thread.Sleep(1000); // 等待1秒钟
                return diskCounter.NextValue() / 1024; // 转换为 KB/s
            }
            else
            {
                // 如果服务端进程未运行，返回系统总体磁盘 I/O 使用情况
                var diskCounter = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total");
                return diskCounter.NextValue() / 1024;
            }
        }

        public async Task UpdateSystemStatsAsync()
        {
            while (true)
            {
                double cpuUsage, memoryUsage, memoryUsageX, diskIO, diskActivePercentage = (0), totalPerformance;
                string memoryUsageText, diskIOTotalText = ""; // 给 diskIOTotalText 设置一个默认值

                // 获取当前服务端进程列表
                var processes = Process.GetProcessesByName(_serverProcessName);

                if (processes.Length > 0)
                {
                    // 服务端进程正在运行，获取其具体数据
                    cpuUsage = GetCpuUsage();
                    memoryUsageX = GetMemoryUsage(); // 单位为 MB
                    var memoryUsageCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                    memoryUsage = memoryUsageCounter.NextValue(); // 单位为百分比
                    memoryUsageText = $"{memoryUsageX:F1} MB"; // 格式化为带单位的文本
                    // 获取磁盘吞吐量（KB/s）
                    var diskCounter = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total");
                    diskCounter.NextValue(); // 第一次调用获取初始值
                    Thread.Sleep(1000); // 等待1秒钟
                    diskIO = diskCounter.NextValue() / 1024; // 转换为 KB/s

                    // 获取磁盘的活动百分比（请求处理的百分比）
                    var diskActiveTimeCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                    diskActiveTimeCounter.NextValue(); // 第一次调用获取初始值
                    Thread.Sleep(1000); // 等待1秒钟
                    diskActivePercentage = diskActiveTimeCounter.NextValue(); // 获取第二次调用的实际值

                    diskIOTotalText = $"{diskIO:F1} KB/s, Disk Active: {diskActivePercentage:F1}%";
                }
                else
                {
                    // 服务端进程未运行，获取总体系统数据
                    // 获取CPU占用率
                    var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuCounter.NextValue(); // 第一次调用获取初始值
                    Thread.Sleep(1000); // 等待1秒钟，让计数器刷新
                    cpuUsage = cpuCounter.NextValue(); // 获取第二次调用的实际值
                    Debug.WriteLine(cpuUsage);

                    // 获取磁盘吞吐量（KB/s）
                    var diskCounter = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total");
                    diskCounter.NextValue(); // 第一次调用获取初始值
                    Thread.Sleep(1000); // 等待1秒钟
                    diskIO = diskCounter.NextValue() / 1024; // 转换为 KB/s

                    // 获取磁盘的活动百分比（请求处理的百分比）
                    var diskActiveTimeCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                    diskActiveTimeCounter.NextValue(); // 第一次调用获取初始值
                    Thread.Sleep(1000); // 等待1秒钟
                    diskActivePercentage = diskActiveTimeCounter.NextValue(); // 获取第二次调用的实际值

                    diskIOTotalText = $"{diskIO:F1} KB/s, Disk Active: {diskActivePercentage:F1}%";

                    // 获取内存使用率
                    var memoryUsageCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                    memoryUsage = memoryUsageCounter.NextValue(); // 单位为百分比
                    memoryUsageText = $"{memoryUsage:F1}%"; // 格式化为百分比文本
                    Debug.WriteLine($"内存占用百分比: {memoryUsage}");

                    // 获取已提交的内存字节数
                    var committedMemoryCounter = new PerformanceCounter("Memory", "Committed Bytes");
                    long committedBytes = (long)committedMemoryCounter.NextValue();  // 获取已提交内存字节数

                    // 将字节数转换为 MB 或 GB
                    double committedMB = committedBytes / (1024.0 * 1024);
                    double committedGB = committedMB / 1024.0;

                    Debug.WriteLine($"已提交内存: {committedMB:F1} MB");

                    // 结合百分比和已提交内存一起显示
                    memoryUsageText = $"{memoryUsage:F1}% （{committedMB:F1} MB ）";
                }

                // 总体性能占用始终计算所有资源的平均值（基于整体系统）
                var overallCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                var overallMemoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                var overallDiskCounter = new PerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total");

                double overallCpuUsage = overallCpuCounter.NextValue();
                double overallMemoryUsage = overallMemoryCounter.NextValue();
                double overallDiskIO = overallDiskCounter.NextValue() / 1024;

                totalPerformance = (overallCpuUsage + overallMemoryUsage + overallDiskIO) / 3;

                // 确保在 UI 线程更新控件
                if (this.DispatcherQueue != null)
                {
                    try
                    {
                        // 更新UI界面
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            // 更新CPU使用率显示
                            CpuUsageProgressBar.Value = cpuUsage;
                            CpuUsageTextBlock.Text = $"CPU 使用率: {cpuUsage:F1}%";

                            // 更新内存使用量显示
                            MemoryUsageProgressBar.Value = memoryUsage;
                            MemoryUsageTextBlock.Text = $"内存使用: {memoryUsageText} ";

                            // 更新磁盘活动百分比显示
                            DiskIOProgressBar.Value = diskActivePercentage; // 设置磁盘活动百分比
                            DiskIOTextBlock.Text = $"磁盘活动: {diskActivePercentage:F1}%";

                            // 更新总体性能显示
                            TotalPerformanceProgressBar.Value = totalPerformance;
                            TotalPerformanceTextBlock.Text = $"总体性能占用: {totalPerformance:F1}%";

                            // 内存使用颜色指示
                            if (processes.Length > 0 && memoryUsage >= 5120) // 服务端进程 > 5GB
                            {
                                MemoryUsageProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                            }
                            else if (processes.Length > 0 && memoryUsage >= 3072) // 服务端进程 > 3GB
                            {
                                MemoryUsageProgressBar.Foreground = new SolidColorBrush(Colors.Orange);
                            }
                            else if (processes.Length == 0 && overallMemoryUsage >= 90) // 总体内存 > 90%
                            {
                                MemoryUsageProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                            }
                            else if (processes.Length == 0 && overallMemoryUsage >= 70) // 总体内存 > 70%
                            {
                                MemoryUsageProgressBar.Foreground = new SolidColorBrush(Colors.Orange);
                            }
                            else
                            {
                                MemoryUsageProgressBar.Foreground = new SolidColorBrush(Colors.Green);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating UI: {ex.Message}");
                    }
                }

                await Task.Delay(1000); // 每秒更新一次
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
                string eftExePath = Path.Combine(folder.Path, "EscapeFromTarkov.exe");

                if (File.Exists(eftExePath))
                {
                    App.ManagerConfig.InstallPath = folder.Path;
                    ClientPathBox.Text = folder.Path;
                    //UpdateConnectButtonState();

                    Utils.CheckEFTVersion(folder.Path);
                    Utils.CheckSITVersion(folder.Path);

                    ManagerConfig.Save();
                    ToastNotificationHelper.ShowNotification("设置", $"EFT客户端路径已更改至：\n {folder.Path} \n ", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "提示");
                }
                else
                {
                    ToastNotificationHelper.ShowNotification("错误", $"所选文件夹无效。确保路径包含游戏可执行文件：\n {folder.Path} \n ", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "错误");
                }
            }
            else
            {
                ToastNotificationHelper.ShowNotification("错误", $"未选择目录", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
            }
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


    }
}
