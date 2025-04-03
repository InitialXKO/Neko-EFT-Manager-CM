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
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI;
using Windows.UI.Popups;
using Neko.EFT.Manager.X.Classes;
using System.IO.Compression;
using System.Diagnostics;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class ClientManagerPage : Page
    {
        public static string Cfg = "BepInEx.cfg";
        public static string path = @"\BepInEx\config\";
        private string configFilePath = System.IO.Path.Combine(App.ManagerConfig.InstallPath + path + Cfg);
        private Dictionary<string, Dictionary<string, string>> configData = new Dictionary<string, Dictionary<string, string>>();
        private bool isAcrylic = true;
        private ThemeConfig themeConfig;

        // 配置项翻译字典
        private Dictionary<string, string> translations = new Dictionary<string, string>
        {
            // 大分类翻译
            { "Caching", "缓存" },
            { "Chainloader", "链式加载器" },
            { "Harmony.Logger", "Harmony日志管理器" },
            { "Logging", "日志" },
            { "Logging.Console", "日志-控制台输出" },
            { "Logging.Disk", "日志-磁盘输出" },
            { "Preloader", "预加载器" },
            { "Preloader.Entrypoint", "预加载器入口" },

            // 配置项翻译
            { "EnableAssemblyCache", "启用程序集缓存" },
            { "HideManagerGameObject", "隐藏管理对象" },
            { "LogChannels", "日志通道" },
            { "UnityLogListening", "监听Unity日志" },
            { "LogConsoleToUnityLog", "将Unity日志显示到控制台日志输出" },
            { "Enabled", "启用" },
            { "PreventClose", "防止关闭" },
            { "ShiftJisEncoding", "Shift-JIS编码" },
            { "StandardOutType", "标准输出类型" },
            { "LogLevels", "日志级别" },
            { "WriteUnityLog", "写入Unity日志" },
            { "AppendLog", "追加日志" },
            { "ApplyRuntimePatches", "应用运行时补丁" },
            { "HarmonyBackend", "Harmony后端" },
            { "DumpAssemblies", "转储程序集" },
            { "LoadDumpedAssemblies", "加载转储的程序集" },
            { "BreakBeforeLoadAssemblies", "在加载程序集前中断" },
            { "Assembly", "程序集" },
            { "Type", "类型" },
            { "Method", "方法" }
        };

        public ClientManagerPage()
        {
            this.InitializeComponent();
            this.Loaded += ClientManagerPage_Loaded;
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled; // 这样才是正确的
        }

        private async void SwitchBackground_Click(object sender, RoutedEventArgs e)
        {
            if (isAcrylic)
            {
                // 切换到渐变背景
                RootGrid.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                RootBorder.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                SwitchBackgroundButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                SaveConfigButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                PackLogsButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                themeConfig.Theme = "Gradient";
            }
            else
            {
                // 切换到亚克力背景
                RootGrid.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                RootBorder.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                SwitchBackgroundButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                SaveConfigButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                SaveConfigButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                PackLogsButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                themeConfig.Theme = "Acrylic";
            }
            isAcrylic = !isAcrylic;
            themeConfig.Save(); // Save the updated theme config
        }
        private async void ClientManagerPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load theme configuration
                await LoadConfig();
                GenerateUI();
                themeConfig = ThemeConfig.Load();

                if (themeConfig.Theme == "Gradient")
                {
                    RootGrid.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                    RootBorder.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                    SwitchBackgroundButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                    SaveConfigButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];
                    PackLogsButton.Background = (LinearGradientBrush)Application.Current.Resources["GradientBrush"];

                    isAcrylic = false;
                }
                else
                {
                    RootGrid.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    RootBorder.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    SwitchBackgroundButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    SaveConfigButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    SaveConfigButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    PackLogsButton.Background = (AcrylicBrush)Application.Current.Resources["AcrylicBrush"];
                    isAcrylic = true;
                }

                if (App.ManagerConfig.InstallPath != null)
                {
                    
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"Error in ModsPage_Loaded: {ex.Message}");
            }
            
        }

        private async Task LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                var lines = await File.ReadAllLinesAsync(configFilePath);
                string currentSection = string.Empty;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                        continue;

                    if (line.Trim().StartsWith("[") && line.Trim().EndsWith("]"))
                    {
                        currentSection = line.Trim().TrimStart('[').TrimEnd(']');
                        if (!configData.ContainsKey(currentSection))
                        {
                            configData[currentSection] = new Dictionary<string, string>();
                        }
                    }
                    else
                    {
                        var keyValue = line.Split(new char[] { '=' }, 2);
                        if (keyValue.Length == 2)
                        {
                            var key = keyValue[0].Trim();
                            var value = keyValue[1].Trim();
                            if (!configData[currentSection].ContainsKey(key))
                            {
                                configData[currentSection][key] = value;
                            }
                        }
                    }
                }
            }
        }

        private void GenerateUI()
        {
            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            foreach (var section in configData)
            {
                var translatedSection = translations.ContainsKey(section.Key) ? translations[section.Key] : section.Key;

                var sectionHeader = new TextBlock
                {
                    Text = $"[{translatedSection}]",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 20, 0, 10)  // Adjusted top margin to separate sections more visibly
                };
                stackPanel.Children.Add(sectionHeader);

                foreach (var keyValuePair in section.Value)
                {
                    var grid = new Grid { Margin = new Thickness(5) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Remaining space for label or control
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Auto width for CheckBox or TextBox

                    var translatedKey = translations.ContainsKey(keyValuePair.Key) ? translations[keyValuePair.Key] : keyValuePair.Key;

                    var textBlock = new TextBlock
                    {
                        Text = $"{translatedKey}:",
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 17
                    };
                    Grid.SetColumn(textBlock, 0);
                    grid.Children.Add(textBlock);

                    var value = keyValuePair.Value.ToLower();
                    if (value == "true" || value == "false")
                    {
                        var checkBox = new CheckBox
                        {
                            IsChecked = value == "true",
                            Tag = $"{section.Key}.{keyValuePair.Key}",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Right, // Align CheckBox to the right
                            Margin = new Thickness(0, 0, 10, 0)  // Adjusted margin for CheckBox
                        };
                        Grid.SetColumn(checkBox, 1);
                        grid.Children.Add(checkBox);
                    }
                    else
                    {
                        var textBox = new TextBox
                        {
                            Text = keyValuePair.Value,
                            Tag = $"{section.Key}.{keyValuePair.Key}",
                            Margin = new Thickness(0, 5, 0, 0),  // Adjusted margin for TextBox
                            Width = 200,  // Fixed width for TextBox
                            HorizontalAlignment = HorizontalAlignment.Right, // Align TextBox to the right
                            TextAlignment = TextAlignment.Left  // Align text inside TextBox to the left
                        };
                        Grid.SetColumn(textBox, 1);
                        grid.Children.Add(textBox);
                    }

                    stackPanel.Children.Add(grid);
                }
            }

            // Add save button
            //var saveButton = new Button
            //{
            //    Content = "保存配置",
            //    Width = 200,
            //    Height = 50,
            //    HorizontalAlignment = HorizontalAlignment.Right,
            //    VerticalAlignment = VerticalAlignment.Bottom,
            //    Margin = new Thickness(10),
            //    CornerRadius = new CornerRadius(8),
            //    FontSize = 19  // Adjusted font size
            //};

            //var gradientBrush = new LinearGradientBrush
            //{
            //    StartPoint = new Point(0, 0),
            //    EndPoint = new Point(1, 1)
            //};

            //gradientBrush.GradientStops.Add(new GradientStop
            //{
            //    Color = Colors.Pink,
            //    Offset = 1
            //});

            //gradientBrush.GradientStops.Add(new GradientStop
            //{
            //    Color = Colors.LightBlue,
            //    Offset = 0
            //});

            //saveButton.Background = gradientBrush;

            //saveButton.Click += SaveConfig_Click;
            //stackPanel.Children.Add(saveButton);

            //// Add pack logs button
            //var packLogsButton = new Button
            //{
            //    Content = "转存日志",
            //    Width = 200,
            //    Height = 50,
            //    HorizontalAlignment = HorizontalAlignment.Right,
            //    VerticalAlignment = VerticalAlignment.Bottom,
            //    Margin = new Thickness(10),
            //    CornerRadius = new CornerRadius(8),
            //    FontSize = 19  // Adjusted font size
            //};

            //packLogsButton.Click += PackLogsButton_Click;
            //stackPanel.Children.Add(packLogsButton);

            MainScrollViewer.Content = stackPanel;
        }

        private async void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            var configLines = new List<string>();

            foreach (var section in configData)
            {
                configLines.Add($"[{section.Key}]");
                foreach (var keyValuePair in section.Value)
                {
                    var tag = $"{section.Key}.{keyValuePair.Key}";
                    var control = FindControlByTag(MainScrollViewer.Content as StackPanel, tag);

                    if (control is CheckBox checkBox)
                    {
                        configLines.Add($"{keyValuePair.Key} = {checkBox.IsChecked}");
                    }
                    else if (control is TextBox textBox)
                    {
                        configLines.Add($"{keyValuePair.Key} = {textBox.Text}");
                    }
                }
                configLines.Add(string.Empty); // 空行分隔段落
            }

            await File.WriteAllLinesAsync(configFilePath, configLines);
            await Utils.ShowInfoBar("保存", " 保存成功", InfoBarSeverity.Success);
        }

        private Control FindControlByTag(Panel parent, string tag)
        {
            foreach (var child in parent.Children)
            {
                if (child is Control control && control.Tag != null && control.Tag.ToString() == tag)
                {
                    return control;
                }
                if (child is Panel panel)
                {
                    var result = FindControlByTag(panel, tag);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private void PackLogsButton_Click(object sender, RoutedEventArgs e)
        {
            // Perform log packing
            PackLogs();
        }

        private async void PackLogs()
        {
            string installPath = App.ManagerConfig.InstallPath;
            string logPackageFolder = Path.Combine(installPath, "LogPackage");

            // Ensure the LogPackage folder exists
            Directory.CreateDirectory(logPackageFolder);

            // Get the latest log directory based on the directory name
            var logDirectories = Directory.GetDirectories(Path.Combine(installPath, "Logs"));
            var latestLogDirectory = logDirectories
                .Select(dir => new DirectoryInfo(dir))
                .OrderByDescending(dir => dir.CreationTime)
                .FirstOrDefault();

            if (latestLogDirectory == null)
            {
                await Utils.ShowInfoBar("错误", "找不到日志目录", InfoBarSeverity.Error);
                return;
            }

            // Define paths
            string clientLogPath = latestLogDirectory.FullName;
            string bepinexLogPath = Path.Combine(installPath, "BepInEx", "LogOutput.log");
            string zipFilePath = Path.Combine(logPackageFolder, $"Logs_{DateTime.Now:yyyyMMddHHmmss}.zip");

            using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                // Add client logs
                var clientLogDirInZip = zip.CreateEntry("Client-Log/");
                foreach (var file in Directory.GetFiles(clientLogPath))
                {
                    zip.CreateEntryFromFile(file, $"Client-Log/{Path.GetFileName(file)}");
                }

                // Add BepInEx log
                if (File.Exists(bepinexLogPath))
                {
                    zip.CreateEntryFromFile(bepinexLogPath, "Bepinex-Log/LogOutput.log");
                }
            }

            // Open the folder containing the zip file

            await Utils.ShowInfoBar("转存成功", " 日志已完成打包，请在新打开的目录中将日志压缩包发送给开发者以诊断问题", InfoBarSeverity.Success);

            Process.Start(new ProcessStartInfo
            {
                FileName = logPackageFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }


        //private Control FindControlByTag(Panel parent, string tag)
        //{
        //    foreach (var child in parent.Children)
        //    {
        //        if (child is Control control && control.Tag != null && control.Tag.ToString() == tag)
        //        {
        //            return control;
        //        }
        //        if (child is Panel panel)
        //        {
        //            var result = FindControlByTag(panel, tag);
        //            if (result != null)
        //            {
        //                return result;
        //            }
        //        }
        //    }
        //    return null;
        //}
    }
}
