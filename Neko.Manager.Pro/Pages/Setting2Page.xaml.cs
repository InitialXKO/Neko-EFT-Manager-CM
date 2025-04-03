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
using Neko.EFT.Manager.X.Classes;
using System.Diagnostics;
using System.Threading.Tasks;
using static GClass1750;
using static Neko.EFT.Manager.X.Classes.ServerConfigManager;
using Newtonsoft.Json;
using System.Reflection;
using Windows.System;
using Windows.UI.Popups;
using Neko.EFT.Manager.X.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Setting2Page : Page
    {
        public static event Action InstallPathChanged;
        private const string FileSelectorExecutable = "FileSelector.exe";
        private const string ClientArgument = "client";
        private const string ServerArgument = "server";
        private List<ServerSourceConfig> serverSources;
        private AppConfig config;
        public Setting2Page()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
            serverSources = ServerConfigManager.LoadServerSources();
            ServerSourcesListView.ItemsSource = serverSources;
            DataContext = App.ManagerConfig;
            LoadServerSources();
            config = AppConfig.Load();
            CurrentServerSourceComboBox.ItemsSource = serverSources;
            CurrentServerSourceComboBox.SelectedItem = serverSources.FirstOrDefault(s => s.Url == config.CurrentServerSourceUrl);
            VersionTextBlock.Text = $"Release: {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";
            //this.SettingsViewer.Background.Opacity = 0.9;
            //this.SettingsBG.Opacity = 0.9;
            //this.VersionStackPanel.Background.Opacity = 0.9;

            DataContext = App.ManagerConfig;
            Loaded += async (sender, e) =>
            {

                await InitializeAsync();

            };
        }

        private void LoadServerSources()
        {
            // 调用 ServerConfigManager.LoadServerSources() 来加载服务器源数据
            serverSources = ServerConfigManager.LoadServerSources();



        }

        private async Task InitializeAsync()
        {
            foreach (var item in ServerSourcesListView.Items)
            {
                var container = (ListViewItem)ServerSourcesListView.ContainerFromItem(item);
                //container.Tapped += ServerSourceItem_Click;
            }
        }
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Window aboutWindow = new Window();
            aboutWindow.Content = new AboutPage();
            aboutWindow.Activate();
        }

        private void ServerSourcesListView_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in ServerSourcesListView.Items)
            {
                var container = (ListViewItem)ServerSourcesListView.ContainerFromItem(item);
                if (container != null)
                {
                    container.Tapped -= ServerSourceItem_Click; // 先移除之前可能附加的事件处理程序
                    container.Tapped += ServerSourceItem_Click;
                }
            }
        }


        private async Task<ServerSourceConfig> ShowServerSourceDialogAsync(ServerSourceConfig existingSource = null)
        {
            if (isDialogOpen) return null; // 如果已经有一个对话框打开，则直接返回
            isDialogOpen = true;

            var nameTextBox = new TextBox { PlaceholderText = "输入服务器源名称", Text = existingSource?.Name ?? "" };
            nameTextBox.SetValue(FrameworkElement.NameProperty, "NameTextBox");

            var urlTextBox = new TextBox { PlaceholderText = "输入服务器源地址", Text = existingSource?.Url ?? "" };
            urlTextBox.SetValue(FrameworkElement.NameProperty, "UrlTextBox");

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = existingSource == null ? "添加新服务器源" : "编辑服务器源",
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                Content = new StackPanel
                {
                    Children =
            {
                new TextBlock { Text = "服务器源名称:" },
                nameTextBox,
                new TextBlock { Text = "服务器源地址:" },
                urlTextBox
            }
                }
            };

            var result = await dialog.ShowAsync();
            isDialogOpen = false; // 在对话框关闭后重置布尔变量

            if (result == ContentDialogResult.Primary)
            {
                if (!string.IsNullOrWhiteSpace(nameTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(urlTextBox.Text))
                {
                    return new ServerSourceConfig
                    {
                        Name = nameTextBox.Text,
                        Url = urlTextBox.Text
                    };
                }
            }

            return null;
        }



        private void RefreshServerSourcesListView()
        {
            ServerSourcesListView.ItemsSource = null;
            ServerSourcesListView.ItemsSource = serverSources;
        }

        private void CurrentServerSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSource = (ServerSourceConfig)CurrentServerSourceComboBox.SelectedItem;
            if (selectedSource != null)
            {
                config.CurrentServerSourceUrl = selectedSource.Url;
                config.Save();
            }
        }




        private async void AddServerSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var newSource = await ShowServerSourceDialogAsync();
            if (newSource != null)
            {
                // 添加到服务器源列表
                serverSources.Add(newSource);

                // 保存服务器源配置
                ServerConfigManager.SaveServerSources(serverSources);

                // 刷新服务器源列表视图
                RefreshServerSourcesListView();
            }
        }



        private async void EditServerSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSource = (ServerSourceConfig)((Button)sender).DataContext;
            var updatedSource = await ShowServerSourceDialogAsync(selectedSource);
            if (updatedSource != null)
            {
                // 更新服务器源配置
                var index = serverSources.IndexOf(selectedSource);
                if (index >= 0)
                {
                    serverSources[index] = updatedSource;
                }

                // 保存服务器源配置
                ServerConfigManager.SaveServerSources(serverSources);

                // 刷新服务器源列表视图
                RefreshServerSourcesListView();
            }
        }


        private bool isDialogOpen = false; // 标记是否有对话框正在显示

        private async void DeleteServerSourceButton_Click(object sender, RoutedEventArgs e)
        {
            if (isDialogOpen) return;

            var button = (Button)sender;
            var sourceToRemove = (ServerSourceConfig)button.DataContext;

            if (sourceToRemove.Url == defaultLocalConfigPath)
            {
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "无法删除",
                    Content = "不能删除本地服务器源配置。",
                    CloseButtonText = "确定"
                };

                isDialogOpen = true;
                await dialog.ShowAsync();
                isDialogOpen = false;
            }
            else
            {
                serverSources.Remove(sourceToRemove);
                ServerConfigManager.SaveServerSources(serverSources);
                RefreshServerSourcesListView();
            }
        }

        private ListBox _serverConfigsListBox;

        private async void ServerSourceItem_Click(object sender, TappedRoutedEventArgs e)
        {
            if (isDialogOpen) return;

            var selectedItem = (ServerSourceConfig)((ListViewItem)sender).Content;
            if (selectedItem != null && selectedItem.Url == defaultLocalConfigPath)
            {
                var localServerConfigs = await LoadLocalServerConfigs();

                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "本地服务器配置",
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    Content = new ScrollViewer
                    {
                        Content = new StackPanel
                        {
                            Children =
                    {
                        new TextBlock { Text = "已有服务器配置：" },
                        (_serverConfigsListBox = new ListBox
                        {
                            Name = "ServerConfigsListBox",
                            ItemsSource = localServerConfigs,
                            ItemTemplate = (DataTemplate)this.Resources["ServerConfigTemplate"]
                        }),
                        new AppBarSeparator(),
                        new TextBlock { Text = "新增服务器配置" },
                        new TextBox { PlaceholderText = "输入服务器名称", Name = "NewServerNameTextBox" },
                        new TextBox { PlaceholderText = "输入服务器地址", Name = "NewServerAddressTextBox" },
                        new TextBox { PlaceholderText = "输入端口", Name = "NewServerPortTextBox" }
                    }
                        }
                    }
                };

                dialog.PrimaryButtonClick += async (s, args) =>
                {
                    var newServerNameTextBox = (TextBox)dialog.FindName("NewServerNameTextBox");
                    var newServerAddressTextBox = (TextBox)dialog.FindName("NewServerAddressTextBox");
                    var newServerPortTextBox = (TextBox)dialog.FindName("NewServerPortTextBox");

                    if (!string.IsNullOrWhiteSpace(newServerNameTextBox.Text) &&
                        !string.IsNullOrWhiteSpace(newServerAddressTextBox.Text) &&
                        !string.IsNullOrWhiteSpace(newServerPortTextBox.Text))
                    {
                        var newServerConfig = new ServerConfig
                        {
                            name = newServerNameTextBox.Text,
                            serverAddress = newServerAddressTextBox.Text,
                            newPort = newServerPortTextBox.Text
                        };

                        localServerConfigs.Add(newServerConfig);
                        await UpdateLocalServerConfigs(localServerConfigs);

                        await RefreshServerConfigsList(_serverConfigsListBox);
                    }
                    else
                    {
                        args.Cancel = true;
                    }
                };

                isDialogOpen = true;
                await dialog.ShowAsync();
                isDialogOpen = false;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var stackPanel = (StackPanel)button.Parent;
            var nameTextBox = (TextBox)stackPanel.FindName("NameTextBox");
            var addressTextBox = (TextBox)stackPanel.FindName("AddressTextBox");
            var portTextBox = (TextBox)stackPanel.FindName("PortTextBox");

            nameTextBox.IsReadOnly = false;
            addressTextBox.IsReadOnly = false;
            portTextBox.IsReadOnly = false;

            button.Content = "保存";
            button.Click -= EditButton_Click;
            button.Click += SaveButton_Click;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var stackPanel = (StackPanel)button.Parent;
            var nameTextBox = (TextBox)stackPanel.FindName("NameTextBox");
            var addressTextBox = (TextBox)stackPanel.FindName("AddressTextBox");
            var portTextBox = (TextBox)stackPanel.FindName("PortTextBox");

            var serverConfig = (ServerConfig)stackPanel.DataContext;
            if (serverConfig != null)
            {
                var localServerConfigs = await LoadLocalServerConfigs();
                var configToUpdate = localServerConfigs.FirstOrDefault(config => config.name == serverConfig.name
                                                                                && config.serverAddress == serverConfig.serverAddress
                                                                                && config.newPort == serverConfig.newPort);

                if (configToUpdate != null)
                {
                    configToUpdate.name = nameTextBox.Text;
                    configToUpdate.serverAddress = addressTextBox.Text;
                    configToUpdate.newPort = portTextBox.Text;
                    await UpdateLocalServerConfigs(localServerConfigs);

                    await RefreshServerConfigsList(_serverConfigsListBox);
                }

                button.Content = "编辑";
                button.Click -= SaveButton_Click;
                button.Click += EditButton_Click;

                nameTextBox.IsReadOnly = true;
                addressTextBox.IsReadOnly = true;
                portTextBox.IsReadOnly = true;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var stackPanel = (StackPanel)button.Parent;
            var serverConfig = (ServerConfig)stackPanel.DataContext;
            if (serverConfig != null)
            {
                var localServerConfigs = await LoadLocalServerConfigs();
                var configToRemove = localServerConfigs.FirstOrDefault(config => config.name == serverConfig.name
                                                                                && config.serverAddress == serverConfig.serverAddress
                                                                                && config.newPort == serverConfig.newPort);
                if (configToRemove != null)
                {
                    localServerConfigs.Remove(configToRemove);
                    await UpdateLocalServerConfigs(localServerConfigs);

                    await RefreshServerConfigsList(_serverConfigsListBox);
                }
            }
        }

        private async Task RefreshServerConfigsList(ListBox listBox)
        {
            var localServerConfigs = await LoadLocalServerConfigs();
            listBox.ItemsSource = localServerConfigs;
        }

        private void ChangeInstallButton_ClickAsync(object sender, RoutedEventArgs e)
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
                    InstallPathChanged?.Invoke();
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

        private async void ChangeAkiServerPath_ClickAsync(object sender, RoutedEventArgs e)
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
                    InstallPathChanged?.Invoke();

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


        private async Task<List<ServerConfig>> LoadLocalServerConfigs()
        {
            // 从文件中读取本地源的内容
            var json = await File.ReadAllTextAsync(defaultLocalConfigPath);
            return JsonConvert.DeserializeObject<List<ServerConfig>>(json);
        }

        // 更新本地源的内容
        private async Task UpdateLocalServerConfigs(List<ServerConfig> configs)
        {
            // 将更新后的内容写入文件
            var json = JsonConvert.SerializeObject(configs, Formatting.Indented);
            await File.WriteAllTextAsync(defaultLocalConfigPath, json);
        }

        private async void neko17link_Click(object sender, RoutedEventArgs e)
        {
            // 指定要打开的链接（URL）
            var url = new Uri("https://qm.qq.com/q/ksIdPfxlxC"); // 这里替换成你想打开的链接

            // 使用 Launcher 来启动 URL
            var success = await Launcher.LaunchUriAsync(url);

            // 如果启动失败，可以在这里处理失败情况
            if (!success)
            {
                // 处理失败的情况，比如弹出提示框
                await new MessageDialog("无法打开链接").ShowAsync();
            }
        }


        private async void oddbalink_Click(object sender, RoutedEventArgs e)
        {
            // 指定要打开的链接（URL）
            var url = new Uri("https://sns.oddba.cn/132504.html"); // 这里替换成你想打开的链接

            // 使用 Launcher 来启动 URL
            var success = await Launcher.LaunchUriAsync(url);

            // 如果启动失败，可以在这里处理失败情况
            if (!success)
            {
                // 处理失败的情况，比如弹出提示框
                await new MessageDialog("无法打开链接").ShowAsync();
            }
        }

        private async void afdlink_Click(object sender, RoutedEventArgs e)
        {
            // 指定要打开的链接（URL）
            var url = new Uri("https://afdian.com/a/Neko17-awa"); // 这里替换成你想打开的链接

            // 使用 Launcher 来启动 URL
            var success = await Launcher.LaunchUriAsync(url);

            // 如果启动失败，可以在这里处理失败情况
            if (!success)
            {
                // 处理失败的情况，比如弹出提示框
                await new MessageDialog("无法打开链接").ShowAsync();
            }
        }

        private async void ColorChangeButton_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog colorPickerWindow = new()
            {
                XamlRoot = Content.XamlRoot
            };

            await colorPickerWindow.ShowAsync();

            string pickedColor = colorPickerWindow.SelectedColor;

            if (pickedColor != null)
            {
                App.ManagerConfig.ConsoleFontColorV = pickedColor;
                ManagerConfig.Save();
            }
        }

        private async void ConsoleFamilyFontChange_Click(object sender, RoutedEventArgs e)
        {
            FontFamilyPickerDialog fontFamilyPickerDialog = new()
            {
                XamlRoot = Content.XamlRoot
            };

            await fontFamilyPickerDialog.ShowAsync();

            string pickedFontFamily = fontFamilyPickerDialog.selectedFontFamily;

            if (pickedFontFamily != null)
            {
                App.ManagerConfig.ConsoleFontFamily = pickedFontFamily;
                ManagerConfig.Save();
            }
        }
    }
}
