using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Controls;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Text;
using WinUIEx;
using FilePickers = global::Windows.Storage.Pickers;

namespace Neko.EFT.Manager.X.Windows
{
    public sealed partial class ConfigGuideWindow : Window
    {
        private int _currentStep = 0;
        private const string FileSelectorExecutable = "FileSelector.exe";
        private const string ClientArgument = "client";
        private const string ServerArgument = "server";

        public ConfigGuideWindow()
        {
            AppWindow.Resize(new(750,520));
            WindowManager manager = WindowManager.Get(this);
            manager.MinHeight = 520;
            manager.MaxHeight = 620;
            manager.MinWidth = 750;
            manager.MaxWidth = 750;
            manager.IsResizable = false;
            manager.IsMaximizable = false;
            this.CenterOnScreen();
            this.Closed += ConfigGuideWindow_Closed;
            this.InitializeComponent();

            AddGameButton.Visibility = Visibility.Visible;
            DownloadGameButton.Visibility = Visibility.Visible;
            LocalInstallButton.Visibility = Visibility.Visible;
        }

        private void OnNextButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (_currentStep)
            {
                case 0: // 客户端路径设置
                    if (!string.IsNullOrWhiteSpace(InstallPathTextBox.Text))
                    {
                        _currentStep++;
                        StepInfoBar.Title = "步骤 2: 设置服务端路径";
                        StepInfoBar.Message = "请设置服务端路径，或选择跳过此步骤。";
                        StepTitleTextBlock.Text = "步骤1配置已完成，你可以点击跳过或继续配置服务端路径。";

                        InstallPathExpander.IsExpanded = false;
                        ServerPathExpander.IsExpanded = true;

                        // 显示跳过按钮
                        SkipButton.Visibility = Visibility.Visible;

                        // 更新进度条
                        StepProgressBar.Value = 50; // 步骤完成一半
                    }
                    else
                    {
                        ToastNotificationHelper.ShowNotification("错误", "客户端路径不能为空！", "确认", null, "通知", "错误");
                    }
                    break;

                case 1: // 服务端路径设置
                    if (!string.IsNullOrWhiteSpace(AkiServerPathTextBox.Text))
                    {
                        CompleteConfiguration(); // 直接完成配置
                    }
                    else
                    {
                        ToastNotificationHelper.ShowNotification("错误", "服务端路径不能为空！", "确认", null, "通知", "错误");
                    }
                    break;
            }
        }

        private void OnSkipButtonClicked(object sender, RoutedEventArgs e)
        {
            CompleteConfiguration(); // 直接完成配置
        }

        private async void OnAddGameButtonClicked(object sender, RoutedEventArgs e)
        {
            // 创建进度对话框
            var progressDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "正在搜索游戏文件夹...",
                Content = CreateProgressDialogContent(),
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            // 获取进度控件引用
            var progressBar = (progressDialog.Content as StackPanel)?.Children
                .OfType<ProgressBar>().FirstOrDefault();
            var statusControls = progressBar?.Tag as ProgressStatusControls;

            // 状态控制
            var cancellationTokenSource = new CancellationTokenSource();
            var isCompleted = false;

            // 关闭事件处理
            TypedEventHandler<ContentDialog, ContentDialogClosingEventArgs> closingHandler = (s, args) =>
            {
                if (!isCompleted) cancellationTokenSource.Cancel();
            };
            progressDialog.Closing += closingHandler;

            List<string> results = null;
            try
            {
                // 显示进度对话框并开始搜索
                var showTask = progressDialog.ShowAsync();
                results = await SearchForGameDirectoriesWithProgressAsync(
                    "EscapeFromTarkov.exe",
                    progressBar,
                    statusControls,
                    () => cancellationTokenSource.IsCancellationRequested);

                isCompleted = true;
                progressDialog.Hide();
            }
            catch (OperationCanceledException)
            {
                // 用户取消搜索时不执行后续操作
                return;
            }
            finally
            {
                cancellationTokenSource.Dispose();
                progressDialog.Closing -= closingHandler;
            }

            // 创建结果对话框
            var resultDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = results?.Count > 0 ? "🎉 搜索完成" : "⚠️ 搜索未完成",
                Content = results?.Count > 0
                    ? CreateResultContent(results)
                    : CreateNoResultContent(),
                PrimaryButtonText = "使用路径",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 处理路径设置
            var dialogResult = await resultDialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary && results?.Count > 0)
            {
                var selectedPath = results.First();

                // 设置客户端路径
                App.ManagerConfig.InstallPath = selectedPath;
                InstallPathTextBox.Text = selectedPath;
                Utils.CheckEFTVersion(selectedPath);
                Utils.CheckSITVersion(selectedPath);

                // 设置服务端路径（假设服务端与客户端同目录）
                App.ManagerConfig.AkiServerPath = selectedPath;
                AkiServerPathTextBox.Text = selectedPath;

                ManagerConfig.SaveAccountInfo();

                ToastNotificationHelper.ShowNotification("路径设置",
                    $"已同时设置客户端和服务端路径至：\n{selectedPath}",
                    "确认",
                    null,
                    "成功",
                    "成功");

                CompleteConfiguration();
            }
        }

        private static UIElement CreateResultContent(List<string> results)
        {
            return new ScrollViewer
            {
                Height = 200,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = {
                new TextBlock {
                    Text = "找到以下有效路径：",
                    FontWeight = FontWeights.SemiBold
                },
                new ItemsControl {
                    ItemsSource = results.Select(path => new TextBlock {
                        Text = path,
                        FontFamily = new FontFamily("Consolas"),
                        TextWrapping = TextWrapping.Wrap
                    })
                },
                new TextBlock {
                    Text = "点击「使用路径」设置客户端和服务端路径",
                    FontStyle = FontStyle.Italic,
                    Opacity = 0.8
                }
            }
                }
            };
        }

        private static UIElement CreateNoResultContent()
        {
            return new StackPanel
            {
                Spacing = 8,
                Children = {
            new SymbolIcon(Symbol.Important) {
                Foreground = new SolidColorBrush(Colors.Orange)
            },
            new TextBlock {
                Text = "未找到游戏目录，请确认：",
                FontSize = 16,
                Margin = new Thickness(0, 10, 0, 0)
            },
            new TextBlock {
                Text = "1. 游戏已正确安装\n2. 已授予程序磁盘访问权限\n3. 防病毒软件未阻止扫描",
                TextWrapping = TextWrapping.Wrap
            }
        }
            };
        }


        private static StackPanel CreateProgressDialogContent()
        {
            var stackPanel = new StackPanel { Spacing = 4 };

            // 进度条
            var progressBar = new ProgressBar
            {
                Width = 320,
                Height = 20,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            stackPanel.Children.Add(progressBar);

            // 状态信息容器
            var statusContainer = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 0),
                Spacing = 2
            };

            // 第一行：磁盘统计
            var diskStatsText = new TextBlock
            {
                FontSize = 12,
                Opacity = 0.8,
                Text = "正在扫描 0/0 个磁盘..."
            };
            statusContainer.Children.Add(diskStatsText);

            // 第二行：当前磁盘进度
            var driveProgressText = new TextBlock
            {
                FontSize = 13,
                Text = "当前磁盘：未开始"
            };
            statusContainer.Children.Add(driveProgressText);

            // 第三行：当前搜索路径
            var currentPathText = new TextBlock
            {
                FontSize = 12,
                Opacity = 0.7,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 300,
                Text = "等待开始搜索..."
            };
            statusContainer.Children.Add(currentPathText);

            stackPanel.Children.Add(statusContainer);

            // 绑定所有状态控件
            progressBar.Tag = new ProgressStatusControls(
                diskStatsText,
                driveProgressText,
                currentPathText
            );

            return stackPanel;
        }

        // 辅助类用于保存状态控件引用
        private sealed record ProgressStatusControls(
            TextBlock DiskStatsText,
            TextBlock DriveProgressText,
            TextBlock CurrentPathText
        );






        private static async Task<List<string>> SearchForGameDirectoriesWithProgressAsync(
    string targetFileName,
    ProgressBar progressBar,
    ProgressStatusControls statusControls, // 改为新的状态控件类型
    Func<bool> isCancelled)
        {
            return await Task.Run(async () =>
            {
                var foundDirectories = new List<string>();
                var lastUpdateTime = DateTime.MinValue;
                int directoriesScanned = 0;

                try
                {
                    var drives = DriveInfo.GetDrives()
                        .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                        .ToList();

                    int totalDrives = drives.Count;
                    int completedDrives = 0;
                    const int DRIVE_SCAN_WEIGHT = 80; // 80%进度分配给磁盘扫描

                    foreach (var drive in drives)
                    {
                        if (isCancelled()) break;

                        completedDrives++;
                        var driveName = drive.Name.TrimEnd('\\');

                        // 更新全局进度
                        UpdateProgress(
                            progressBar,
                            statusControls,
                            (int)((double)(completedDrives - 1) / totalDrives * DRIVE_SCAN_WEIGHT),
                            $"磁盘扫描 ({completedDrives}/{totalDrives})",    // 第一行
                            $"当前扫描：{driveName}",                         // 第二行
                            "正在初始化搜索..."                              // 第三行
                        );

                        // 搜索当前磁盘
                        SearchDirectory(drive.RootDirectory.FullName,
                            targetFileName,
                            foundDirectories,
                            progressBar,
                            statusControls,
                            (currentPath) =>
                            {
                                // 限流：每秒最多更新10次路径
                                if ((DateTime.Now - lastUpdateTime).TotalMilliseconds < 100)
                                    return;

                                directoriesScanned++;
                                UpdateProgress(
                                    progressBar,
                                    statusControls,
                                    (int)((double)completedDrives / totalDrives * DRIVE_SCAN_WEIGHT),
                                    $"磁盘扫描 ({completedDrives}/{totalDrives})",
                                    $"{driveName} (已扫描 {directoriesScanned} 个目录)",
                                    currentPath.Length > 50
                                        ? $"...{currentPath.Substring(currentPath.Length - 47)}"
                                        : currentPath
                                );
                                lastUpdateTime = DateTime.Now;
                            },
                            isCancelled);
                    }

                    // 最终完成动画
                    for (int i = DRIVE_SCAN_WEIGHT; i <= 100; i++)
                    {
                        if (isCancelled()) break;
                        UpdateProgress(
                            progressBar,
                            statusControls,
                            i,
                            "扫描完成",
                            $"共扫描 {directoriesScanned} 个目录",
                            "正在生成结果..."
                        );
                        await Task.Delay(30);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"搜索时发生错误: {ex.Message}");
                    UpdateProgress(
                        progressBar,
                        statusControls,
                        (int)progressBar.Value,
                        "发生错误",
                        "扫描中断",
                        ex.Message
                    );
                }

                return foundDirectories;
            });
        }


        private static void SearchDirectory(
    string directory,
    string targetFileName,
    List<string> foundDirectories,
    ProgressBar progressBar,
    ProgressStatusControls statusControls,
    Action<string> onDirectoryScanned,
    Func<bool> isCancelled)
        {
            if (isCancelled()) return;

            try
            {
                onDirectoryScanned?.Invoke(directory);

                // 搜索主程序文件
                foreach (var file in Directory.EnumerateFiles(directory, targetFileName, SearchOption.TopDirectoryOnly))
                {
                    // 验证文件夹结构
                    var directoryInfo = new DirectoryInfo(directory);
                    if (IsValidGameDirectory(directoryInfo))
                    {
                        foundDirectories.Add(directory);
                        break; // 找到有效目录后停止当前目录的搜索
                    }
                }

                // 递归搜索子目录
                foreach (var subDir in Directory.EnumerateDirectories(directory)
                    .Where(d => !ShouldExcludeDirectory(d)))
                {
                    SearchDirectory(subDir, targetFileName, foundDirectories,
                        progressBar, statusControls, onDirectoryScanned, isCancelled);
                }
            }
            catch
            {
                // 忽略无权限目录
            }
        }

        private static bool IsValidGameDirectory(DirectoryInfo directory)
        {
            try
            {
                // 必须包含 EscapeFromTarkov_Data 文件夹
                var dataDir = Path.Combine(directory.FullName, "EscapeFromTarkov_Data");
                if (!Directory.Exists(dataDir))
                    return false;

                // 检查服务器执行文件
                var serverFiles = Directory.GetFiles(directory.FullName, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f =>
                    {
                        var fileName = Path.GetFileName(f);
                        return fileName.Equals("SPT.Server.exe", StringComparison.OrdinalIgnoreCase) ||
                               fileName.Equals("Aki.Server.exe", StringComparison.OrdinalIgnoreCase) ||
                               fileName.Equals("Server.exe", StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                // 必须存在且唯一
                return serverFiles.Count == 1;
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldExcludeDirectory(string path)
        {
            var excludedSegments = new[]
            {
        "ProgramData",
        "Users",
        "AppData",
        "Windows",
        "Microsoft Shared",
        "$RECYCLE.BIN"
    };

            return excludedSegments.Any(segment =>
                path.IndexOf(segment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void UpdateProgress(
    ProgressBar progressBar,
    ProgressStatusControls statusControls,
    int progressValue,
    string diskStatus,
    string driveStatus,
    string pathStatus)
        {
            progressBar.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    progressBar.Value = progressValue;
                    statusControls.DiskStatsText.Text = diskStatus;
                    statusControls.DriveProgressText.Text = driveStatus;
                    statusControls.CurrentPathText.Text = pathStatus;
                }
                catch
                {
                    // 防止对象已释放时出错
                }
            });
        }



        private void CompleteConfiguration()
        {
            AddGameButton.Visibility = Visibility.Collapsed;
            _currentStep++; // 更新步骤为完成状态
            StepInfoBar.Title = "完成配置";
            StepInfoBar.Message = "配置已完成，点击保存配置以应用更改。";
            StepTitleTextBlock.Text = "配置已全部完成，点击保存配置以应用更改。";

            InstallPathExpander.Visibility = Visibility.Collapsed;
            ServerPathExpander.Visibility = Visibility.Collapsed;

            NextButton.Content = "保存配置"; // 修改按钮内容
            NextButton.Click -= OnNextButtonClicked;
            NextButton.Click += OnSaveConfigurationClicked;

            // 隐藏跳过按钮
            AddGameButton.Visibility = Visibility.Collapsed;
            DownloadGameButton.Visibility = Visibility.Collapsed;
            LocalInstallButton.Visibility = Visibility.Collapsed;
            SkipButton.Visibility = Visibility.Collapsed;

            // 更新进度条
            StepProgressBar.Value = 100; // 完成配置
        }



        private void OnPreviousButtonClicked(object sender, RoutedEventArgs e)
        {
            switch (_currentStep)
            {
                case 1: // 返回到步骤 1（客户端路径）
                    _currentStep--;
                    StepInfoBar.Title = "步骤 1: 设置客户端路径";
                    StepInfoBar.Message = "请设置客户端路径。";
                    StepTitleTextBlock.Text = "欢迎使用，由于你是首次启动，请根据引导完成以下配置↓";
                    InstallPathExpander.IsExpanded = true;
                    ServerPathExpander.IsExpanded = false;
                    AddGameButton.Visibility = Visibility.Visible;
                    DownloadGameButton.Visibility = Visibility.Visible;
                    LocalInstallButton.Visibility = Visibility.Visible;
                    // 隐藏跳过按钮（如果在第 2 步被显示过）
                    SkipButton.Visibility = Visibility.Collapsed;

                    // 更新进度条
                    StepProgressBar.Value = 0; // 返回到第一个步骤
                    break;

                case 2: // 返回到步骤 2（服务端路径）
                    _currentStep--;
                    StepInfoBar.Title = "步骤 2: 设置服务端路径";
                    StepInfoBar.Message = "请设置服务端路径，或选择跳过此步骤。";
                    StepTitleTextBlock.Text = "步骤1配置已完成，你可以点击跳过或继续配置服务端路径。";
                    InstallPathExpander.Visibility = Visibility.Visible;
                    ServerPathExpander.Visibility = Visibility.Visible;
                    AddGameButton.Visibility = Visibility.Visible;
                    DownloadGameButton.Visibility = Visibility.Visible;
                    LocalInstallButton.Visibility = Visibility.Visible;
                    ServerPathExpander.IsExpanded = true;
                    InstallPathExpander.IsExpanded = false;

                    // 显示跳过按钮
                    SkipButton.Visibility = Visibility.Visible;

                    // 修改下一步按钮内容和事件绑定
                    NextButton.Content = "下一步";
                    NextButton.Click -= OnSaveConfigurationClicked;
                    NextButton.Click -= OnPreviousButtonClicked;
                    NextButton.Click += OnNextButtonClicked;

                    // 更新进度条
                    StepProgressBar.Value = 50; // 返回到第二步骤
                    break;
            }
        }



        private TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();

        public Task<bool> WaitForConfigurationAsync()
        {
            return _completionSource.Task;
        }

        private void ConfigGuideWindow_Closed(object sender, WindowEventArgs args)
        {
            // 用户关闭窗口，设置任务返回 false
            _completionSource.TrySetResult(false);
        }

        

        private void OnSaveConfigurationClicked(object sender, RoutedEventArgs e)
        {
            var managerConfig = new ManagerConfig
            {
                InstallPath = InstallPathTextBox.Text,
                AkiServerPath = AkiServerPathTextBox.Text
            };
            _completionSource.TrySetResult(true); // 配置完成，返回 true
            this.Close();
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

        

        private async void OnChangeInstallPathClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var process = StartFileSelector(ClientArgument);
                await process.WaitForExitAsync();

                ManagerConfig.Load();
                var selectedPath = App.ManagerConfig.InstallPath;

                if (!string.IsNullOrEmpty(selectedPath) && File.Exists(Path.Combine(selectedPath, "EscapeFromTarkov.exe")))
                {
                    InstallPathTextBox.Text = selectedPath; // 更新 TextBox 值
                    Utils.CheckEFTVersion(selectedPath);
                    Utils.CheckSITVersion(selectedPath);
                    ManagerConfig.SaveAccountInfo();
                    ToastNotificationHelper.ShowNotification("设置", $"EFT客户端路径已更改至：\n {selectedPath} \n ", "确认", null, "通知", "提示");
                }
                else
                {
                    ToastNotificationHelper.ShowNotification("错误", "所选文件夹无效。确保路径包含游戏可执行文件！", "确认", null, "通知", "错误");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationHelper.ShowNotification("错误", $"启动文件选择器时发生错误: {ex.Message}", "确认", null, "通知", "警告");
            }
        }

        private async void OnChangeServerPathClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var process = StartFileSelector(ServerArgument);
                await process.WaitForExitAsync();

                ManagerConfig.Load();
                string akiServerPath = App.ManagerConfig.AkiServerPath;

                string akiServerExePath = Path.Combine(akiServerPath, "Aki.Server.exe");
                string sptServerExePath = Path.Combine(akiServerPath, "SPT.Server.exe");
                string serverExePath = Path.Combine(akiServerPath, "Server.exe");

                if (File.Exists(akiServerExePath) || File.Exists(sptServerExePath) || File.Exists(serverExePath))
                {
                    AkiServerPathTextBox.Text = akiServerPath; // 更新 TextBox 值
                    ManagerConfig.SaveAccountInfo();
                    ToastNotificationHelper.ShowNotification("设置", $"SPT安装目录已更改至：\n {akiServerPath} \n ", "确认", null, "通知", "提示");
                }
                else
                {
                    ToastNotificationHelper.ShowNotification("错误", $"所选文件夹无效。确保路径包含服务端可执行文件！", "确认", null, "通知", "错误");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationHelper.ShowNotification("错误", $"选择目录时发生错误: {ex.Message}", "确认", null, "通知", "警告");
            }
        }

        private async void OnDownloadGameButtonClicked(object sender, RoutedEventArgs e)
        {
            //var uri = new Uri("https://sns.oddba.cn/bbs/spt-r");
            //await Launcher.LaunchUriAsync(uri);

            GameResDLWindow GameResDLWindows = new GameResDLWindow();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            GameResDLWindows.Activate();
        }

        private async void OnLocalResourceInstallButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.Content is FrameworkElement element)
            {
                var dialog = new LocalResourceInstallDialog(element.XamlRoot);
                // ✅ 订阅事件
                dialog.InstallCompleted += (s, e) => CompleteConfiguration();
                await dialog.ShowAsync();
            }
        }





    }
}
