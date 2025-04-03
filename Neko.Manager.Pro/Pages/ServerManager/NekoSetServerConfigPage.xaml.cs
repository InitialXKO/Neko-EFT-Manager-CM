using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Pages.ServerManager
{
    public sealed partial class NekoSetServerConfigPage : Page
    {
        private Dictionary<string, Dictionary<string, UIElement>> configFileControls;
        private string ConfigRootDirectory;

        public NekoSetServerConfigPage()
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
            configFileControls = new Dictionary<string, Dictionary<string, UIElement>>();
            Loaded += async (sender, e) =>
            {
                await InitializeAsync();
            };
        }

        private async Task InitializeAsync()
        {
            try
            {
                ConfigRootDirectory = GetConfigRootDirectory();
                if (ConfigRootDirectory == null)
                {
                    await ShowErrorDialog("配置错误", "未找到有效的服务端配置目录.");
                    return;
                }

                await AddConfig("Server/configs/http.json");
                await AddConfig("Server/configs/core.json");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("初始化错误", $"初始化时发生错误: {ex.Message}");
            }
        }

        private string GetConfigRootDirectory()
        {
            var possibleDirectories = new[] { "Aki_Data", "SPT_Data" };
            foreach (var dir in possibleDirectories)
            {
                var path = Path.Combine(App.ManagerConfig.AkiServerPath, dir);
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            throw new DirectoryNotFoundException("未找到有效的服务端配置目录.");
        }

        private async Task AddConfig(string configFilePath)
        {
            try
            {
                string fullConfigFilePath = Path.Combine(ConfigRootDirectory, configFilePath);

                if (!File.Exists(fullConfigFilePath))
                {
                    await ShowErrorDialog("配置错误", $"配置文件 {configFilePath} 不存在,请确认你的服务端目录已设置");
                    return;
                }

                string json = await File.ReadAllTextAsync(fullConfigFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                TextBlock title = new TextBlock
                {
                    Text = Path.GetFileName(fullConfigFilePath),
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                StackPanel configPanel = new StackPanel
                {
                    Margin = new Thickness(0, 10, 0, 20)
                };
                configPanel.Children.Add(title);

                Dictionary<string, UIElement> controls = new Dictionary<string, UIElement>();

                ProcessConfig(config, configPanel, controls);

                configFileControls[fullConfigFilePath] = controls;
                ConfigFilesStackPanel.Children.Add(configPanel);
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("加载错误", $"加载配置文件 {configFilePath} 时发生错误: {ex.Message}");
            }
        }

        private void ProcessConfig(Dictionary<string, JsonElement> config, StackPanel configPanel, Dictionary<string, UIElement> controls)
        {
            foreach (var kvp in config)
            {
                if (kvp.Value.ValueKind == JsonValueKind.Array)
                {
                    continue;
                }

                TextBlock label = new TextBlock
                {
                    Text = kvp.Key,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                switch (kvp.Value.ValueKind)
                {
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        CheckBox checkBox = new CheckBox
                        {
                            IsChecked = kvp.Value.GetBoolean(),
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        controls[kvp.Key] = checkBox;
                        configPanel.Children.Add(label);
                        configPanel.Children.Add(checkBox);
                        break;
                    case JsonValueKind.Object:
                        var nestedConfig = kvp.Value.EnumerateObject().ToDictionary(k => k.Name, v => v.Value);
                        StackPanel nestedConfigPanel = new StackPanel
                        {
                            Margin = new Thickness(20, 0, 0, 0)
                        };
                        configPanel.Children.Add(label);
                        configPanel.Children.Add(nestedConfigPanel);
                        ProcessConfig(nestedConfig, nestedConfigPanel, controls);
                        break;
                    default:
                        TextBox textBox = new TextBox
                        {
                            Text = kvp.Value.ToString(),
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        controls[kvp.Key] = textBox;
                        configPanel.Children.Add(label);
                        configPanel.Children.Add(textBox);
                        break;
                }
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var configFilePath in configFileControls.Keys)
                {
                    string fullConfigFilePath = configFilePath;

                    string originalJson = await File.ReadAllTextAsync(fullConfigFilePath);
                    var originalConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(originalJson);

                    foreach (var kvp in configFileControls[configFilePath])
                    {
                        if (kvp.Value is CheckBox checkBox)
                        {
                            originalConfig[kvp.Key] = JsonDocument.Parse(checkBox.IsChecked.ToString()).RootElement;
                        }
                        else if (kvp.Value is TextBox textBox)
                        {
                            originalConfig[kvp.Key] = JsonDocument.Parse(JsonSerializer.Serialize(textBox.Text)).RootElement;
                        }
                    }

                    string updatedJson = JsonSerializer.Serialize(originalConfig, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(fullConfigFilePath, updatedJson);
                }

                ContentDialog dialog = new ContentDialog
                {
                    Title = "保存成功",
                    Content = "配置已保存.",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("保存错误", $"保存配置文件时发生错误: {ex.Message}");
            }
        }
    }
}
