using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace FileSelector;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string mode;
    private ManagerConfig _config;

    // 保留无参数构造函数
    public MainWindow() : this(Environment.GetCommandLineArgs())
    {
    }

    public MainWindow(string[] args)
    {
        InitializeComponent();
        LoadConfig();
        LoadRootFolders();
        FolderTree.Background = new SolidColorBrush(Color.FromArgb(204, 255, 255, 255)); // 204 是透明度值
        // 解析启动参数
        if (args.Length > 1) // 确保 args[0] 是应用名，args[1] 是模式
        {
            mode = args[1].ToLower();
        }
        else
        {
            MessageBox.Show("未提供启动模式，默认为client模式。");
            mode = "client"; // 默认模式
        }
    }

    // 删除默认构造函数

    private void LoadConfig()
    {
        ManagerConfig.Load();
        _config = App.ManagerConfig; // 假设 App.ManagerConfig 是你的配置实例

        // 初始化 game_path.txt 文件
        InitializeGamePathFile();


    }

    private void InitializeGamePathFile()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "game_path.txt");

        // 检查文件是否存在，如果不存在则创建
        if (!File.Exists(filePath))
        {
            // 如果文件不存在，写入默认值（可以根据需要修改）
            string defaultPath = ""; // 默认值可以为空或特定路径
            File.WriteAllText(filePath, defaultPath);
        }
    }
    private void LoadRootFolders()
    {
        string[] drives = Directory.GetLogicalDrives();
        foreach (var drive in drives)
        {
            TreeViewNode rootNode = new TreeViewNode
            {
                Header = drive,
                Tag = drive
            };
            FolderTree.Items.Add(rootNode);
        }
    }

    private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var selectedNode = e.NewValue as TreeViewNode;
        if (selectedNode != null && selectedNode.Children.Count == 0)
        {
            LoadSubFolders(selectedNode);
        }
    }

    private void LoadSubFolders(TreeViewNode parentNode)
    {
        string path = parentNode.Tag as string;

        try
        {
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                TreeViewNode childNode = new TreeViewNode
                {
                    Header = Path.GetFileName(directory),
                    Tag = directory
                };
                parentNode.Children.Add(childNode);
                parentNode.Items.Add(childNode);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法访问文件夹: {ex.Message}");
        }
    }

    private bool ContainsChineseCharacters(string input)
    {
        return input.Any(c => c >= 0x4e00 && c <= 0x9fff); // 判断是否包含中文字符
    }

    private void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedNode = FolderTree.SelectedItem as TreeViewNode;
        if (selectedNode != null)
        {
            string folderPath = selectedNode.Tag.ToString();

            // 检查路径是否包含中文字符
            if (ContainsChineseCharacters(folderPath))
            {
                MessageBox.Show("所选路径包含中文字符，请确保游戏路径不包含中文字符。");
                return;
            }

            bool isClientMode = mode == "client";
            string executableFile = isClientMode ? "EscapeFromTarkov.exe" : "Aki.Server.exe"; // 可以扩展

            if (isClientMode && File.Exists(Path.Combine(folderPath, executableFile)))
            {
                MessageBox.Show($"选中的文件夹: {folderPath}，包含游戏可执行文件。");
                UpdateConfig("InstallPath", folderPath);
                WritePathToGamePathFile(folderPath); // 写入路径到 game_path.txt
                this.Close();
            }
            else if (!isClientMode &&
                     (File.Exists(Path.Combine(folderPath, "Aki.Server.exe")) ||
                      File.Exists(Path.Combine(folderPath, "SPT.Server.exe")) ||
                      File.Exists(Path.Combine(folderPath, "Server.exe"))))
            {
                MessageBox.Show($"选中的文件夹: {folderPath}，包含服务器可执行文件。");
                UpdateConfig("AkiServerPath", folderPath);
                WritePathToGamePathFile(folderPath); // 写入路径到 game_path.txt
                this.Close();
            }
            else
            {
                MessageBox.Show("所选文件夹不符合要求。");
            }
        }
        else
        {
            MessageBox.Show("请先选择一个文件夹。");
        }
    }



    private void WritePathToGamePathFile(string path)
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "game_path.txt");

        // 将路径写入 game_path.txt
        File.WriteAllText(filePath, path);
    }


    private void UpdateConfig(string key, string value)
    {
        string configDir = Path.Combine(AppContext.BaseDirectory, "UserData", "Config");
        string configFilePath = Path.Combine(configDir, "ManagerConfig.json");

        // 确保目录存在
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

        // 读取现有配置
        Dictionary<string, object> existingConfig = new();
        if (File.Exists(configFilePath))
        {
            string json = File.ReadAllText(configFilePath);
            existingConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }

        // 更新或添加配置项
        existingConfig[key] = value;

        // 保存更新后的配置
        string updatedJson = JsonSerializer.Serialize(existingConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configFilePath, updatedJson);
    }



    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

public class TreeViewNode : TreeViewItem
{
    public List<TreeViewNode> Children { get; } = new List<TreeViewNode>();
}

