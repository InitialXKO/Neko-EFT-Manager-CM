using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Threading;
using System.IO.Compression;
using Microsoft.UI.Dispatching;
using Neko.EFT.Manager.X.Classes;
using CommunityToolkit.WinUI;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using SharpCompress.Archives;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Navigation;
using SharpCompress.Common;
using System.Text.Json.Serialization;
using System.Text.Json;
using Neko.EFT.Manager.X.Pages.ServerManager;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class ModsPage : Page
    {
        public StorageFile file;
        public ObservableCollection<ModInfo> Mods { get; set; } = new ObservableCollection<ModInfo>();
        private List<ModInfo> EnabledMods = new List<ModInfo>();
        private List<ModInfo> DisabledMods = new List<ModInfo>();
        private string modsBackupDirectoryPath = Path.Combine(App.ManagerConfig.InstallPath, "user", "modbackup-off");
        private string modsDirectoryPath = Path.Combine(App.ManagerConfig.InstallPath, "user", "mods");
        private bool isAcrylic = true;
        private ThemeConfig themeConfig;
        private Dictionary<string, string> modConfigs;

        public ModsPage()
        {

            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            this.Loaded += ModsPage_Loaded;
            
            //this.ModGrid.Background.Opacity = 0.96;
            //this.InstallModButton.Background.Opacity = 0.96;

        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is StorageFile file)
            {
                // 调用安装逻辑
                InstallMod(file, App.m_window as MainWindow, CancellationToken.None);
            }
        }
        private async void SwitchBackground_Click(object sender, RoutedEventArgs e)
        {
            if (isAcrylic)
            {
                // 切换到渐变背景
                MainGrid.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                SwitchBackgroundButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                InstallModButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                themeConfig.Theme = "Gradient";
            }
            else
            {
                // 切换到亚克力背景
                MainGrid.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                InstallModButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                SwitchBackgroundButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                themeConfig.Theme = "Acrylic";
            }
            isAcrylic = !isAcrylic;
            themeConfig.Save(); // Save the updated theme config
        }

        private FileSystemWatcher modWatcher;

        public async void ModsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load theme configuration
                themeConfig = ThemeConfig.Load();

                if (themeConfig.Theme == "Gradient")
                {
                    MainGrid.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                    SwitchBackgroundButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                    InstallModButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];

                    isAcrylic = false;
                }
                else
                {
                    MainGrid.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    InstallModButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    SwitchBackgroundButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    isAcrylic = true;
                }

                if (App.ManagerConfig.InstallPath != null)
                {
                    LoadingRing.Visibility = Visibility.Visible; // 显示加载动画
                    await LoadMods();
                    LoadingRing.Visibility = Visibility.Collapsed; // 隐藏加载动画
                    ModGrid.Visibility = Visibility.Visible; // 显示模组列表

                    modConfigs = ModConfig.Load();
                    foreach (var mod in Mods)
                    {
                        if (modConfigs.TryGetValue(mod.FilePath, out string customName))
                        {
                            mod.CustomName = customName;
                        }
                    }
                }
                else
                {
                    await ShowErrorDialog("配置错误", "未配置 \"安装路径\"。转到设置配置安装路径。");
                }

                ModsList.Focus(FocusState.Programmatic); // 设置焦点

            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"Error in ModsPage_Loaded: {ex.Message}");
            }
        }

        public async void InstallMod_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            filePicker.FileTypeFilter.Add(".zip");
            filePicker.FileTypeFilter.Add(".rar");
            filePicker.FileTypeFilter.Add(".7z");

            MainWindow window = App.m_window as MainWindow;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            try
            {
                var zipFile = await filePicker.PickSingleFileAsync();
                if (zipFile != null)
                {
                    
                    await InstallMod(zipFile, window, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowNotification("错误", $"选择文件时发生错误：{ex.Message}", InfoBarSeverity.Error);
                ToastNotificationHelper.ShowNotification("错误", $"选择文件时发生错误：{ex.Message}", "确定", null, string.Empty, string.Empty);
            }
        }

        public async Task InstallMod(StorageFile zipFile, MainWindow window, CancellationToken cancellationToken)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // 显示安装开始通知
                ShowNotification("模组安装", $"模组 {zipFile.Name} 安装中...", "确定", "通知");
                NotificationService.Instance.ShowNotification("正在安装", $"正在安装模组文件: {zipFile.Name} ...\n 如文件较大请耐心等待。", InfoBarSeverity.Warning);

                // 解压文件
                await ExtractArchive(zipFile.Path, tempDirectory, cancellationToken);

                // 获取压缩包内的 DLL 文件信息
                var dllInfo = await GetModInfoFromDll(tempDirectory);

                // 显示模组信息
                await ShowModInfoAndConfirm(zipFile.Name, dllInfo, tempDirectory, cancellationToken);

                // 安装模组
                await _InstallMod(tempDirectory, cancellationToken);

                // 安装完成通知
                NotificationService.Instance.ShowNotification("安装完成", $"模组： {zipFile.Name}  已完成安装\n 请刷新该页面", InfoBarSeverity.Error);
                ShowNotification("完成", "模组安装完成。", "确定", "通知");
            }
            catch (OperationCanceledException)
            {
                // 用户取消安装
                NotificationService.Instance.ShowNotification("安装取消", $"模组： {zipFile.Name}  安装已被用户取消。", InfoBarSeverity.Error);
                ShowNotification("取消", "模组安装已取消", "确认", "通知"); 
            }

            catch (InvalidOperationException ex)
            {
                // 捕获其他异常
                ShowNotification("错误", $"{ex.Message}\n 正在尝试服务端Mod安装...", "确认", "通知");
                await Task.Delay(3000); // 等待显示通知

                try
            {
                    file = zipFile;

                NotificationService.Instance.ShowNotification("正在安装", $"正在安装的模组: {file.Name}  是服务端Mod...\n 如文件较大请耐心等待。", InfoBarSeverity.Warning);
                

                using (var StempDirectory = new TempDirectory())
                {
                    await SExtractModFiles(file, StempDirectory.Path);

                    var modInfo = await SParsePackageJson(StempDirectory.Path);

                    if (modInfo == null || !await SShowModDetailsConfirmationAsync(modInfo))
                    {
                        NotificationService.Instance.ShowNotification("安装取消", "已取消安装或模组类型不匹配，如你已取消安装请忽略", InfoBarSeverity.Error);

                        ToastNotificationHelper.ShowNotification("取消", "模组类型不匹配或mod文件错误，请尝试重新安装此文件，如你已取消安装请忽略", "确认",
               null, "通知", "错误");
                        // 游戏退出后关闭现有通知并显示新的通知
                        // 获取父页面的实例


                        return;
                    }

                    await SCompleteModInstallation(file, StempDirectory.Path);
                }
            }
            catch (Exception exZ)
            {
                SShowErrorNotification($"安装过程中发生错误: {exZ.Message}");
            }


            }
            catch (Exception ex)
            {
                // 捕获其他异常
                ShowNotification("错误", $"安装过程中发生错误：{ex.Message}", "确认", "通知");
                throw; // 重新抛出异常以便上层处理
            }
            
            finally
            {
                // 删除临时文件
                if (Directory.Exists(tempDirectory))
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        ShowNotification("错误", $"删除临时文件时发生错误：{ex.Message}", "确认", "通知");
                    }
                }
            }
        }

        public async Task ShowModInfoAndConfirm(string fileName, List<DllInfo> dllInfo, string tempDirectory, CancellationToken cancellationToken)
        {
            // 创建一个 ListView 来展示 DLL 信息
            var dllListView = new ListView
            {
                ItemTemplate = (DataTemplate)Resources["DllInfoTemplate"], // 引用 XAML 中定义的 DataTemplate
                ItemsSource = dllInfo, // 绑定 DLL 信息
                Margin = new Thickness(10)
            };

            // 定义对话框内容
            var contentStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Children =
        {
            new TextBlock
            {
                Text = $"模组文件：{fileName}",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            },
            new TextBlock
            {
                Text = "检测到以下 DLL 文件：",
                FontSize = 16,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 10)
            },
            dllListView
        }
            };

            // 创建 ContentDialog
            ContentDialog modInfoDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot, // 确保对话框在正确的内容区域显示
                Title = "客户端模组信息",
                Content = contentStackPanel,
                PrimaryButtonText = "继续安装",
                CloseButtonText = "取消安装",
                DefaultButton = ContentDialogButton.Primary,
                Background = new SolidColorBrush(Colors.White) // 设置对话框背景
            };

            var result = await modInfoDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                // 用户取消安装
                throw new OperationCanceledException("用户取消了模组安装");
            }
        }

        public async Task<List<DllInfo>> GetModInfoFromDll(string directoryPath)
        {
            List<DllInfo> dllInfoList = new List<DllInfo>();
            try
            {
                string[] dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories);

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        // 获取 DLL 文件信息
                        var versionInfo = FileVersionInfo.GetVersionInfo(dllFile);
                        dllInfoList.Add(new DllInfo
                        {
                            Name = Path.GetFileNameWithoutExtension(dllFile),
                            Version = versionInfo.FileVersion,
                            Description = versionInfo.FileDescription
                        });
                    }
                    catch (Exception ex)
                    {
                        // 如果获取信息失败，跳过该 DLL
                        ShowNotification("警告", $"获取 {dllFile} 的信息时发生错误：{ex.Message}", "确认", "通知");
                    }
                }

                // 如果未找到有效的 DLL 文件
                if (!dllInfoList.Any())
                {
                    throw new InvalidOperationException("压缩文件中未找到有效的 DLL 文件");
                }
            }
            catch (Exception ex)
            {
                ShowNotification("错误", $"获取 DLL 文件信息失败：{ex.Message}", "确认", "通知");
                throw; // 重新抛出异常以便上层处理
            }

            return dllInfoList;
        }

        public class DllInfo
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
        }

        public async Task _InstallMod(string tempDirectory, CancellationToken cancellationToken)
        {
            try
            {
                string modDirectory = Path.Combine(App.ManagerConfig.InstallPath, "BepInEx", "plugins");

                if (IsBepInExMod(tempDirectory))
                {
                    await InstallBepInExMod(tempDirectory, modDirectory, cancellationToken);
                }
                else if (ContainsDllFiles(tempDirectory))
                {
                    await InstallGeneralMod(tempDirectory, modDirectory, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("压缩文件内不包含有效的 DLL 文件。请确保是客户端模组文件");
                }
            }
            catch (Exception ex)
            {
                ShowNotification("错误", $"安装过程中出现错误：{ex.Message}", "确认", "通知");
                throw; // 重新抛出异常以便上层处理
            }
        }

        public async Task InstallBepInExMod(string tempDirectory, string modDirectory, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(modDirectory);
            string bepInExPluginDirectory = Path.Combine(tempDirectory, "BepInEx", "plugins");

            foreach (string dirPath in Directory.GetDirectories(bepInExPluginDirectory, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string targetDirPath = dirPath.Replace(bepInExPluginDirectory, modDirectory);
                Directory.CreateDirectory(targetDirPath);
            }

            foreach (string newPath in Directory.GetFiles(bepInExPluginDirectory, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string destinationPath = newPath.Replace(bepInExPluginDirectory, modDirectory);
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath); // 删除已存在的文件
                }
                File.Copy(newPath, destinationPath, true);
            }

            ToastNotificationHelper.ShowNotification("完成", "模组安装完成。", "确定", null, string.Empty, string.Empty);
        }

        public async Task InstallGeneralMod(string tempDirectory, string modDirectory, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(modDirectory);

            foreach (string newPath in Directory.GetFiles(tempDirectory, "*.dll", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string targetDir = Path.GetDirectoryName(newPath);
                string targetModDir = Path.Combine(modDirectory, Path.GetFileName(targetDir));
                Directory.CreateDirectory(targetModDir);

                foreach (string file in Directory.GetFiles(targetDir))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string destinationPath = Path.Combine(targetModDir, Path.GetFileName(file));
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath); // 删除已存在的文件
                    }
                    File.Copy(file, destinationPath, true);
                }
            }

            ToastNotificationHelper.ShowNotification("完成", "模组安装完成。", "确定", null, string.Empty, string.Empty);
        }




        public async Task ExtractArchive(string archivePath, string extractPath, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var extension = Path.GetExtension(archivePath).ToLower();

                // 处理 7z 和 rar 格式
                if (extension == ".7z" || extension == ".rar")
                {
                    using (var archive = new SevenZipExtractor.ArchiveFile(archivePath))
                    {
                        archive.Extract(extractPath);
                    }
                }
                // 处理其他格式（zip）
                else
                {
                    using (var archive = ArchiveFactory.Open(archivePath))
                    {
                        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var destinationPath = Path.Combine(extractPath, entry.Key);

                            // 路径安全验证
                            if (!destinationPath.StartsWith(Path.GetFullPath(extractPath), StringComparison.Ordinal))
                            {
                                throw new IOException($"非法解压路径: {entry.Key}");
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                            using (var fileStream = File.Create(destinationPath))
                            {
                                entry.WriteTo(fileStream);
                            }
                        }
                    }
                }
            }, cancellationToken);
        }


        public bool IsBepInExMod(string directoryPath)
        {
            return Directory.Exists(Path.Combine(directoryPath, "BepInEx", "plugins"));
        }

        public bool ContainsDllFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories).Any();
        }

        public void ShowNotification(string title, string content, string actionButton, string tag)
        {
            try
            {
                ToastNotificationHelper.ShowNotification(title, content, actionButton, null, tag, string.Empty);
            }
            catch (Exception ex)
            {
                // 如果通知显示失败，记录日志或简单输出
                Console.WriteLine($"通知显示失败：{ex.Message}");
            }
        }


        public class SModInfo
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("version")]
            public string Version { get; set; }

            [JsonPropertyName("author")]
            public string Author { get; set; }

            [JsonPropertyName("sptVersion")]
            public string RequiredSptVersion { get; set; }
        }

        private async void SInstallMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                file = await SPickModFileAsync();

                NotificationService.Instance.ShowNotification("正在安装", $"模组 {file.Name} 正在安装中\n 如文件较大请耐心等待", InfoBarSeverity.Warning);
                if (file == null) return;

                using (var tempDirectory = new TempDirectory())
                {
                    await SExtractModFiles(file, tempDirectory.Path);

                    var modInfo = await SParsePackageJson(tempDirectory.Path);

                    if (modInfo == null || !await SShowModDetailsConfirmationAsync(modInfo))
                    {
                        NotificationService.Instance.ShowNotification("安装取消", "已取消安装或模组类型不匹配，请尝试前往客户端mod管理器安装此文件，如你已取消安装请忽略", InfoBarSeverity.Error);

                        ToastNotificationHelper.ShowNotification("取消", "模组类型不匹配，请尝试前往客户端mod管理器安装此文件", "确认",
               null, "通知", "错误");
                        // 游戏退出后关闭现有通知并显示新的通知
                        // 获取父页面的实例


                        return;
                    }

                    await SCompleteModInstallation(file, tempDirectory.Path);
                }
            }
            catch (Exception ex)
            {
                SShowErrorNotification($"安装过程中发生错误: {ex.Message}");
            }
        }

        #region File Operations
        private async Task<StorageFile> SPickModFileAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                FileTypeFilter = { ".zip", ".7z", ".rar" }
            };

            WinRT.Interop.InitializeWithWindow.Initialize(picker,
                WinRT.Interop.WindowNative.GetWindowHandle(App.m_window));

            return await picker.PickSingleFileAsync();
        }

        private async Task SExtractModFiles(StorageFile file, string tempDirectory)
        {
            ToastNotificationHelper.ShowNotification("解压中", $"正在解压 {file.Name}...", "确认",
                null, "通知", "错误");
            Directory.CreateDirectory(tempDirectory);
            await Task.Run(() => ExtractArchive(file.Path, tempDirectory, CancellationToken.None));
        }
        #endregion

        #region Mod Information Handling
        private async Task<SModInfo> SParsePackageJson(string tempDirectory)
        {
            var packageJsonPath = Directory.EnumerateFiles(tempDirectory, "package.json",
                SearchOption.AllDirectories).FirstOrDefault();

            if (packageJsonPath == null)
            {
                SShowErrorNotification("无效的模组文件，缺少 package.json\n 请尝试安装为客户端Mod");
                await Task.Delay(4000); // 等待显示通知



                return null;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                using var stream = File.OpenRead(packageJsonPath);
                var modInfo = await JsonSerializer.DeserializeAsync<SModInfo>(stream, options);

                if (string.IsNullOrWhiteSpace(modInfo?.Name))
                {
                    SShowErrorNotification("无效的package.json格式，缺少必要字段");
                    return null;
                }

                return modInfo;
            }
            catch (JsonException ex)
            {
                SShowErrorNotification($"解析package.json失败: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                SShowErrorNotification($"读取模组信息失败: {ex.Message}");
                return null;
            }
        }

        private async Task SExtractArchive(string archivePath, string extractPath, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                string extension = Path.GetExtension(archivePath).ToLower();

                // 处理 .7z 和 .rar
                if (extension == ".7z" || extension == ".rar")
                {
                    using (var archive = new SevenZipExtractor.ArchiveFile(archivePath))
                    {
                        archive.Extract(extractPath);
                    }
                }
                // 处理 .zip
                else if (extension == ".zip")
                {
                    using (var archive = ArchiveFactory.Open(archivePath))
                    {
                        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                        {
                            var destinationPath = Path.Combine(extractPath, entry.Key);

                            // 防止路径遍历攻击
                            if (!destinationPath.StartsWith(Path.GetFullPath(extractPath), StringComparison.Ordinal))
                            {
                                throw new IOException("解压路径无效");
                            }

                            // 确保目录存在
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                            // 提取文件
                            entry.WriteToDirectory(extractPath, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("不支持的压缩文件格式");
                }
            }, cancellationToken);
        }

        private async Task<bool> SShowModDetailsConfirmationAsync(SModInfo modInfo)
        {
            return await App.MainWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "服务端模组详情 - 安装确认",
                    PrimaryButtonText = "继续安装",
                    SecondaryButtonText = "取消安装",
                    DefaultButton = ContentDialogButton.Primary
                };

                // 创建带间距的Grid
                var grid = new Grid
                {
                    ColumnSpacing = 12,
                    RowSpacing = 8,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                // 明确列定义
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 标签列
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 值列

                // 设置行定义（关键修改）
                for (int i = 0; i < 4; i++)
                {
                    grid.RowDefinitions.Add(new RowDefinition
                    {
                        Height = GridLength.Auto, // 自动高度但有限制
                        MinHeight = 32 // 保证最小行高
                    });
                }

                void AddDetailRow(int row, string label, string value)
                {
                    // 标签列
                    var labelBlock = new TextBlock
                    {
                        Text = label,
                        VerticalAlignment = VerticalAlignment.Center,
                        Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
                        MaxWidth = 120 // 防止标签过长
                    };
                    Grid.SetColumn(labelBlock, 0);
                    Grid.SetRow(labelBlock, row);
                    grid.Children.Add(labelBlock);

                    // 值列（关键修改）
                    var valueBlock = new TextBlock
                    {
                        Text = value ?? "未指定",
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        MaxLines = 3, // 限制最大行数
                        TextTrimming = TextTrimming.CharacterEllipsis, // 超出部分显示...
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetColumn(valueBlock, 1);
                    Grid.SetRow(valueBlock, row);
                    grid.Children.Add(valueBlock);
                }

                AddDetailRow(0, "模组名称:", modInfo.Name);
                AddDetailRow(1, "当前版本:", modInfo.Version);
                AddDetailRow(2, "模组作者:", modInfo.Author);
                AddDetailRow(3, "所需SPT版本:", modInfo.RequiredSptVersion);

                // 调整ScrollViewer设置（关键修改）
                dialog.Content = new ScrollViewer
                {
                    Content = grid,
                    VerticalScrollMode = ScrollMode.Disabled, // 优先不使用滚动
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MaxHeight = 400, // 适当增大最大高度
                    Padding = new Thickness(12),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };

                // 异步显示前强制布局计算
                await Task.Delay(50); // 给UI线程喘息时间
                var result = await dialog.ShowAsync();
                return result == ContentDialogResult.Primary;
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        }

        #endregion

        #region Installation Process
        private async Task SCompleteModInstallation(StorageFile file, string tempDirectory)
        {
            var packageJsonPath = Directory.EnumerateFiles(tempDirectory, "package.json",
                SearchOption.AllDirectories).First();

            var modRootPath = Path.GetDirectoryName(packageJsonPath);
            var modDirectoryName = Path.GetFileName(modRootPath);
            var targetPath = Path.Combine(modsDirectoryPath, modDirectoryName);

            await Task.Run(() => SafeCopyDirectory(modRootPath, targetPath));

            await SUpdateModOrder(modDirectoryName);


            NotificationService.Instance.ShowNotification("安装完成", $"模组 {file.Name} 已成功安装，请刷新该页面", InfoBarSeverity.Success);
            ToastNotificationHelper.ShowNotification("安装完成",
                $"模组 {file.Name} 已成功安装", "确认",
                null, "通知", "通知");
        }

        private void SafeCopyDirectory(string source, string target)
        {
            if (Directory.Exists(target)) Directory.Delete(target, true);
            SCopyDirectory(source, target);
        }

        private async Task SUpdateModOrder(string modDirectoryName)
        {
            var serverModsPage = new ServerModsPage();

            var modOrder = (await serverModsPage.InitializeOrderJson()).ToList();
            if (!modOrder.Contains(modDirectoryName))
            {
                modOrder.Add(modDirectoryName);
                await serverModsPage.SaveOrderJson(modOrder);
            }
        }
        #endregion

        #region Helper Methods
        private void SShowErrorNotification(string message)
        {
            ToastNotificationHelper.ShowNotification("错误", message, "确认",
                null, "通知", "错误");
        }

        // 保持原有CopyDirectory方法不变
        private class TempDirectory : IDisposable
        {
            public string Path { get; }

            public TempDirectory()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    Guid.NewGuid().ToString());
            }

            public void Dispose()
            {
                try { Directory.Delete(Path, true); }
                catch { /* 可添加日志记录 */ }
            }
        }
        #endregion

        private void SCopyDirectory(string sourceDir, string destDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            var dirs = dir.GetDirectories();

            // 如果目标目录不存在，则创建
            Directory.CreateDirectory(destDir);

            // 复制文件
            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // 递归复制子目录
            foreach (var subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destDir, subDir.Name);
                SCopyDirectory(subDir.FullName, newDestinationDir);
            }
        }





        private void ModsList_DragOver(object sender, DragEventArgs e)
        {

            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖动至窗口内以安装模组";
            e.DragUIOverride.IsContentVisible = true;
        }

        private async void ModsList_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (IStorageItem item in items)
                {
                    if (item is StorageFile file)
                    {
                        string fileType = file.FileType.ToLower();
                        if (fileType == ".zip" || fileType == ".rar" || fileType == ".7z")
                        {
                            try
                            {
                                await InstallMod(file, App.m_window as MainWindow, CancellationToken.None);
                            }
                            catch (Exception ex)
                            {
                                ToastNotificationHelper.ShowNotification("错误", $"安装过程中出现错误：{ex.Message}", "确认", null, string.Empty, string.Empty);
                            }
                        }
                        else
                        {
                            ToastNotificationHelper.ShowNotification("不支持的文件类型", $"文件 \"{file.Name}\" 不是有效的模组文件（.zip、.rar 或 .7z）。", "确认", null, string.Empty, string.Empty);
                        }
                    }
                }
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

        private async Task LoadMods()
        {
            await Task.Run(() =>
            {
                string pluginsPath = Path.Combine(App.ManagerConfig.InstallPath, "BepInEx", "plugins");
                if (!Directory.Exists(pluginsPath))
                    return;

                var modFiles = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories);
                var disabledModFiles = Directory.GetFiles(pluginsPath, "*.dll.off", SearchOption.AllDirectories);
                var allModFiles = modFiles.Concat(disabledModFiles);

                EnabledMods.Clear();
                DisabledMods.Clear();

                // 加载模组配置
                var modConfigs = ModConfig.Load();

                foreach (var modFile in allModFiles)
                {
                    var modInfo = new ModInfo
                    {
                        FilePath = modFile,
                        Name = Path.GetFileNameWithoutExtension(modFile).Replace(".off", ""),
                        IsEnabled = !modFile.EndsWith(".off"), // 根据文件名后缀来设置启用状态
                        CustomName = modConfigs.ContainsKey(modFile) ? modConfigs[modFile] : null
                    };

                    try
                    {
                        if (modInfo.IsEnabled)
                        {
                            // 使用 AssemblyLoadContext 动态加载程序集
                            var context = new AssemblyLoadContext("ModLoadContext", isCollectible: true);
                            Assembly modAssembly = context.LoadFromAssemblyPath(modFile);

                            modInfo.Version = modAssembly.GetName().Version.ToString();
                            var descriptionAttribute = modAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
                            modInfo.Description = descriptionAttribute?.Description ?? "无说明";

                            // 卸载上下文以释放文件锁
                            context.Unload();
                            GC.Collect();
                            GC.WaitForPendingFinalizers();

                            EnabledMods.Add(modInfo);
                        }
                        else
                        {
                            modInfo.Version = "未知版本";
                            modInfo.Description = "无说明";

                            DisabledMods.Add(modInfo);
                        }
                    }
                    catch
                    {
                        modInfo.Version = "未知版本";
                        modInfo.Description = "无说明";

                        DisabledMods.Add(modInfo);
                    }
                }
            });

            ModsList.ItemsSource = EnabledMods.Concat(DisabledMods);
        }



        private async Task ManageModStatus(ModInfo modInfo, bool enable)
        {
            try
            {
                if (enable && !modInfo.IsEnabled)
                {
                    string offFilePath = modInfo.FilePath + ".off";
                    if (modInfo.FilePath.EndsWith(".off"))
                    {
                        offFilePath = modInfo.FilePath;
                        modInfo.FilePath = modInfo.FilePath.Substring(0, modInfo.FilePath.Length - 4);
                    }

                    if (File.Exists(offFilePath))
                    {
                        File.Move(offFilePath, modInfo.FilePath);
                        modInfo.IsEnabled = true;
                        EnabledMods.Add(modInfo);
                        DisabledMods.Remove(modInfo);

                        // 重新加载mod列表以确保状态更新
                        await LoadMods();
                    }
                }
                else if (!enable && modInfo.IsEnabled)
                {
                    string newFilePath = modInfo.FilePath + ".off";
                    if (!modInfo.FilePath.EndsWith(".off"))
                    {
                        File.Move(modInfo.FilePath, newFilePath);
                        modInfo.FilePath = newFilePath;
                        modInfo.IsEnabled = false;
                        DisabledMods.Add(modInfo);
                        EnabledMods.Remove(modInfo);

                        // 重新加载mod列表以确保状态更新
                        await LoadMods();
                    }
                }

                // 保存自定义名称
                modConfigs[modInfo.FilePath] = modInfo.CustomName;
                ModConfig.Save(modConfigs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManageModStatus中的错误: {ex.Message}");
            }
        }

        


        private async void ModCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var modInfo = checkbox.DataContext as ModInfo;

            if (modInfo != null)
            {
                await ManageModStatus(modInfo, true);
            }
        }

        private async void ModCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var modInfo = checkbox.DataContext as ModInfo;

            if (modInfo != null)
            {
                await ManageModStatus(modInfo, false);
            }
        }

        private ModInfo selectedMod;

        private async void ModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModsList.SelectedItem is ModInfo selectedMod)
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = $"编辑模组 '{selectedMod.Name}'的自定义名称 ",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    Content = new StackPanel
                    {
                        Children =
                {
                    new TextBlock { Text = "自定义名称：" },
                    new TextBox
                    {
                        Text = selectedMod.CustomName ?? selectedMod.Name,
                        Name = "CustomNameTextBox",
                        PlaceholderText = "输入自定义名称",
                        Margin = new Thickness(0, 0, 0, 10)
                    }
                }
                    }
                };

                dialog.PrimaryButtonClick += async (s, args) =>
                {
                    var customNameTextBox = (TextBox)dialog.FindName("CustomNameTextBox");
                    if (!string.IsNullOrWhiteSpace(customNameTextBox.Text))
                    {
                        // 更新选中的模组自定义名称
                        selectedMod.CustomName = customNameTextBox.Text;

                        // 更新配置文件
                        var modConfigs = ModConfig.Load();
                        modConfigs[selectedMod.FilePath] = selectedMod.CustomName;
                        ModConfig.Save(modConfigs);

                        // 更新显示
                        await RefreshModList();
                    }
                    else
                    {
                        args.Cancel = true; // 防止对话框关闭
                    }
                };

                await dialog.ShowAsync();
            }
        }

        private async Task RefreshModList()
        {
            // 重新加载模组列表以确保显示更新后的自定义名称
            await LoadMods();
        }





        private void ModsList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine("KeyDown 触发"); // 调试输出

            if (e.Key == VirtualKey.Delete)
            {
                Debug.WriteLine("Delete 按下"); // 调试输出

                var selectedMod = ModsList.SelectedItem as ModInfo;
                if (selectedMod != null)
                {
                    Debug.WriteLine($"选中的MOD: {selectedMod.Name}"); // 调试输出
                    DeleteMod(selectedMod).ConfigureAwait(false); // 确保DeleteMod方法被调用
                }
                else
                {
                    Debug.WriteLine("未选中任何MOD"); // 调试输出
                }
            }
        }

        private async Task DeleteMod(ModInfo mod)
        {
            Debug.WriteLine("DeleteMod 方法被调用");
            Debug.WriteLine($"准备删除MOD: {mod.Name}, 文件路径: {mod.FilePath}");

            // 提示用户确认删除
            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot, // 确保 XamlRoot 设置正确
                Title = "确认删除",
                Content = $"确定要删除模组 '{mod.Name}' 吗？此操作将删除对应的文件。",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消"
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // 删除文件
                    if (File.Exists(mod.FilePath))
                    {
                        File.Delete(mod.FilePath);
                        Debug.WriteLine($"文件已成功删除: {mod.FilePath}");

                        // 从 EnabledMods 或 DisabledMods 中移除
                        if (EnabledMods.Contains(mod))
                        {
                            EnabledMods.Remove(mod); // 从启用的模组列表中移除
                        }
                        else if (DisabledMods.Contains(mod))
                        {
                            DisabledMods.Remove(mod); // 从禁用的模组列表中移除
                        }

                        // 更新 UI 绑定
                        ModsList.ItemsSource = EnabledMods.Concat(DisabledMods).ToList(); // 重新绑定列表

                        ToastNotificationHelper.ShowNotification("完成", $"模组：{mod.Name} 已删除", "确认", null, string.Empty, string.Empty);
                    }
                    else
                    {
                        Debug.WriteLine($"文件不存在: {mod.FilePath}");
                        ToastNotificationHelper.ShowNotification("错误", $"模组文件不存在：{mod.FilePath}", "确认", null, string.Empty, string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"删除文件时发生错误: {ex.Message}");
                    ToastNotificationHelper.ShowNotification("错误", $"删除模组文件时发生错误：{ex.Message}", "确认", null, string.Empty, string.Empty);
                }
            }
            else
            {
                Debug.WriteLine("用户取消了删除操作");
            }
        }


        private async Task TryDeleteFileAsync(string filePath, int retries = 3, int delay = 1000)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    // 尝试重命名文件
                    //string tempFilePath = filePath + ".deleting";
                    //File.Move(filePath, tempFilePath);

                    // 删除重命名后的文件
                    File.Delete(filePath);
                    return;
                }
                catch (IOException ex) when (ex.Message.Contains("used by another process"))
                {
                    Debug.WriteLine($"删除文件时发生错误: {ex.Message}");
                    await Task.Delay(delay); // 等待一会儿再重试
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine($"删除文件时发生错误: {ex.Message}");
                    await Task.Delay(delay); // 等待一会儿再重试
                }
            }
            throw new IOException($"无法删除文件: {filePath}");
        }

    }

    public class ModInfo
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }

        public string CustomName { get; set; } // 新增自定义名称属性
    }
}
