using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Neko.EFT.Manager.X.Classes;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Neko.EFT.Manager.X.Pages;

public sealed partial class GameVersionLibraryPage : Page
{
    public static readonly string BaseDirectory = AppContext.BaseDirectory;
    public static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "UserData", "Config");
    public static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "GameVersionsConfig.json");


    public ObservableCollection<GameVersionLb> GameVersionList { get; set; } = new();

    private GameVersionLb? _editingVersion;
    private static readonly string GameVersionConfigFile = ConfigFilePath;
    public GameVersionLibraryPage()
    {
        this.InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
        _ = LoadGameVersionsAsync();
        
    }

    private async Task LoadGameVersionsAsync()
    {
        try
        {
            // 检查文件是否存在，若不存在则初始化一个空文件
            if (File.Exists(GameVersionConfigFile))
            {
                var json = await File.ReadAllTextAsync(GameVersionConfigFile);
                var versions = JsonSerializer.Deserialize<List<GameVersionLb>>(json);
                if (versions != null)
                {
                    GameVersionList.Clear();
                    foreach (var version in versions)
                    {
                        GameVersionList.Add(version);
                    }
                }
            }
            else
            {
                // 如果文件不存在，初始化一个空的版本库并创建配置文件
                var emptyVersionList = new List<GameVersionLb>();
                var json = JsonSerializer.Serialize(emptyVersionList);
                await File.WriteAllTextAsync(GameVersionConfigFile, json);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync("加载版本信息失败", ex.Message);
        }
    }


    private async Task SaveGameVersionsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(GameVersionList);
            await File.WriteAllTextAsync(GameVersionConfigFile, json);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync("保存版本信息失败", ex.Message);
        }
    }

    private async void OpenAddGameVersionDialog_Click(object sender, RoutedEventArgs e)
    {
        // 重置弹窗内容
        _editingVersion = null;
        GameNameTextBox.Text = string.Empty;
        ClientPathTextBlock.Text = "未选择客户端路径";
        ServerPathTextBlock.Text = "未选择服务端路径";

        await AddGameVersionDialog.ShowAsync();
    }

    private async void SelectClientPath_Click(object sender, RoutedEventArgs e)
    {
        string? selectedPath = await OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            ClientPathTextBlock.Text = selectedPath;
        }
    }

    private async void SelectServerPath_Click(object sender, RoutedEventArgs e)
    {
        string? selectedPath = await OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(selectedPath))
        {
            ServerPathTextBlock.Text = selectedPath;
        }
    }

    private async void EditGameVersion_Click(object sender, RoutedEventArgs e)
    {
        var version = (sender as FrameworkElement)?.DataContext as GameVersionLb;
        if (version != null)
        {
            _editingVersion = version;
            GameNameTextBox.Text = version.GameName;
            ClientPathTextBlock.Text = version.GamePath;
            ServerPathTextBlock.Text = version.GameServerPath;

            await AddGameVersionDialog.ShowAsync();
        }
    }

    private async void DeleteGameVersion_Click(object sender, RoutedEventArgs e)
    {
        // 获取当前选中的版本
        var version = (sender as FrameworkElement)?.DataContext as GameVersionLb;
        if (version != null)
        {
            // 创建确认删除的 ContentDialog
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "删除游戏版本",
                Content = $"您确定要删除游戏版本： {version.GameName} 吗？",
                PrimaryButtonText = "删除",
                SecondaryButtonText = "取消"
            };

            // 显示对话框并等待用户响应
            var result = await dialog.ShowAsync();

            // 如果用户点击了删除按钮
            if (result == ContentDialogResult.Primary)
            {
                // 删除选中的版本
                GameVersionList.Remove(version);
                // 保存更新后的版本列表
                await SaveGameVersionsAsync();
            }
        }
    }


    private async void LaunchGameVersion_Click(object sender, RoutedEventArgs e)
    {
        var version = (sender as FrameworkElement)?.DataContext as GameVersionLb;
        if (version != null)
        {
            ContentDialog launchDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "启动游戏",
                Content = $"正在启动 {version.GameName}...",
                CloseButtonText = "确定"
                
            };
            await launchDialog.ShowAsync();
        }
    }

    private async void AddGameVersionDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 添加或更新版本信息
        if (string.IsNullOrWhiteSpace(GameNameTextBox.Text) ||
            ClientPathTextBlock.Text == "未选择客户端路径" ||
            ServerPathTextBlock.Text == "未选择服务端路径")
        {
            args.Cancel = true; // 防止关闭对话框
            return;
        }

        if (_editingVersion != null)
        {
            // 编辑现有版本
            _editingVersion.GameName = GameNameTextBox.Text;
            _editingVersion.GamePath = ClientPathTextBlock.Text;
            _editingVersion.GameServerPath = ServerPathTextBlock.Text;
        }
        else
        {
            // 添加新版本
            GameVersionList.Add(new GameVersionLb(
    GameNameTextBox.Text,
    ClientPathTextBlock.Text,
    ServerPathTextBlock.Text
));
        }

        await SaveGameVersionsAsync();
    }

    // 使用 VisualTreeHelper 遍历视觉树，查找父级 Expander
    private Expander FindParentExpander(DependencyObject current)
    {
        while (current != null)
        {
            // 如果找到 Expander，则返回
            if (current is Expander expander)
            {
                return expander;
            }
            // 向上查找父元素
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    // 遍历 StackPanel 中查找目标 TextBox
    private TextBlock FindTextBlockInStackPanel(DependencyObject parent, string name)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock textBlock && textBlock.Name == name)
            {
                return textBlock;
            }
            // 如果子控件是容器，递归查找
            var result = FindTextBlockInStackPanel(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void SwitchConfig_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        // 通过 VisualTreeHelper 查找父级 Expander
        var expander = FindParentExpander(button);
        if (expander == null)
        {
            Debug.WriteLine("Expander not found through visual tree.");
            return;
        }

        // 获取 Expander 的 DataContext
        var selectedVersion = expander.DataContext as GameVersionLb;
        if (selectedVersion == null)
        {
            Debug.WriteLine("DataContext is not GameVersionLb.");
            return;
        }

        // 遍历所有版本，更新启用状态
        foreach (var version in GameVersionList)
        {
            version.IsEnabled = false;
        }

        // 启用当前版本
        selectedVersion.IsEnabled = true;

        // 获取路径值
        string selectedGamePath = selectedVersion.GamePath;
        string selectedServerPath = selectedVersion.GameServerPath;

        if (string.IsNullOrEmpty(selectedGamePath) || string.IsNullOrEmpty(selectedServerPath))
        {
            Debug.WriteLine("Paths are empty.");
            return;
        }

        SaveGameVersionsAsync();
        // 设置全局配置文件路径
        UpdateConfigPath(selectedGamePath, selectedServerPath);

        // 更新UI（如果需要）
        RefreshUI();
    }


    private void RefreshUI()
    {
        var mainWindow = App.MainWindow;

        mainWindow.ContentFrame.Navigate(typeof(GameVersionLibraryPage));
    }



    private void UpdateConfigPath(string selectedGamePath, string selectedServerPath)
    {
        // 加载配置
        ManagerConfig.Load();

        // 更新配置中的路径
        App.ManagerConfig.InstallPath = selectedGamePath;
        App.ManagerConfig.AkiServerPath = selectedServerPath;

        // 保存更新后的配置
        ManagerConfig.Save();
    }





    private async Task<string?> OpenFolderPickerAsync()
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定"
        };
        await dialog.ShowAsync();
    }
}

public partial class GameVersionLb : INotifyPropertyChanged
{
    public string GameName { get; set; }
    public string GamePath { get; set; }
    public string GameServerPath { get; set; }

    private string _clientVersion;
    public string ClientVersion
    {
        get => _clientVersion;
        set
        {
            _clientVersion = value;
            OnPropertyChanged(nameof(ClientVersion));
        }
    }

    private string _serverVersion;
    internal string gamePath;
    internal string gameServerPath;

    public string ServerVersion
    {
        get => _serverVersion;
        set
        {
            _serverVersion = value;
            OnPropertyChanged(nameof(ServerVersion));
        }
    }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    // 构造函数
    public GameVersionLb(string gameName, string gamePath, string gameServerPath)
    {
        GameName = gameName;
        GamePath = gamePath;
        GameServerPath = gameServerPath;

        LoadVersions(); // 自动加载客户端和服务端版本号
        IsEnabled = false; // 默认未启用
    }

    // 自动加载版本号
    public void LoadVersions()
    {
        // 获取 EscapeFromTarkov.exe 的版本号
        if (File.Exists(Path.Combine(GamePath, "EscapeFromTarkov.exe")))
        {
            var clientFileVersion = FileVersionInfo.GetVersionInfo(Path.Combine(GamePath, "EscapeFromTarkov.exe"));
            ClientVersion = clientFileVersion.FileVersion ?? "未知版本";
        }
        else
        {
            ClientVersion = "未找到文件";
        }

        // 获取 SPT.Server.exe 的版本号
        string[] possibleFileNames = { "SPT.Server.exe", "Aki.Server.exe", "Server.exe" };
        List<string> foundFiles = new List<string>();

        // 检查路径中存在的文件
        foreach (var fileName in possibleFileNames)
        {
            string fullPath = Path.Combine(GameServerPath, fileName);
            if (File.Exists(fullPath))
            {
                foundFiles.Add(fullPath);
            }
        }

        if (foundFiles.Count == 0)
        {
            // 没有找到任何文件
            ServerVersion = "未找到文件";
        }
        else if (foundFiles.Count > 1)
        {
            // 找到多个文件，提示用户
            ServerVersion = "检测到多个服务端文件，请保留一个文件并删除其他文件：\n" + string.Join("\n", foundFiles);
        }
        else
        {
            // 仅找到一个文件，获取其版本信息
            var serverFileVersion = FileVersionInfo.GetVersionInfo(foundFiles[0]);
            ServerVersion = serverFileVersion.FileVersion ?? "未知版本";
        }

    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
