using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Diagnostics;
using Neko.EFT.Manager.X.Classes;
using System.IO.Compression;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Neko.EFT.Manager.X.Windows;
using System.Text;
using System.Text.Json.Serialization;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using SharpCompress.Archives;
using System.Threading;
using SharpCompress.Common;
using SevenZipExtractor;
using CommunityToolkit.WinUI;
using static Neko.EFT.Manager.X.Pages.ModsPage;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Neko.EFT.Manager.X.Pages.GameResManager
{
    public sealed partial class ServerModsPage : Page
    {
        private readonly ModInstaller _modInstaller;

        public StorageFile file;
        public ObservableCollection<ServerModInfo> Mods { get; set; } = new ObservableCollection<ServerModInfo>();
        private string modsDirectoryPath = Path.Combine(App.ManagerConfig.InstallPath, "user", "mods");
        private string modsBackupDirectoryPath = Path.Combine(App.ManagerConfig.InstallPath, "user", "modbackup-off");
        private ServerCompatibilityInfo compatibilityInfo;
        private string AkiServerPath = App.ManagerConfig.AkiServerPath;
        public string ServerVersion { get; set; }



        public ServerModsPage()
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
            this.Loaded += ModsPage_Loaded;
            this.ModGrid.Background.Opacity = 0.96;
            this.InstallModButton.Background.Opacity = 0.96;

            // 初始化兼容性信息类
            compatibilityInfo = new ServerCompatibilityInfo(App.ManagerConfig.AkiServerPath);
        }


        private async void ModsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ManagerConfig.InstallPath != null)
            {
                LoadingRing.Visibility = Visibility.Visible;
                ModGrid.Visibility = Visibility.Collapsed;

                await LoadMods();

                LoadingRing.Visibility = Visibility.Collapsed;
                ModGrid.Visibility = Visibility.Visible;

                DataContext = this;

                ServerVersion = GetServerVersion();
                ServerVersionTextBlock.Text = $"当前服务端版本: {ServerVersion}";

                this.ModsList.DragOver += ModsList_DragOver;
                this.ModsList.Drop += ModsList_Drop;
            }
            else
            {
                await Utils.ShowInfoBar("配置错误", $"未配置 \"安装路径\"。转到设置配置安装路径。", InfoBarSeverity.Error);
            }
        }

        public string GetServerVersion()
        {
            string[] serverExeNames = { "Aki.Server.exe", "SPT.Server.exe", "Server.exe" };

            foreach (var exeName in serverExeNames)
            {
                string serverExePath = Path.Combine(AkiServerPath, exeName);
                if (File.Exists(serverExePath))
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(serverExePath);
                    // 返回标准化的版本号，转换为三部分格式
                    Version version = new Version(fileVersionInfo.FileVersion);
                    Version threeDigitVersion = new Version(version.Major, version.Minor, version.Build == -1 ? 0 : version.Build);
                    return threeDigitVersion.ToString();
                }
            }

            return "N/A";
        }

        private async Task LoadMods()
        {
            try
            {
                Mods.Clear();

                if (string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
                {
                    await ShowErrorDialog("配置错误", "未配置 \"安装路径\"。转到设置配置安装路径。");
                    return;
                }

                // 初始化 order.json
                var modOrder = await InitializeOrderJson();

                var allModDirectories = new List<string>();

                if (Directory.Exists(modsDirectoryPath))
                {
                    allModDirectories.AddRange(Directory.GetDirectories(modsDirectoryPath));
                }

                if (Directory.Exists(modsBackupDirectoryPath))
                {
                    allModDirectories.AddRange(Directory.GetDirectories(modsBackupDirectoryPath));
                }

                foreach (var modDirectory in allModDirectories.OrderBy(d => modOrder.IndexOf(Path.GetFileName(d))))
                {
                    string configFilePath = Path.Combine(modDirectory, "package.json");

                    if (File.Exists(configFilePath))
                    {
                        string json = await File.ReadAllTextAsync(configFilePath);
                        var modInfo = JsonSerializer.Deserialize<ServerModInfo>(json);
                        if (modInfo != null)
                        {
                            modInfo.DirectoryPath = modDirectory;
                            modInfo.DisplayName = Path.GetFileName(modDirectory);

                            // 检查兼容性
                            CompatibilityStatus compatibilityStatus = compatibilityInfo.CheckCompatibility(modInfo);
                            modInfo.CompatibilityStatus = compatibilityStatus;

                            Mods.Add(modInfo);
                        }
                    }
                }

                // 检查和更新模组的启用状态和顺序
                await CheckAndUpdateModStatusAndOrder(modOrder);

                ModsList.ItemsSource = Mods;
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("错误", $"加载模组配置时发生错误: {ex.Message}");
            }
        }



        public async Task<List<string>> InitializeOrderJson()
        {
            string orderFilePath = Path.Combine(modsDirectoryPath, "order.json");
            List<string> modOrder = new List<string>();

            if (File.Exists(orderFilePath))
            {
                string orderFileContent = await File.ReadAllTextAsync(orderFilePath);
                if (!string.IsNullOrWhiteSpace(orderFileContent))
                {
                    var orderJson = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(orderFileContent, new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        PropertyNameCaseInsensitive = true
                    });
                    modOrder = orderJson?["order"] ?? new List<string>();
                }
            }

            if (modOrder.Count == 0)
            {
                var modDirectories = Directory.GetDirectories(modsDirectoryPath).Select(Path.GetFileName).ToList();

                foreach (var modDirectory in modDirectories)
                {
                    if (!modOrder.Contains(modDirectory))
                    {
                        modOrder.Add(modDirectory);
                    }
                }

                var updatedOrderJson = new Dictionary<string, List<string>> { { "order", modOrder } };
                string updatedOrderFileContent = JsonSerializer.Serialize(updatedOrderJson, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                await File.WriteAllTextAsync(orderFilePath, updatedOrderFileContent);

                ToastNotificationHelper.ShowNotification("初始化完成", $"模组加载顺序列表已重新初始化：\n{updatedOrderFileContent}", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "测试");
            }

            // 移除重复项
            modOrder = modOrder.Distinct().ToList();
            await SaveOrderJson(modOrder);

            return modOrder;
        }

        public async Task SaveOrderJson(List<string> modOrder)
        {
            string orderFilePath = Path.Combine(modsDirectoryPath, "order.json");
            var updatedOrderJson = new Dictionary<string, List<string>> { { "order", modOrder } };
            string updatedOrderFileContent = JsonSerializer.Serialize(updatedOrderJson, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            await File.WriteAllTextAsync(orderFilePath, updatedOrderFileContent);
        }


        private async Task SaveMod(ServerModInfo modInfo)
        {
            try
            {
                string configFilePath = Path.Combine(modInfo.DirectoryPath, "package.json");

                if (File.Exists(configFilePath))
                {
                    // 读取原始 JSON 数据
                    string json = await File.ReadAllTextAsync(configFilePath, Encoding.UTF8);
                    var options = new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        PropertyNameCaseInsensitive = true
                    };

                    // 将 JSON 反序列化为字典
                    var existingModInfoDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

                    if (existingModInfoDict != null)
                    {
                        // 更新 IsEnabled 字段
                        if (existingModInfoDict.ContainsKey("IsEnabled"))
                        {
                            existingModInfoDict["IsEnabled"] = modInfo.IsEnabled;
                        }
                        else
                        {
                            existingModInfoDict.Add("IsEnabled", modInfo.IsEnabled);
                        }

                        // 序列化更新后的数据
                        string updatedJson = JsonSerializer.Serialize(existingModInfoDict, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });

                        // 使用 StreamWriter 写入文件，确保不包含 BOM
                        using (var writer = new StreamWriter(configFilePath, false, new UTF8Encoding(false)))
                        {
                            await writer.WriteAsync(updatedJson);
                        }
                    }
                    else
                    {
                        await ShowErrorDialog("错误", "无法读取模组配置文件的原始数据。");
                    }
                }
                else
                {
                    await ShowErrorDialog("错误", "模组配置文件不存在。");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("错误", $"保存模组配置时发生错误: {ex.Message}");
            }
        }






        private async Task ManageModStatus(ServerModInfo servermodInfo, bool enable)
        {
            try
            {
                string modDirectoryPath = servermodInfo.DirectoryPath;
                string targetDirectoryPath = enable ? modsDirectoryPath : modsBackupDirectoryPath;

                if (Directory.Exists(modDirectoryPath))
                {
                    string newModDirectoryPath = Path.Combine(targetDirectoryPath, Path.GetFileName(modDirectoryPath));

                    if (!Directory.Exists(targetDirectoryPath))
                    {
                        Directory.CreateDirectory(targetDirectoryPath);
                    }

                    if (Directory.Exists(newModDirectoryPath))
                    {
                        await ShowErrorDialog("错误", "目标目录已存在，请检查并重试。");
                        return;
                    }

                    Directory.Move(modDirectoryPath, newModDirectoryPath);
                    servermodInfo.IsEnabled = enable;
                    servermodInfo.DirectoryPath = newModDirectoryPath;

                    await SaveMod(servermodInfo);

                    var modOrder = (await InitializeOrderJson()).ToList();
                    if (enable)
                    {
                        if (!modOrder.Contains(Path.GetFileName(newModDirectoryPath)))
                        {
                            modOrder.Add(Path.GetFileName(newModDirectoryPath));
                        }
                    }
                    else
                    {
                        modOrder.Remove(Path.GetFileName(newModDirectoryPath));
                    }
                    await SaveOrderJson(modOrder);

                    // 更新模组列表顺序和状态
                    Mods.Remove(Mods.FirstOrDefault(m => m.DirectoryPath == newModDirectoryPath));
                    Mods.Add(servermodInfo);

                }
                else
                {
                    Debug.WriteLine($"目录未找到: {modDirectoryPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ManageModStatus中的错误: {ex.Message}");
            }
        }

        private async Task CheckAndUpdateModStatusAndOrder(List<string> modOrder)
        {
            var modDirectories = Mods.Select(m => m.DirectoryPath).ToList();

            // 检查顺序
            var currentOrder = modDirectories.OrderBy(d => modOrder.IndexOf(Path.GetFileName(d))).ToList();
            if (!Enumerable.SequenceEqual(modDirectories, currentOrder))
            {
                // 重新排序 Mods
                Mods.Clear();
                foreach (var modDirectory in currentOrder)
                {
                    string configFilePath = Path.Combine(modDirectory, "package.json");

                    if (File.Exists(configFilePath))
                    {
                        string json = await File.ReadAllTextAsync(configFilePath);
                        var modInfo = JsonSerializer.Deserialize<ServerModInfo>(json);
                        if (modInfo != null)
                        {
                            modInfo.DirectoryPath = modDirectory;
                            modInfo.IsEnabled = modDirectory.StartsWith(modsDirectoryPath);
                            modInfo.DisplayName = Path.GetFileName(modDirectory);
                            Mods.Add(modInfo);
                        }
                    }
                }
            }

            // 检查启用状态
            foreach (var mod in Mods)
            {
                bool shouldBeEnabled = modOrder.Contains(Path.GetFileName(mod.DirectoryPath));
                if (mod.IsEnabled != shouldBeEnabled)
                {
                    mod.IsEnabled = shouldBeEnabled;
                    await SaveMod(mod);
                }
            }
        }

        private async void ModCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var servermodInfo = checkbox.DataContext as ServerModInfo;

            if (servermodInfo != null)
            {
                await ManageModStatus(servermodInfo, true);
            }
        }

        private async void ModCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var servermodInfo = checkbox.DataContext as ServerModInfo;

            if (servermodInfo != null)
            {
                await ManageModStatus(servermodInfo, false);
            }
        }

        private void ModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle item selection if needed
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



        // 新增ModInfo类用于存储模组信息
        public class ModInfo
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

        private async void InstallMod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                file = await PickModFileAsync();

                NotificationService.Instance.ShowNotification("正在安装", $"模组 {file.Name} 正在安装中\n 如文件较大请耐心等待", InfoBarSeverity.Warning);
                if (file == null) return;

                using (var tempDirectory = new TempDirectory())
                {
                    await ExtractModFiles(file, tempDirectory.Path);

                    var (modInfo, error) = await ParsePackageJson(tempDirectory.Path);

                    if (modInfo == null)
                    {
                        switch (error)
                        {
                            case PackageJsonError.FileNotFound:
                                NotificationService.Instance.ShowNotification("安装取消", "未找到 package.json 文件，可能为客户端模组，正在尝试安装客户端模组...", InfoBarSeverity.Warning);


                                await Task.Delay(3000);

                                MainWindow window = App.m_window as MainWindow;

                                try
                                {
                                    var zipFile = file;
                                    if (zipFile != null)
                                    {

                                        await CInstallMod(zipFile, window, CancellationToken.None);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    NotificationService.Instance.ShowNotification("错误", $"选择文件时发生错误：{ex.Message}", InfoBarSeverity.Error);
                                    ToastNotificationHelper.ShowNotification("错误", $"选择文件时发生错误：{ex.Message}", "确定", null, string.Empty, string.Empty);
                                }

                                break;
                            case PackageJsonError.InvalidFormat:
                                NotificationService.Instance.ShowNotification("安装取消", "package.json 格式无效，请检查文件格式", InfoBarSeverity.Warning);
                                ToastNotificationHelper.ShowNotification("取消", "package.json 格式无效", "确认", null, "通知", "警告");
                                break;
                            case PackageJsonError.MissingRequiredFields:
                                NotificationService.Instance.ShowNotification("安装取消", "package.json 缺少必要字段，请检查文件内容", InfoBarSeverity.Warning);
                                ToastNotificationHelper.ShowNotification("取消", "package.json 缺少必要字段", "确认", null, "通知", "警告");
                                break;
                            case PackageJsonError.UnknownError:
                                NotificationService.Instance.ShowNotification("安装取消", "发生未知错误，请检查日志或联系支持", InfoBarSeverity.Error);
                                ToastNotificationHelper.ShowNotification("取消", "发生未知错误", "确认", null, "通知", "错误");
                                break;
                        }

                        return;
                    }

                    if (!await ShowModDetailsConfirmationAsync(modInfo))
                    {
                        NotificationService.Instance.ShowNotification("安装取消", "已取消安装或模组类型不匹配，请尝试前往客户端mod管理器安装此文件，如你已取消安装请忽略", InfoBarSeverity.Error);

                        ToastNotificationHelper.ShowNotification("取消", "模组类型不匹配，请尝试前往客户端mod管理器安装此文件", "确认",
                            null, "通知", "错误");

                        return;
                    }

                    await CompleteModInstallation(file, tempDirectory.Path);
                }
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"安装过程中发生错误: {ex.Message}");
            }
        }

        #region File Operations
        private async Task<StorageFile> PickModFileAsync()
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

        private async Task ExtractModFiles(StorageFile file, string tempDirectory)
        {
            ToastNotificationHelper.ShowNotification("解压中", $"正在解压 {file.Name}...", "确认",
                null, "通知", "错误");
            Directory.CreateDirectory(tempDirectory);
            await Task.Run(() => ExtractArchive(file.Path, tempDirectory, CancellationToken.None));
        }
        #endregion

        #region Mod Information Handling
        // 添加错误类型枚举
        public enum PackageJsonError
        {
            None = 0,
            FileNotFound,
            InvalidFormat,
            MissingRequiredFields,
            UnknownError
        }

        private async Task<(ModInfo modInfo, PackageJsonError error)> ParsePackageJson(string tempDirectory)
        {
            try
            {
                var packageJsonPath = Directory.EnumerateFiles(tempDirectory, "package.json",
                    SearchOption.AllDirectories).FirstOrDefault();

                if (packageJsonPath == null)
                {
                    return (null, PackageJsonError.FileNotFound);
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                using var stream = File.OpenRead(packageJsonPath);
                var modInfo = await JsonSerializer.DeserializeAsync<ModInfo>(stream, options);

                if (string.IsNullOrWhiteSpace(modInfo?.Name))
                {
                    return (null, PackageJsonError.MissingRequiredFields);
                }

                return (modInfo, PackageJsonError.None);
            }
            catch (JsonException)
            {
                return (null, PackageJsonError.InvalidFormat);
            }
            catch (Exception)
            {
                return (null, PackageJsonError.UnknownError);
            }
        }

        private async Task ExtractArchive(string archivePath, string extractPath, CancellationToken cancellationToken)
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

        private async Task<bool> ShowModDetailsConfirmationAsync(ModInfo modInfo)
        {
            return await App.MainWindow.DispatcherQueue.EnqueueAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "模组详情 - 安装确认",
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
        private async Task CompleteModInstallation(StorageFile file, string tempDirectory)
        {
            var packageJsonPath = Directory.EnumerateFiles(tempDirectory, "package.json",
                SearchOption.AllDirectories).First();

            var modRootPath = Path.GetDirectoryName(packageJsonPath);
            var modDirectoryName = Path.GetFileName(modRootPath);
            var targetPath = Path.Combine(modsDirectoryPath, modDirectoryName);

            await Task.Run(() => SafeCopyDirectory(modRootPath, targetPath));

            await UpdateModOrder(modDirectoryName);


            NotificationService.Instance.ShowNotification("安装完成", $"模组 {file.Name} 已成功安装，请刷新该页面", InfoBarSeverity.Success);
            ToastNotificationHelper.ShowNotification("安装完成",
                $"模组 {file.Name} 已成功安装","确认",
                null, "通知", "通知");
        }

        private void SafeCopyDirectory(string source, string target)
        {
            if (Directory.Exists(target)) Directory.Delete(target, true);
            CopyDirectory(source, target);
        }

        private async Task UpdateModOrder(string modDirectoryName)
        {
            var modOrder = (await InitializeOrderJson()).ToList();
            if (!modOrder.Contains(modDirectoryName))
            {
                modOrder.Add(modDirectoryName);
                await SaveOrderJson(modOrder);
            }
        }
        #endregion

        #region Helper Methods
        private void ShowErrorNotification(string message)
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

        private void CopyDirectory(string sourceDir, string destDir)
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
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        private async void MoveModUp_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var servermodInfo = button.DataContext as ServerModInfo;

            if (servermodInfo != null)
            {
                int index = Mods.IndexOf(servermodInfo);
                if (index > 0)
                {
                    Mods.Move(index, index - 1); // 使用 ObservableCollection 的 Move 方法
                    await UpdateModOrder();
                }
            }
        }

        private async void MoveModDown_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var servermodInfo = button.DataContext as ServerModInfo;

            if (servermodInfo != null)
            {
                int index = Mods.IndexOf(servermodInfo);
                if (index < Mods.Count - 1)
                {
                    Mods.Move(index, index + 1); // 使用 ObservableCollection 的 Move 方法
                    await UpdateModOrder();
                }
            }
        }

        private async Task UpdateModOrder()
        {
            var modOrder = Mods.Select(m => Path.GetFileName(m.DirectoryPath)).ToList();
            await SaveOrderJson(modOrder);
        }

        private bool previousSafeModeState = false;
        private List<ServerModInfo> disabledModsBeforeSafeMode = new List<ServerModInfo>();

        private async void SafeModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            bool currentSafeModeState = SafeModeToggle.IsOn;

            if (currentSafeModeState != previousSafeModeState)
            {
                previousSafeModeState = currentSafeModeState;
                SafeModeIcon.Visibility = currentSafeModeState ? Visibility.Visible : Visibility.Collapsed;

                if (currentSafeModeState)
                {
                    ToastNotificationHelper.ShowNotification("安全模式", $"你已进入安全模式，将禁用所有模组：\n{currentSafeModeState}", "确认", (arg) =>
                    {
                        // 执行其他操作...
                    }, "通知", "测试");
                    // 进入安全模式，禁用所有模组
                    disabledModsBeforeSafeMode.Clear();
                    var modsToDisable = new List<ServerModInfo>();

                    foreach (var mod in Mods)
                    {
                        if (mod.IsEnabled)
                        {
                            disabledModsBeforeSafeMode.Add(new ServerModInfo
                            {
                                Name = mod.Name,
                                DirectoryPath = mod.DirectoryPath,
                                IsEnabled = mod.IsEnabled
                            });

                            modsToDisable.Add(mod);
                        }
                    }

                    foreach (var mod in modsToDisable)
                    {
                        await ManageModStatus(mod, false);
                    }
                }
                else
                {
                    // 退出安全模式，恢复之前禁用的模组状态
                    var modsToEnable = new List<ServerModInfo>(disabledModsBeforeSafeMode);

                    foreach (var mod in modsToEnable)
                    {
                        await ManageModStatus(mod, true);
                    }

                    disabledModsBeforeSafeMode.Clear();
                }
            }



        }

        private void ModsList_DragOver(object sender, DragEventArgs e)
        {

            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖动以安装模组";
            e.DragUIOverride.IsContentVisible = true;
        }

        private async void ModsList_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items.OfType<StorageFile>())
                {
                    await ProcessDroppedFile(item);
                }
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"处理拖放文件时发生错误: {ex.Message}");
            }
        }

        private async Task ProcessDroppedFile(StorageFile file)
        {
            // 检查文件类型是否受支持
            if (!IsSupportedArchive(file.FileType))
            {
                ShowUnsupportedFileNotification(file);
                return;
            }

            // 初始化临时目录
            using (var tempDirectory = new TempDirectory())
            {
                try
                {

                    NotificationService.Instance.ShowNotification("正在安装模组", $"模组: {file.Name}  正在安装中...", InfoBarSeverity.Warning);
                    // 解压模组文件到临时目录
                    await ExtractModFiles(file, tempDirectory.Path);

                    // 解析 package.json 文件
                    var (modInfo, error) = await ParsePackageJson(tempDirectory.Path);

                    // 检查是否无法解析或模组信息为空
                    if (modInfo == null)
                    {
                        switch (error)
                        {
                            case PackageJsonError.FileNotFound:
                                NotificationService.Instance.ShowNotification("正在安装模组", $"模组: {file.Name}  未找到 package.json 文件，可能为客户端模组，正在尝试安装客户端模组...", InfoBarSeverity.Warning);
                                ShowErrorNotification("未找到 package.json 文件，可能为客户端模组，正在尝试安装客户端模组...");

                                var window = App.m_window as MainWindow;
                                
                                    var zipFile = file;
                                    if (zipFile != null)
                                    {

                                        await CInstallMod(zipFile, window, CancellationToken.None);
                                    }
                               
                                break;
                            case PackageJsonError.InvalidFormat:
                                ShowErrorNotification("package.json 格式无效，请检查文件格式");
                                
                                break;
                            case PackageJsonError.MissingRequiredFields:
                                ShowErrorNotification("package.json 缺少必要字段，请检查文件内容");
                               
                                break;
                            case PackageJsonError.UnknownError:
                                ShowErrorNotification("发生未知错误，请检查日志或联系支持");
                                
                                break;
                        }
                        return;
                    }

                    // 检查是否有未完成的确认操作
                    if (_isConfirmingModDetails)
                    {
                        ShowErrorNotification("正在进行确认操作，请稍候...");
                        return;
                    }

                    // 设置正在确认
                    _isConfirmingModDetails = true;

                    // 显示模组详细信息确认
                    bool isConfirmed = await ShowModDetailsConfirmationAsync(modInfo);

                    // 如果用户取消
                    if (!isConfirmed)
                    {

                        NotificationService.Instance.ShowNotification("安装取消", $"服务端模组: {file.Name}  安装已取消。", InfoBarSeverity.Error);
                        ShowErrorNotification("模组安装已取消");
                    }
                    else
                    {
                        // 完成模组安装
                        await CompleteModInstallation(file, tempDirectory.Path);
                    }

                    // 还原确认状态
                    _isConfirmingModDetails = false;
                }
                catch (Exception ex)
                {
                    ShowErrorNotification($"安装 {file.Name} 失败: {ex.Message}");
                }
            }
        }

        // 私有字段，用于标记是否正在确认模组详细信息
        private bool _isConfirmingModDetails = false;

        #region Helper Methods
        private bool IsSupportedArchive(string fileType)
        {
            var supportedTypes = new[] { ".zip", ".rar", ".7z" };
            return supportedTypes.Contains(fileType.ToLower());
        }

        private void ShowUnsupportedFileNotification(StorageFile file)
        {
            ToastNotificationHelper.ShowNotification("不支持的文件类型",
                $"文件 \"{file.Name}\" 不是有效的模组文件（zip/7z/rar）。",
                "确认",
                null,
                "通知",
                "错误");
        }
        #endregion

        private void ModsList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Delete)
            {
                var selectedMod = ModsList.SelectedItem as ServerModInfo;
                if (selectedMod != null)
                {
                    // 执行删除操作
                    DeleteMod(selectedMod);
                }
            }
        }

        private async void DeleteMod(ServerModInfo mod)
        {
            // 从数据源中删除 mod
            var modsList = ModsList.ItemsSource as ObservableCollection<ServerModInfo>;
            if (modsList != null && modsList.Contains(mod))
            {
                // 提示用户确认删除
                var dialog = new ContentDialog
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "确认删除",
                    Content = $"确定要删除模组 '{mod.DisplayName}' 吗？此操作将删除对应的文件夹。",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消"
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // 删除文件夹
                    try
                    {
                        Directory.Delete(mod.DirectoryPath, true);
                    }
                    catch (Exception ex)
                    {
                        //await ShowErrorDialog("错误", $"删除模组文件夹时发生错误: {ex.Message}");
                        ToastNotificationHelper.ShowNotification("错误", $"删除模组文件夹时发生错误：{ex.Message}", "确认", null, string.Empty, string.Empty);
                        return;
                    }

                    // 从列表中移除
                    modsList.Remove(mod);
                    ToastNotificationHelper.ShowNotification("完成", $"模组：{mod.DisplayName} 已删除", "确认", null, string.Empty, string.Empty);
                }
            }
        }


        public async Task CInstallMod(StorageFile zipFile, MainWindow window, CancellationToken cancellationToken)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // 显示安装开始通知
                CShowNotification("模组安装", $"模组 {zipFile.Name} 安装中...", "确定", "通知");
                NotificationService.Instance.ShowNotification("正在安装", $"正在安装客户端模组文件: {zipFile.Name} ...\n 如文件较大请耐心等待。", InfoBarSeverity.Warning);

                // 解压文件
                await ExtractArchive(zipFile.Path, tempDirectory, cancellationToken);

                // 获取压缩包内的 DLL 文件信息
                var dllInfo = await CGetModInfoFromDll(tempDirectory);

                // 显示模组信息
                await CShowModInfoAndConfirm(zipFile.Name, dllInfo, tempDirectory, cancellationToken);

                // 安装模组
                await _CInstallMod(tempDirectory, cancellationToken);

                // 安装完成通知
                NotificationService.Instance.ShowNotification("安装完成", $"客户端模组文件： {zipFile.Name} 已完成安装\n 请重新导肮至客户端模组页面查看", InfoBarSeverity.Success);
                CShowNotification("完成", "模组安装完成。", "确定", "通知");
            }
            catch (OperationCanceledException)
            {


                NotificationService.Instance.ShowNotification("安装取消", $"模组: {zipFile.Name} 安装已被取消。", InfoBarSeverity.Error);
                // 用户取消安装
                CShowNotification("取消", "模组安装已取消", "确认", "通知");
            }

            catch (InvalidOperationException ex)
            {
                // 捕获其他异常
                CShowNotification("错误", $"{ex.Message}\n 此文件不是模组文件", "确认", "通知");
                await Task.Delay(3000); // 等待显示通知


            }
            catch (Exception ex)
            {
                // 捕获其他异常
                CShowNotification("错误", $"安装过程中发生错误：{ex.Message}", "确认", "通知");
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
                        CShowNotification("错误", $"删除临时文件时发生错误：{ex.Message}", "确认", "通知");
                    }
                }
            }
        }

        public async Task CShowModInfoAndConfirm(string fileName, List<DllInfo> dllInfo, string tempDirectory, CancellationToken cancellationToken)
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

        public async Task<List<DllInfo>> CGetModInfoFromDll(string directoryPath)
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
                        CShowNotification("警告", $"获取 {dllFile} 的信息时发生错误：{ex.Message}", "确认", "通知");
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
                CShowNotification("错误", $"获取 DLL 文件信息失败：{ex.Message}", "确认", "通知");
                throw; // 重新抛出异常以便上层处理
            }

            return dllInfoList;
        }

        public class CDllInfo
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
        }

        public async Task _CInstallMod(string tempDirectory, CancellationToken cancellationToken)
        {
            try
            {
                string modDirectory = Path.Combine(App.ManagerConfig.InstallPath, "BepInEx", "plugins");

                if (CIsBepInExMod(tempDirectory))
                {
                    await CInstallBepInExMod(tempDirectory, modDirectory, cancellationToken);
                }
                else if (ContainsDllFiles(tempDirectory))
                {
                    await CInstallGeneralMod(tempDirectory, modDirectory, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("压缩文件内不包含有效的 DLL 文件。请确保是客户端模组文件");
                }
            }
            catch (Exception ex)
            {
                CShowNotification("错误", $"安装过程中出现错误：{ex.Message}", "确认", "通知");
                throw; // 重新抛出异常以便上层处理
            }
        }

        public async Task CInstallBepInExMod(string tempDirectory, string modDirectory, CancellationToken cancellationToken)
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

        public async Task CInstallGeneralMod(string tempDirectory, string modDirectory, CancellationToken cancellationToken)
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




        public async Task CExtractArchive(string archivePath, string extractPath, CancellationToken cancellationToken)
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


        public bool CIsBepInExMod(string directoryPath)
        {
            return Directory.Exists(Path.Combine(directoryPath, "BepInEx", "plugins"));
        }

        public bool ContainsDllFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*.dll", SearchOption.AllDirectories).Any();
        }

        public void CShowNotification(string title, string content, string actionButton, string tag)
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

    }

}
