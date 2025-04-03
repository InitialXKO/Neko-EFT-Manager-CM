using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using Dissonance.Networking;
using System.Management;
using Neko.EFT.Manager.X.Windows;
using Microsoft.UI;
using Windows.UI;
using System.Threading.Tasks;
using System.Text;
using Neko.EFT.Manager.X.Classes;
using static GClass1750;
using System.Threading;
using Microsoft.UI.Dispatching;
using CommunityToolkit.WinUI;
using System.Text.RegularExpressions;
using Microsoft.UI.Text;
using Neko.EFT.Manager.X.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class VntConfigManagementPage : Page
{

    

    // ObservableCollection 用于动态管理配置文件
    public ObservableCollection<VntConfig> VntConfigs { get; set; } = new();

    private bool IsEditing = false; // 是否为编辑模式
    private VntConfig EditingConfig = null; // 当前编辑的配置
                                            // 在类的字段中存储 UI 线程的 DispatcherQueue
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private List<VntConfig> activeVntConfigs = new List<VntConfig>(); // 添加全局列表


    public VntConfigManagementPage()
    {
        this.InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        
        LoadAllVntConfigs();
        this.DataContext = this;
    }



    private void LoadAllVntConfigs()
    {
        var configDirectory = Path.Combine("Configs");

        if (Directory.Exists(configDirectory))
        {
            var configFiles = Directory.GetFiles(configDirectory, "*.yaml");

            foreach (var configFile in configFiles)
            {
                var config = LoadVntConfig(configFile);
                VntConfigs.Add(config);
            }
        }
    }

    private VntConfig LoadVntConfig(string configFilePath)
    {
        var input = File.ReadAllText(configFilePath);
        var deserializer = new DeserializerBuilder()
                            .Build();  // 不使用命名约定

        var config = deserializer.Deserialize<VntConfig>(input);
        return config;
    }


    private void SaveVntConfig(VntConfig config)
    {
        var serializer = new SerializerBuilder()
                            .Build();  // 不使用命名约定

        var yaml = serializer.Serialize(config);

        var directoryPath = Path.Combine("Configs");

        // 如果目录不存在，则创建目录
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var filePath = Path.Combine(directoryPath, $"{config.VntName}.yaml");

        // 如果文件不存在，创建并写入
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, yaml);
        }
        else
        {
            // 如果文件已经存在，可以选择覆盖或进行其他处理
            // 比如覆盖文件:
            File.WriteAllText(filePath, yaml);

            
        }
    }

    private async void ConfigFIKA_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var config = button?.DataContext as VntConfig;

        if (config != null)
        {
            // 使用现有的 ManagerConfig 实例，假设它是一个单例或者已经在其他地方初始化
            var managerConfig = App.ManagerConfig; // 假设你通过资源字典引用了该实例

            if (managerConfig == null)
            {
                ShowMessage("无法找到ManagerConfig实例！");
                return;
            }

            string installPath = managerConfig.InstallPath; // 获取installPath

            Debug.WriteLine($"InstallPath: {installPath}");
            if (string.IsNullOrEmpty(installPath))
            {
                ShowMessage("安装路径无效！");
                return;
            }

            // 拼接文件路径
            string configFilePath = Path.Combine(installPath, "BepInEx", "config", "com.fika.core.cfg");

            // 检查路径有效性
            char[] invalidChars = Path.GetInvalidPathChars();
            if (installPath.Any(c => invalidChars.Contains(c)))
            {
                ShowMessage("安装路径包含无效字符！");
                return;
            }

            if (!File.Exists(configFilePath))
            {
                ShowMessage("配置文件未找到！");
                return;
            }

            try
            {
                // 读取配置文件
                string fileContent = await File.ReadAllTextAsync(configFilePath);

                // 打印文件内容以调试
                Debug.WriteLine($"原始文件内容:\n{fileContent}");

                // 查找 Force IP 行
                string newForceIP = $"Force IP = {config.ip}";

                // 查找 Force IP = 行，并替换整行
                int forceIPIndex = fileContent.IndexOf("Force IP =");

                if (forceIPIndex != -1)
                {
                    // 找到该行的开始位置
                    int startOfLine = fileContent.LastIndexOf("\n", forceIPIndex) + 1; // 行的起始位置
                    int endOfLine = fileContent.IndexOf("\n", forceIPIndex); // 查找该行的结束位置

                    if (endOfLine == -1)
                    {
                        // 如果是文件的最后一行
                        fileContent = fileContent.Substring(0, startOfLine) + newForceIP;
                    }
                    else
                    {
                        // 替换整行
                        fileContent = fileContent.Substring(0, startOfLine) + newForceIP + fileContent.Substring(endOfLine);
                    }
                }
                else
                {
                    // 如果没有 Force IP =，则直接添加新的行
                    fileContent += $"\n{newForceIP}";
                }

                // 保存修改后的文件
                await File.WriteAllTextAsync(configFilePath, fileContent);

                // 提示用户更新成功
                ShowMessage($"Fika配置文件Force IP 已更新为: {config.ip}\n现在你可以通过联机模块进行游戏");
            }
            catch (Exception ex)
            {
                // 输出更详细的错误信息
                ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");

                // 如果有内部异常，也可以输出
                if (ex.InnerException != null)
                {
                    ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
                }
            }
        }
    }



    private async void ConfigHOST_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var config = button?.DataContext as VntConfig;

        if (config != null)
        {
            // 使用现有的 ManagerConfig 实例，假设它是一个单例或者已经在其他地方初始化
            var managerConfig = App.ManagerConfig; // 假设你通过资源字典引用了该实例

            if (managerConfig == null)
            {
                ShowMessage("无法找到ManagerConfig实例！");
                return;
            }

            string installPath = managerConfig.InstallPath; // 获取installPath

            Debug.WriteLine($"InstallPath: {installPath}");
            if (string.IsNullOrEmpty(installPath))
            {
                ShowMessage("安装路径无效！");
                return;
            }

            // 确定实际存在的文件路径
            string sptPath = Path.Combine(installPath, "SPT_Data", "Server", "configs", "http.json");
            string akiPath = Path.Combine(installPath, "aki_Data", "Server", "configs", "http.json");

            string configFilePath = File.Exists(sptPath) ? sptPath : akiPath;

            if (!File.Exists(configFilePath))
            {
                ShowMessage("配置文件未找到！");
                return;
            }

            try
            {
                // 读取配置文件
                string fileContent = await File.ReadAllTextAsync(configFilePath);

                // 解析 JSON 内容
                var jsonConfig = System.Text.Json.JsonDocument.Parse(fileContent);
                var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent);

                if (configDict == null)
                {
                    ShowMessage("配置文件解析失败！");
                    return;
                }

                // 修改配置项
                configDict["ip"] = "0.0.0.0";
                configDict["backendIp"] = config.ip;

                // 序列化回 JSON
                string updatedJson = System.Text.Json.JsonSerializer.Serialize(configDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                // 保存修改后的文件
                await File.WriteAllTextAsync(configFilePath, updatedJson);

                // 显示弹窗并处理用户选择
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "配置已更新",
                    Content = $"http.json 配置已更新:\nbackendIp 修改为: {config.ip}\n点击确定打开服务端管理器，取消返回。",
                    PrimaryButtonText = "确定",
                    SecondaryButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    OpeSPTServerManagerWindow(); // 打开新窗口
                }
            }
            catch (Exception ex)
            {
                // 输出更详细的错误信息
                ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");

                // 如果有内部异常，也可以输出
                if (ex.InnerException != null)
                {
                    ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
                }
            }
        }
    }



    private async void OpeSPTServerManagerWindow()
    {
        SPTServerManager SPTServerManager = new SPTServerManager();
        await Task.Delay(TimeSpan.FromSeconds(0.02));
        SPTServerManager.Activate();
    }




    //private void ShowMessage(string message)
    //{
    //    // 提示消息框
    //    var dialog = new ContentDialog
    //    {
    //        XamlRoot = this.XamlRoot,
    //        Title = "提示",
    //        Content = message,
    //        CloseButtonText = "关闭"
    //    };
    //    dialog.ShowAsync();
    //}


    // 编辑配置文件
    private async void EditVntConfig_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var config = button?.DataContext as VntConfig;

        if (config != null)
        {
            // 设置为编辑模式
            IsEditing = true;
            EditingConfig = config;

            // 初始化对话框数据
            AddVntConfigDialog.Title = "编辑 vnt 配置文件";
            VntNameTextBox.Text = config.VntName;
            DeviceNameTextBox.Text = config.name;
            ServerAddressTextBox.Text = config.server_address;
            TokenTextBox.Text = config.token;
            IPTextBox.Text = config.ip;

            // 禁用 VntNameTextBox 输入
            VntNameTextBox.IsReadOnly = true;

            // 显示对话框
            await AddVntConfigDialog.ShowAsync();
        }
    }




    // 确定添加或编辑配置文件
    // 确定添加或编辑配置文件

    private async void OpenAddVntConfigDialog_Click(object sender, RoutedEventArgs e)
    {
        // 设置为新增模式
        AddVntConfigDialog.IsPrimaryButtonEnabled = false;
        IsEditing = false;
        EditingConfig = null;

        // 初始化对话框数据
        AddVntConfigDialog.Title = "添加新 vnt 配置文件";
        VntNameTextBox.Text = string.Empty;
        DeviceNameTextBox.Text = GetDeviceName();
        ServerAddressTextBox.Text = string.Empty;
        TokenTextBox.Text = string.Empty;
        IPTextBox.Text = string.Empty;

        // 恢复 VntNameTextBox 的可编辑性
        VntNameTextBox.IsReadOnly = false;

        // 显示对话框
        await AddVntConfigDialog.ShowAsync();
    }


    private async void AddVntConfigDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 保存之前进行字段验证

        bool allFieldsValid = ValidateFields();

        if (allFieldsValid)
        {
            if (IsEditing && EditingConfig != null)
            {
                // 编辑模式：根据原始名称找到对应的配置并更新
                var originalConfig = VntConfigs.FirstOrDefault(c => c.VntName == EditingConfig.VntName);
                if (originalConfig != null)
                {
                    originalConfig.VntName = VntNameTextBox.Text;
                    originalConfig.name = DeviceNameTextBox.Text;
                    originalConfig.server_address = ServerAddressTextBox.Text;
                    originalConfig.token = TokenTextBox.Text;
                    originalConfig.ip = IPTextBox.Text;

                    // 保存更新后的配置到文件
                    SaveVntConfig(originalConfig);
                }
            }
            else
            {
                // 新增模式：创建并添加新配置
                
                var newConfig = new VntConfig
                {
                    VntName = VntNameTextBox.Text,
                    name = DeviceNameTextBox.Text,
                    server_address = ServerAddressTextBox.Text,
                    token = TokenTextBox.Text,
                    ip = IPTextBox.Text,
                };

                VntConfigs.Add(newConfig);

                // 保存新增的配置到文件
                SaveVntConfig(newConfig);
            }

            // 更新 UI
            RefreshVntConfigList(EditingConfig ?? VntConfigs.Last());

            // 关闭对话框
            AddVntConfigDialog.Hide();
        }
        else
        {
            // 如果字段验证失败，可以在这里处理错误提示（例如闪烁红框，或者显示消息等）
        }
    }

    private bool ValidateFields()
    {
        bool allFieldsValid = true;

        // 获取所有必填项的输入值
        string vntName = VntNameTextBox.Text;
        string deviceName = DeviceNameTextBox.Text;
        string serverAddress = ServerAddressTextBox.Text;
        string token = TokenTextBox.Text;
        string ip = IPTextBox.Text;

        // 校验是否有任何必填项为空，并标记未填写的字段
        if (string.IsNullOrWhiteSpace(vntName))
        {
            VntNameTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
            allFieldsValid = false;
        }
        else
        {
            VntNameTextBox.BorderBrush = new SolidColorBrush(Colors.Gray);
        }

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            DeviceNameTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
            allFieldsValid = false;
        }
        else
        {
            DeviceNameTextBox.BorderBrush = new SolidColorBrush(Colors.Gray);
        }

        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            ServerAddressTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
            allFieldsValid = false;
        }
        else
        {
            ServerAddressTextBox.BorderBrush = new SolidColorBrush(Colors.Gray);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            TokenTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
            allFieldsValid = false;
        }
        else
        {
            TokenTextBox.BorderBrush = new SolidColorBrush(Colors.Gray);
        }

        if (string.IsNullOrWhiteSpace(ip))
        {
            IPTextBox.BorderBrush = new SolidColorBrush(Colors.Red);
            allFieldsValid = false;
        }
        else
        {
            IPTextBox.BorderBrush = new SolidColorBrush(Colors.Gray);
        }

        return allFieldsValid;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // 每次用户输入时实时检查字段状态
        bool allFieldsValid = ValidateFields();

        // 启用或禁用保存按钮
        AddVntConfigDialog.IsPrimaryButtonEnabled = allFieldsValid;
    }




    // 删除配置文件
    private void DeleteVntConfig_Click(object sender, RoutedEventArgs e)
    {
        var selectedConfig = (sender as Button)?.DataContext as VntConfig;

        if (selectedConfig != null)
        {
            // 删除配置文件
            var filePath = Path.Combine("Configs", $"{selectedConfig.VntName}.yaml");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // 从集合中移除配置项
            VntConfigs.Remove(selectedConfig);
        }
    }





    private void RefreshVntConfigList(VntConfig updatedConfig)
    {
        // 如果是 ObservableCollection，UI 会自动更新，无需手动操作。
        // 如果不是，可以通过以下方式手动更新 UI：
        var index = VntConfigs.IndexOf(updatedConfig);
        if (index >= 0)
        {
            VntConfigs[index] = updatedConfig; // 替换原有项
        }
    }


    private Process vntProcess = null;
    private readonly string vntCliPath = Path.Combine("Assets", "vnt", "vnt-cli.exe");
    private CancellationTokenSource cancellationTokenSource = new();

    private async void ConnectVntConfig_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var vntConfig = button?.DataContext as VntConfig;
        if (vntConfig == null) return;

        if (!vntConfig.IsConnected) // 连接
        {
            // 🔹 **显示连接模式选择对话框**
            string selectedMode = await ShowConnectionModeDialog();
            if (string.IsNullOrEmpty(selectedMode)) return; // 用户取消

            vntConfig.IsConnecting = true; // 按钮禁用
            bool success = false;

            switch (selectedMode)
            {
                case "Host":
                    success = await ConnectToServer(vntConfig);
                    if (success)
                    {
                        await ConfigHostFIKA(vntConfig); // 🔹 **配置主机**
                    }
                    break;

                case "Client":
                    success = await ConnectToServer(vntConfig);
                    
                    break;

                case "MatchHost": // ✅ 新增战局主机模式
                    success = await ConnectToServer(vntConfig);
                    if (success)
                    {
                        await ConfigFIKA(vntConfig); // 🔹 **成功后执行 FIKA 配置**
                    }
                    break;

                case "NetworkOnly":
                    success = await ConnectToServer(vntConfig);
                    break;
            }

            vntConfig.IsConnected = success;
            vntConfig.IsConnecting = false; // 解除禁用
        }
        else // 断开
        {
            await DisConnect(vntConfig);
            vntConfig.IsConnected = false;
        }
    }

    private async Task<string> ShowConnectionModeDialog()
    {
        var dialog = new ContentDialog
        {
            Title = "请选择联机模式",
            XamlRoot = this.XamlRoot,
            CloseButtonText = "取消" // ✅ 仅保留取消按钮
        };

        // 🔹 选项按钮
        var hostButton = new Button { Content = "全托管主机", Width = 250, Margin = new Thickness(5) };
        var clientButton = new Button { Content = "仅客户端", Width = 250, Margin = new Thickness(5) };
        var matchHostButton = new Button { Content = "仅战局主机", Width = 250, Margin = new Thickness(5) };
        var networkOnlyButton = new Button { Content = "仅网络主机", Width = 250, Margin = new Thickness(5) };

        // 🔹 绑定点击事件
        hostButton.Click += (s, e) => { dialog.Tag = "Host"; dialog.Hide(); };
        clientButton.Click += (s, e) => { dialog.Tag = "Client"; dialog.Hide(); };
        matchHostButton.Click += (s, e) => { dialog.Tag = "MatchHost"; dialog.Hide(); };
        networkOnlyButton.Click += (s, e) => { dialog.Tag = "NetworkOnly"; dialog.Hide(); };

        // 🔹 布局
        StackPanel panel = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };
       
        panel.Children.Add(hostButton);
        panel.Children.Add(clientButton);
        panel.Children.Add(matchHostButton);
        panel.Children.Add(networkOnlyButton);

        dialog.Content = panel;

        // 🔹 显示对话框
        var result = await dialog.ShowAsync();

        return dialog.Tag?.ToString(); // ✅ 用户选择返回值，取消返回 null
    }

    private async void ShowMessageFika(string message)
    {
        // 使用 ContentDialog 显示简单消息
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "提示",
            Content = message,
            CloseButtonText = "确定"
        };

        await dialog.ShowAsync();
    }
    private async Task ConfigFIKA(VntConfig config)
    {
        if (config == null) return;

        // 获取 ManagerConfig 实例
        var managerConfig = App.ManagerConfig; // 假设你通过资源字典引用了该实例
        if (managerConfig == null)
        {
            ShowMessageFika("无法找到 ManagerConfig 实例！");
            return;
        }

        string installPath = managerConfig.InstallPath; // 获取安装路径
        Debug.WriteLine($"InstallPath: {installPath}");

        if (string.IsNullOrEmpty(installPath))
        {
            ShowMessageFika("安装路径无效！");
            return;
        }

        // 拼接 FIKA 配置文件路径
        string configFilePath = Path.Combine(installPath, "BepInEx", "config", "com.fika.core.cfg");

        // 检查路径是否有效
        char[] invalidChars = Path.GetInvalidPathChars();
        if (installPath.Any(c => invalidChars.Contains(c)))
        {
            ShowMessageFika("安装路径包含无效字符！");
            return;
        }

        if (!File.Exists(configFilePath))
        {
            ShowMessageFika("FIKA 配置文件未找到！");
            return;
        }

        try
        {
            // 读取配置文件
            string fileContent = await File.ReadAllTextAsync(configFilePath);
            Debug.WriteLine($"原始文件内容:\n{fileContent}");

            // 修改 "Force IP" 选项
            string newForceIP = $"Force IP = {config.ip}";
            int forceIPIndex = fileContent.IndexOf("Force IP =");

            if (forceIPIndex != -1)
            {
                // 替换 "Force IP" 这一行
                int startOfLine = fileContent.LastIndexOf("\n", forceIPIndex) + 1;
                int endOfLine = fileContent.IndexOf("\n", forceIPIndex);

                if (endOfLine == -1)
                {
                    fileContent = fileContent.Substring(0, startOfLine) + newForceIP;
                }
                else
                {
                    fileContent = fileContent.Substring(0, startOfLine) + newForceIP + fileContent.Substring(endOfLine);
                }
            }
            else
            {
                // 如果文件中没有 "Force IP"，则添加
                fileContent += $"\n{newForceIP}";
            }

            // 保存修改后的文件
            await File.WriteAllTextAsync(configFilePath, fileContent);

            // 显示成功消息
            ShowMessage($"Fika 配置文件 Force IP 已更新为: {config.ip}\n现在你可以通过联机模块进行游戏");
        }
        catch (Exception ex)
        {
            ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
            }
        }
    }

    private async Task ConfigHost(VntConfig config)
    {
        if (config == null) return;

        var managerConfig = App.ManagerConfig; // 假设已初始化
        if (managerConfig == null)
        {
            ShowMessage("无法找到ManagerConfig实例！");
            return;
        }

        string installPath = managerConfig.InstallPath;
        if (string.IsNullOrEmpty(installPath))
        {
            ShowMessage("安装路径无效！");
            return;
        }

        string sptPath = Path.Combine(installPath, "SPT_Data", "Server", "configs", "http.json");
        string akiPath = Path.Combine(installPath, "aki_Data", "Server", "configs", "http.json");
        string configFilePath = File.Exists(sptPath) ? sptPath : akiPath;

        if (!File.Exists(configFilePath))
        {
            ShowMessage("配置文件未找到！");
            return;
        }

        try
        {
            // 读取 JSON
            string fileContent = await File.ReadAllTextAsync(configFilePath);
            var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent);
            if (configDict == null)
            {
                ShowMessage("配置文件解析失败！");
                return;
            }

            // 修改配置项
            configDict["ip"] = "0.0.0.0";
            configDict["backendIp"] = config.ip;

            string updatedJson = System.Text.Json.JsonSerializer.Serialize(configDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            // 写回文件
            await File.WriteAllTextAsync(configFilePath, updatedJson);

            // 弹窗提示
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "配置已更新",
                Content = $"http.json 配置已更新:\nbackendIp 修改为: {config.ip}\n点击确定打开服务端管理器，取消返回。",
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                OpeSPTServerManagerWindow(); // 打开服务端管理器
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
            }
        }
    }


    private async Task ConfigHostFIKA(VntConfig config)
    {
        if (config == null) return;

        var managerConfig = App.ManagerConfig; // 获取 ManagerConfig 实例
        if (managerConfig == null)
        {
            await ShowMessage("❌ 无法找到 ManagerConfig 实例！");
            return;
        }

        string installPath = managerConfig.InstallPath; // 获取安装路径
        Debug.WriteLine($"InstallPath: {installPath}");

        if (string.IsNullOrEmpty(installPath))
        {
            await ShowMessage("❌ 安装路径无效！");
            return;
        }

        // 🔹 记录所有日志信息，最后一次性显示
        StringBuilder logBuilder = new StringBuilder();

        // =======================
        // 1️⃣ 配置 FIKA
        // =======================
        string fikaConfigPath = Path.Combine(installPath, "BepInEx", "config", "com.fika.core.cfg");

        if (!File.Exists(fikaConfigPath))
        {
            logBuilder.AppendLine("⚠️ FIKA 配置文件未找到！");
        }
        else
        {
            try
            {
                string fikaConfig = await File.ReadAllTextAsync(fikaConfigPath);
                Debug.WriteLine($"原始 FIKA 配置:\n{fikaConfig}");

                // 修改 "Force IP"
                string newForceIP = $"Force IP = {config.ip}";
                int forceIPIndex = fikaConfig.IndexOf("Force IP =");

                if (forceIPIndex != -1)
                {
                    // 替换 "Force IP" 这一行
                    int startOfLine = fikaConfig.LastIndexOf("\n", forceIPIndex) + 1;
                    int endOfLine = fikaConfig.IndexOf("\n", forceIPIndex);

                    fikaConfig = (endOfLine == -1)
                        ? fikaConfig.Substring(0, startOfLine) + newForceIP
                        : fikaConfig.Substring(0, startOfLine) + newForceIP + fikaConfig.Substring(endOfLine);
                }
                else
                {
                    // 添加新行
                    fikaConfig += $"\n{newForceIP}";
                }

                await File.WriteAllTextAsync(fikaConfigPath, fikaConfig);
                logBuilder.AppendLine($"✅ FIKA 配置更新成功！Force IP = {config.ip}");
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine($"❌ 配置 FIKA 失败: {ex.Message}");
            }
        }

        // =======================
        // 2️⃣ 配置 Host (http.json)
        // =======================
        string sptPath = Path.Combine(installPath, "SPT_Data", "Server", "configs", "http.json");
        string akiPath = Path.Combine(installPath, "aki_Data", "Server", "configs", "http.json");
        string hostConfigPath = File.Exists(sptPath) ? sptPath : akiPath;

        if (!File.Exists(hostConfigPath))
        {
            logBuilder.AppendLine("⚠️ 服务器配置文件 http.json 未找到！");
        }
        else
        {
            try
            {
                string hostConfig = await File.ReadAllTextAsync(hostConfigPath);
                var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(hostConfig);

                if (configDict != null)
                {
                    configDict["ip"] = "0.0.0.0";
                    configDict["backendIp"] = config.ip;

                    string updatedJson = System.Text.Json.JsonSerializer.Serialize(configDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(hostConfigPath, updatedJson);

                    logBuilder.AppendLine($"✅ 服务器 http.json 配置更新成功！backendIp = {config.ip}");
                }
                else
                {
                    logBuilder.AppendLine("❌ 服务器配置文件解析失败！");
                }
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine($"❌ 配置服务器失败: {ex.Message}");
            }
        }

        // =======================
        // 3️⃣ 显示最终结果
        // =======================
        await ShowMessage(logBuilder.ToString());

        // 🔹 询问是否打开服务端管理器
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "配置完成",
            Content = "服务器与 FIKA 配置已完成。\n是否打开服务端管理器？",
            PrimaryButtonText = "确定",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            OpeSPTServerManagerWindow(); // 打开服务端管理器
        }
    }


    private async 

    Task
ShowMessage(string message)
    {
        // 使用 ContentDialog 显示简单消息
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "提示",
            Content = message,
            CloseButtonText = "确定"
        };

        await dialog.ShowAsync();
    }

    private async Task<bool> ConnectAsHost(VntConfig vntConfig)
    {
        Debug.WriteLine("🔹 作为【主机】连接");

        
        return await ConnectToServer(vntConfig); // 目前执行相同的逻辑
    }

    private async Task<bool> ConnectAsClient(VntConfig vntConfig)
    {
        Debug.WriteLine("🔹 作为【客户端】连接");
        return await ConnectToServer(vntConfig); // 目前执行相同的逻辑
    }

    private async Task<bool> ConnectAsNetworkOnly(VntConfig vntConfig)
    {
        Debug.WriteLine("🔹 作为【仅连接组网】模式连接");
        return await ConnectToServer(vntConfig); // 目前执行相同的逻辑
    }

    




    private async Task<bool> ConnectToServer(VntConfig vntConfig)
    {
        cancellationTokenSource = new CancellationTokenSource();
        string configFilePath = Path.Combine("Configs", $"{vntConfig.VntName}.yaml");

        if (!File.Exists(configFilePath) || !File.Exists(vntCliPath))
        {
            Debug.WriteLine("配置文件或 vnt-cli.exe 不存在");
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = vntCliPath,
            Arguments = $"-c -f \"{configFilePath}\"",
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true,
        };

        try
        {
            vntProcess = Process.Start(startInfo);
            if (vntProcess != null)
            {
                vntProcess.EnableRaisingEvents = true;
                vntProcess.Exited += (sender, args) =>
                {
                    _dispatcherQueue?.TryEnqueue(() =>
                    {
                        vntConfig.IsConnected = false;
                        SaveVntConfig(vntConfig); // ✅ 进程退出时也保存状态
                    });
                };

                await Task.Delay(3000);
                string status = await GetDeviceList();
                bool connected = !status.Contains("连接尚未就绪");

                vntConfig.IsConnected = connected;
                vntConfig.IsConnecting = false; // ✅ 解除按钮禁用
                SaveVntConfig(vntConfig); // ✅ 连接成功后保存

                if (connected)
                {
                    StartPeriodicStatusUpdate(vntConfig);
                }

                return connected;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error starting vnt-cli.exe: {ex.Message}");
        }

        return false;
    }




    private async void DisConnect_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var vntConfig = button?.DataContext as VntConfig;

        if (vntConfig != null)
        {
            await DisConnect(vntConfig);
            vntConfig.ConnectionStatus = "已断开";
            SaveVntConfig(vntConfig);
        }
    }

    private async Task DisConnect(VntConfig vntConfig)
    {
        try
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
            }

            if (!File.Exists(vntCliPath))
            {
                Debug.WriteLine($"vnt-cli.exe not found at: {vntCliPath}");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = vntCliPath,
                Arguments = "--stop",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                Debug.WriteLine($"Output: {output}");
                if (!string.IsNullOrEmpty(error)) Debug.WriteLine($"Error: {error}");
            }

            vntConfig.IsConnected = false;
            vntConfig.ConnectionStatus = "已断开";
            SaveVntConfig(vntConfig); // ✅ 断开后保存
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("🔴 任务已取消，安全退出");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error stopping vnt-cli.exe: {ex.Message}");
        }
    }




    //private async void DeviceList_Click(object sender, RoutedEventArgs e)
    //{
    //    var button = sender as Button;
    //    var vntConfig = button?.DataContext as VntConfig;
    //    if (vntConfig != null)
    //    {
    //        await UpdateConnectionStatus(vntConfig);
    //    }
    //}





    private async Task<string> GetDeviceList()
    {
        if (!File.Exists(vntCliPath)) return "vnt-cli.exe不存在";

        var startInfo = new ProcessStartInfo
        {
            FileName = vntCliPath,
            Arguments = "--list",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(error)) Debug.WriteLine($"Error: {error}");
                if (output.Contains("ConnectionReset")) return "连接尚未就绪";
                return output;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error starting vnt-cli.exe: {ex.Message}");
        }
        return "获取设备列表失败";
    }



    private async void DeviceList_Click(object sender, RoutedEventArgs e)
    {

        await GetDeviceListShow();


    }

    private async Task GetDeviceListShow()
    {
        // 获取 vnt-cli.exe 的路径
        string vntCliPath = Path.Combine("Assets", "vnt", "vnt-cli.exe");

        if (File.Exists(vntCliPath))
        {
            // 启动 vnt-cli.exe 进程来获取设备列表
            var startInfo = new ProcessStartInfo
            {
                FileName = vntCliPath,                // 使用 vnt-cli.exe 的正确路径
                Arguments = "--list",                  // 获取设备列表
                UseShellExecute = false,              // 不使用外壳执行
                RedirectStandardOutput = true,        // 重定向标准输出
                RedirectStandardError = true,         // 重定向标准错误
                CreateNoWindow = true,                // 不显示命令行窗口
                StandardOutputEncoding = Encoding.UTF8,  // 强制使用 UTF-8 编码
                StandardErrorEncoding = Encoding.UTF8   // 强制使用 UTF-8 编码
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        // 获取命令的输出并处理设备列表
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.WriteLine($"Error: {error}");
                        }
                        else
                        {
                            // 如果输出包含 ConnectionReset 错误，替换为“连接尚未就绪”
                            if (output.Contains("ConnectionReset"))
                            {
                                output = "连接尚未就绪";
                            }

                            Debug.WriteLine($"Device List: {output}");

                            // 显示设备列表在弹窗中
                            await ShowDeviceListDialog(output);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting vnt-cli.exe: {ex.Message}");
            }
        }
        else
        {
            Debug.WriteLine($"vnt-cli.exe not found at: {vntCliPath}");
        }
    }

    //private async Task ShowDeviceListDialog(string rawOutput)
    //{
    //    var devices = ParseDeviceList(rawOutput);

    //    var listView = new ListView
    //    {
    //        XamlRoot = this.XamlRoot,
    //        SelectionMode = ListViewSelectionMode.None,
    //        MinHeight = 400, // 最小高度，保证内容能显示
    //        MinWidth = 900   // 最小宽度，适配更大列表
    //    };

    //    // 🔹 创建列标题
    //    var headerPanel = new Grid();
    //    headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
    //    headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
    //    headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
    //    headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
    //    headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

    //    headerPanel.Children.Add(new TextBlock { Text = "设备名称", FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
    //    Grid.SetColumn((FrameworkElement)headerPanel.Children[^1], 0);

    //    headerPanel.Children.Add(new TextBlock { Text = "虚拟 IP", FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
    //    Grid.SetColumn((FrameworkElement)headerPanel.Children[^1], 1);

    //    headerPanel.Children.Add(new TextBlock { Text = "状态", FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
    //    Grid.SetColumn((FrameworkElement)headerPanel.Children[^1], 2);

    //    headerPanel.Children.Add(new TextBlock { Text = "连接类型", FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
    //    Grid.SetColumn((FrameworkElement)headerPanel.Children[^1], 3);

    //    headerPanel.Children.Add(new TextBlock { Text = "RTT", FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
    //    Grid.SetColumn((FrameworkElement)headerPanel.Children[^1], 4);

    //    // 🔹 添加设备数据
    //    foreach (var device in devices)
    //    {
    //        var grid = new Grid();
    //        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
    //        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
    //        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
    //        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
    //        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

    //        grid.Children.Add(new TextBlock { Text = device.Name, TextAlignment = TextAlignment.Center });
    //        Grid.SetColumn((FrameworkElement)grid.Children[^1], 0);

    //        grid.Children.Add(new TextBlock { Text = device.VirtualIp, TextAlignment = TextAlignment.Center });
    //        Grid.SetColumn((FrameworkElement)grid.Children[^1], 1);

    //        grid.Children.Add(new TextBlock { Text = device.Status, TextAlignment = TextAlignment.Center });
    //        Grid.SetColumn((FrameworkElement)grid.Children[^1], 2);

    //        grid.Children.Add(new TextBlock { Text = device.ConnectionType, TextAlignment = TextAlignment.Center });
    //        Grid.SetColumn((FrameworkElement)grid.Children[^1], 3);

    //        grid.Children.Add(new TextBlock { Text = device.RTT, TextAlignment = TextAlignment.Center });
    //        Grid.SetColumn((FrameworkElement)grid.Children[^1], 4);

    //        listView.Items.Add(grid);
    //    }

    //    // 🔹 让列表可以滚动
    //    var scrollViewer = new ScrollViewer
    //    {
    //        Content = listView,
    //        MinHeight = 400,  // 保持足够的显示区域
    //        MinWidth = 800,   // 让设备信息完整显示
    //        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
    //        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    //    };

    //    // 🔹 创建主布局
    //    var mainPanel = new StackPanel();
    //    mainPanel.Children.Add(headerPanel); // 添加列标题
    //    mainPanel.Children.Add(scrollViewer); // 可滚动列表

    //    var dialog = new ContentDialog
    //    {
    //        XamlRoot = this.XamlRoot,
    //        Title = "设备列表",
    //        Content = mainPanel,  // 保持内容完整
    //        PrimaryButtonText = "关闭",
    //        MinWidth = 820,  // 让对话框更大
    //        MinHeight = 500
    //    };

    //    await dialog.ShowAsync();
    //}


    private async Task ShowDeviceListDialog(string rawOutput)
    {
        var devices = ParseDeviceList(rawOutput);

        var dialog = new DeviceListDialog(devices)
        {
            XamlRoot = this.XamlRoot  // ✅ 确保 XamlRoot 设置正确
        };

        await dialog.ShowAsync();
    }







    private List<DeviceInfo> ParseDeviceList(string rawOutput)
    {
        var devices = new List<DeviceInfo>();
        var lines = rawOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1))  // 跳过第一行表头
        {
            var columns = Regex.Split(line.Trim(), @"\s{2,}");  // 以两个及以上空格分割

            if (columns.Length >= 2)
            {
                devices.Add(new DeviceInfo
                {
                    Name = columns[0],
                    VirtualIp = columns.Length > 1 ? columns[1] : "N/A",
                    Status = columns.Length > 2 ? columns[2] : "Unknown",
                    ConnectionType = columns.Length > 3 ? columns[3] : "N/A",
                    RTT = columns.Length > 4 ? columns[4] : "N/A"
                });
            }
        }
        return devices;
    }

    // 设备信息类
    public class DeviceInfo
    {
        public string Name { get; set; }
        public string VirtualIp { get; set; }
        public string Status { get; set; }
        public string ConnectionType { get; set; }
        public string RTT { get; set; }
    }


    private async Task UpdateConnectionStatus(VntConfig vntConfig)
    {
        string deviceList = await GetDeviceList();

        await DispatcherQueue.GetForCurrentThread().EnqueueAsync(() =>
        {
            vntConfig.IsConnected = !deviceList.Contains("连接尚未就绪");
            vntConfig.ConnectionStatus = vntConfig.IsConnected ? "已连接" : "未连接";
            SaveVntConfig(vntConfig); // ✅ 状态更新后保存
        });
    }



    private async void StartPeriodicStatusUpdate(VntConfig vntConfig)
    {
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                string deviceList = await GetDeviceList();

                await DispatcherQueue.GetForCurrentThread().EnqueueAsync(() =>
                {
                    bool wasConnected = vntConfig.IsConnected;
                    vntConfig.IsConnected = !deviceList.Contains("连接尚未就绪");

                    if (wasConnected != vntConfig.IsConnected)
                    {
                        Debug.WriteLine($"连接状态变化: {vntConfig.IsConnected}");
                    }
                });

                // **等待时，检查取消标志**
                await Task.Delay(10000, cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("🔴 定时任务被取消，停止状态更新！");
        }
    }




    private async Task ShowErrorDialog(string message)
    {
        // 创建并显示一个提示框
        ContentDialog errorDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "错误",
            Content = message,
            PrimaryButtonText = "确定"
        };

        // 显示弹窗
        await errorDialog.ShowAsync();
    }



    


    public string GetDeviceName()
    {
        return Environment.MachineName;
    }

    public string GetDeviceId()
    {
        string deviceId = string.Empty;
        try
        {
            // 获取物理磁盘的序列号作为设备 ID
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            foreach (ManagementObject disk in searcher.Get())
            {
                deviceId = disk["SerialNumber"].ToString();
                if (!string.IsNullOrEmpty(deviceId))
                    break; // 获取到序列号后可以退出
            }
        }
        catch (Exception ex)
        {
            // 处理异常
            deviceId = Guid.NewGuid().ToString(); // 若出错使用 GUID
        }

        return deviceId;
    }


}


public class VntConfig : INotifyPropertyChanged
{


    private bool _isConnected;
    private bool _isConnecting;

    private string mode; // ✅ 新增 Mode 属性

    public string Mode // ✅ 连接模式（Host/Client/NetworkOnly）
    {
        get => mode;
        set
        {
            if (mode != value)
            {
                mode = value;
                OnPropertyChanged(nameof(Mode));
            }
        }
    }
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            if (_isConnecting != value)
            {
                _isConnecting = value;
                OnPropertyChanged(nameof(IsConnecting));
            }
        }
    }

    public string GetDeviceId()
    {
        string deviceId = string.Empty;
        try
        {
            // 获取物理磁盘的序列号作为设备 ID
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            foreach (ManagementObject disk in searcher.Get())
            {
                deviceId = disk["SerialNumber"].ToString();
                if (!string.IsNullOrEmpty(deviceId))
                    break; // 获取到序列号后可以退出
            }
        }
        catch (Exception ex)
        {
            // 处理异常
            deviceId = Guid.NewGuid().ToString(); // 若出错使用 GUID
        }

        return deviceId;
    }
    // VntName 默认为空字符串
    private string _vntName = string.Empty;
    public string VntName
    {
        get => _vntName;
        set
        {
            if (_vntName != value)
            {
                _vntName = value;
                OnPropertyChanged(nameof(VntName));
            }
        }
    }

    // ConnectionStatus 默认为空字符串
    private string _connectionStatus = string.Empty;
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            if (_connectionStatus != value)
            {
                _connectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }
    }

    // tap 默认为 false
    private bool _tap = false;
    public bool tap
    {
        get => _tap;
        set
        {
            if (_tap != value)
            {
                _tap = value;
                OnPropertyChanged(nameof(tap));
            }
        }
    }

    // token 默认为空字符串
    private string _token = string.Empty;
    public string token
    {
        get => _token;
        set
        {
            if (_token != value)
            {
                _token = value;
                OnPropertyChanged(nameof(token));
            }
        }
    }

    // device_id 默认为空字符串
    private string _deviceId = string.Empty;

    public string device_id
    {
        get
        {
            // 自动获取设备 ID
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = GetDeviceId();
            }
            return _deviceId;
        }
        set
        {
            if (_deviceId != value)
            {
                _deviceId = value;
                OnPropertyChanged(nameof(device_id));
            }
        }
    }

    // name 默认为空字符串
    private string _deviceName = string.Empty;

    public string name
    {
        get
        {
            // 自动获取设备名称
            if (string.IsNullOrEmpty(_deviceName))
            {
                _deviceName = GetDeviceName();
            }
            return _deviceName;
        }
        set
        {
            if (_deviceName != value)
            {
                _deviceName = value;
                OnPropertyChanged(nameof(name));
            }
        }
    }

    // 获取设备名称
    private string GetDeviceName()
    {
        // 使用 Environment.MachineName 获取本机的设备名称
        return Environment.MachineName;
    }

    // server_address 默认为空字符串
    private string _serverAddress = string.Empty;
    public string server_address
    {
        get => _serverAddress;
        set
        {
            if (_serverAddress != value)
            {
                _serverAddress = value;
                OnPropertyChanged(nameof(server_address));
            }
        }
    }

    // stun_server 默认为空列表
    private List<string> _stunServer = new List<string>
    {
        "stun1.l.google.com:19302",
        "stun2.l.google.com:19302",
        "stun.miwifi.com",
        "stun.chat.bilibili.com",
        "stun.hitv.com",
        "stun.cdnbye.com"
    };
    public List<string> stun_server
    {
        get => _stunServer;
        set
        {
            if (_stunServer != value)
            {
                _stunServer = value;
                OnPropertyChanged(nameof(stun_server));
            }
        }
    }

    // mtu 默认为 1420
    private int _mtu = 1420;
    public int mtu
    {
        get => _mtu;
        set
        {
            if (_mtu != value)
            {
                _mtu = value;
                OnPropertyChanged(nameof(mtu));
            }
        }
    }

    // tcp 默认为 false
    private bool _tcp = false;
    public bool tcp
    {
        get => _tcp;
        set
        {
            if (_tcp != value)
            {
                _tcp = value;
                OnPropertyChanged(nameof(tcp));
            }
        }
    }

    // ip 默认为空字符串
    private string _ip = string.Empty;
    public string ip
    {
        get => _ip;
        set
        {
            if (_ip != value)
            {
                _ip = value;
                OnPropertyChanged(nameof(ip));
            }
        }
    }

    // server_encrypt 默认为 false
    private bool _serverEncrypt = false;
    public bool server_encrypt
    {
        get => _serverEncrypt;
        set
        {
            if (_serverEncrypt != value)
            {
                _serverEncrypt = value;
                OnPropertyChanged(nameof(server_encrypt));
            }
        }
    }

    // cipher_model 默认为空字符串
    //private string _cipherModel = string.Empty;
    //public string cipher_model
    //{
    //    get => _cipherModel;
    //    set
    //    {
    //        if (_cipherModel != value)
    //        {
    //            _cipherModel = value;
    //            OnPropertyChanged(nameof(cipher_model));
    //        }
    //    }
    //}

    // punch_model 默认为 "all"
    private string _punchModel = "all";
    public string punch_model
    {
        get => _punchModel;
        set
        {
            if (_punchModel != value)
            {
                _punchModel = value;
                OnPropertyChanged(nameof(punch_model));
            }
        }
    }

    // use_channel 默认为 "all"
    private string _useChannel = "all";
    public string use_channel
    {
        get => _useChannel;
        set
        {
            if (_useChannel != value)
            {
                _useChannel = value;
                OnPropertyChanged(nameof(use_channel));
            }
        }
    }

    // cmd 默认为 false
    private bool _cmd = false;
    public bool cmd
    {
        get => _cmd;
        set
        {
            if (_cmd != value)
            {
                _cmd = value;
                OnPropertyChanged(nameof(cmd));
            }
        }
    }

    // 实现 INotifyPropertyChanged 接口
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}





