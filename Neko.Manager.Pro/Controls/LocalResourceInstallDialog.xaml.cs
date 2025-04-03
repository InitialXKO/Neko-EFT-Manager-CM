using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using EFT.Visual;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Windows;
using SharpCompress.Archives;
using SharpCompress.Common;


// 使用命名空间别名，避免与项目中的 Windows 命名空间冲突
using FilePickers = global::Windows.Storage.Pickers;

namespace Neko.EFT.Manager.X.Controls
{
    public sealed partial class LocalResourceInstallDialog : ContentDialog
    {
        public event EventHandler InstallCompleted;
        private CancellationTokenSource _cancellationTokenSource;
        private Stopwatch _installStopwatch;
        private long _totalBytesProcessed;
        private long _totalBytesToProcess;
        private string _currentFile;

        public LocalResourceInstallDialog(XamlRoot xamlRoot)
        {
            this.InitializeComponent();
            this.XamlRoot = xamlRoot; // 关键：手动设置 XamlRoot
        }

        // 客户端文件选择按钮事件处理
        private async void ClientSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FilePickers.FileOpenPicker();
            picker.SuggestedStartLocation = FilePickers.PickerLocationId.Desktop;

            // 获取窗口句柄
            ConfigGuideWindow window = App.c_window as ConfigGuideWindow;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            // 仅支持 .zip 文件
            picker.FileTypeFilter.Add(".zip");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // 校验文件名是否以 "Client." 开头
                if (!Path.GetFileName(file.Path).StartsWith("Client.", StringComparison.OrdinalIgnoreCase))
                {
                    // 在界面上显示错误，而不是弹出 ContentDialog
                    ErrorTextBlock.Text = "请选择有效的客户端压缩包（文件名需以 'Client.' 开头）。";
                    ErrorTextBlock.Visibility = Visibility.Visible;  // 确保可见
                    return;
                }

                // 隐藏错误信息
                ErrorTextBlock.Visibility = Visibility.Collapsed;
                ClientFilePathTextBox.Text = file.Path;
            }
        }


        // 服务端文件选择按钮事件处理
        private async void ServerSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FilePickers.FileOpenPicker();
            picker.SuggestedStartLocation = FilePickers.PickerLocationId.Desktop;
            // 支持 .7z 与 .zip 格式
            picker.FileTypeFilter.Add(".7z");
            picker.FileTypeFilter.Add(".zip");
            ConfigGuideWindow window = App.c_window as ConfigGuideWindow;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string fileName = Path.GetFileName(file.Path);
                // 校验文件名是否以 "SPT-" 或 "SPT-AKI" 开头
                if (!(fileName.StartsWith("SPT-", StringComparison.OrdinalIgnoreCase) ||
                      fileName.StartsWith("SPT-AKI", StringComparison.OrdinalIgnoreCase)))
                {
                    ErrorTextBlock.Text = "请选择有效的客户端压缩包（文件名需以 'SPT' 开头）。";
                    ErrorTextBlock.Visibility = Visibility.Visible;  // 确保可见
                }
                ServerFilePathTextBox.Text = file.Path;
            }
        }

        // 当用户点击 ContentDialog 的"确定"按钮时触发此事件
        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(ClientFilePathTextBox.Text) ||
                string.IsNullOrWhiteSpace(ServerFilePathTextBox.Text))
            {
                args.Cancel = true;
                ErrorTextBlock.Text = "\"请同时选择客户端和服务端压缩包。\"";
                ErrorTextBlock.Visibility = Visibility.Visible;  // 确保可见

                return;
            }

            try
            {
                var folderPicker = new FilePickers.FolderPicker();
                folderPicker.SuggestedStartLocation = FilePickers.PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*"); // 允许选择任何文件夹
                ConfigGuideWindow window = App.c_window as ConfigGuideWindow;
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder == null)
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "未选择安装目标文件夹。";
                    ErrorTextBlock.Visibility = Visibility.Visible;  // 确保可见
                    return;
                }

                string destinationPath = folder.Path;
                var cancellationTokenSource = new CancellationTokenSource();

                // 解压客户端
                await ExtractArchiveAsync(ClientFilePathTextBox.Text, destinationPath, cancellationTokenSource.Token);

                // 查找游戏目录
                string gameDirectory = FindGameDirectory(destinationPath);
                if (string.IsNullOrEmpty(gameDirectory))
                {
                    args.Cancel = true;
                    ErrorTextBlock.Text = "未找到 EscapeFromTarkov.exe，请检查客户端文件。";
                    ErrorTextBlock.Visibility = Visibility.Visible;  // 确保可见
                    
                    return;
                }

                // 解压服务端到游戏目录
                await ExtractArchiveAsync(ServerFilePathTextBox.Text, gameDirectory, cancellationTokenSource.Token);

                // 设置游戏路径
                App.ManagerConfig.InstallPath = gameDirectory;
                ManagerConfig.SaveAccountInfo();

                ErrorTextBlock.Text = "游戏已成功安装！";
                ErrorTextBlock.Visibility = Visibility.Visible;  // 确保可见
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                ErrorTextBlock.Text = $"安装失败: {ex.Message}";
                ErrorTextBlock.Visibility = Visibility.Visible;  // 确保可见
                
            }
        }


        private async void InstallPathSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FilePickers.FolderPicker();

            // 绑定窗口句柄 (WinUI 3 桌面应用必须绑定，否则会报错)
            ConfigGuideWindow window = App.c_window as ConfigGuideWindow;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = FilePickers.PickerLocationId.Desktop;

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                InstallPathTextBox.Text = folder.Path;
            }
        }

        /// <summary>
        /// 根据压缩包后缀调用不同解压方式，并报告进度
        /// </summary>
        private async Task ExtractArchiveAsync(string archivePath, string extractPath, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(archivePath);
            long archiveSize = fileInfo.Length;
            
            await Task.Run(() =>
            {
                var extension = Path.GetExtension(archivePath).ToLower();
    
                if (extension is ".7z" or ".rar")
                {
                    using var archive = new SevenZipExtractor.ArchiveFile(archivePath);
                    
                    // 更新任务状态
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateTaskStatus($"正在解压 {Path.GetFileName(archivePath)}...", 0);
                    });
                    
                    // 由于无法获取进度，直接解压并更新总体进度
                    archive.Extract(extractPath);
                    
                    // 解压完成后更新进度
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        UpdateTaskStatus($"{Path.GetFileName(archivePath)} 解压完成", 50);
                        _totalBytesProcessed += archiveSize;
                    });
                }
                else if (extension == ".zip")
                {
                    using var archive = ArchiveFactory.Open(archivePath);
                    var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
                    int totalEntries = entries.Count;
                    int processedEntries = 0;
                    
                    foreach (var entry in entries)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException();
                            
                        string fileName = entry.Key;
                        
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            _currentFile = fileName;
                            UpdateFileProgress(fileName, 0);
                        });
                        
                        entry.WriteToDirectory(extractPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                        
                        processedEntries++;
                        _totalBytesProcessed += entry.Size;
                        
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            UpdateFileProgress(fileName, 100);
                            double overallProgress = (processedEntries * 100.0 / totalEntries);
                            UpdateTotalProgress(overallProgress);
                        });
                    }
                }
                else
                {
                    throw new NotSupportedException($"不支持的压缩格式: {extension}");
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 在指定目录中查找包含 EscapeFromTarkov.exe 的文件夹
        /// </summary>
        private string FindGameDirectory(string baseDirectory)
        {
            try
            {
                var files = Directory.GetFiles(baseDirectory, "EscapeFromTarkov.exe", SearchOption.AllDirectories);
                return files.Any() ? Path.GetDirectoryName(files.First()) : null;
            }
            catch
            {
                return null;
            }
        }

        // 安装按钮点击事件
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ClientFilePathTextBox.Text) ||
                string.IsNullOrWhiteSpace(ServerFilePathTextBox.Text) ||
                string.IsNullOrWhiteSpace(InstallPathTextBox.Text))
            {
                ErrorTextBlock.Text = "请同时选择客户端、服务端压缩包和安装路径。";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            // 重置并显示进度界面
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ProgressGrid.Visibility = Visibility.Visible;
            InstallButton.IsEnabled = false;
            
            // 初始化进度跟踪变量
            _cancellationTokenSource = new CancellationTokenSource();
            _installStopwatch = Stopwatch.StartNew();
            _totalBytesProcessed = 0;
            _totalBytesToProcess = new FileInfo(ClientFilePathTextBox.Text).Length + 
                                  new FileInfo(ServerFilePathTextBox.Text).Length;

            try
            {
                string destinationPath = InstallPathTextBox.Text;

                // 更新进度
                UpdateTaskStatus("准备解压客户端文件...", 0);

                // 解压客户端
                await ExtractArchiveAsync(ClientFilePathTextBox.Text, destinationPath, _cancellationTokenSource.Token);

                // 查找游戏目录
                UpdateTaskStatus("查找游戏目录...", 50);
                string gameDirectory = FindGameDirectory(destinationPath);
                if (string.IsNullOrEmpty(gameDirectory))
                {
                    ErrorTextBlock.Text = "未找到 EscapeFromTarkov.exe，请检查客户端文件。";
                    ErrorTextBlock.Visibility = Visibility.Visible;
                    ProgressGrid.Visibility = Visibility.Collapsed;
                    InstallButton.IsEnabled = true;
                    return;
                }

                // 更新进度
                UpdateTaskStatus("解压服务端文件...", 50);

                // 解压服务端到游戏目录
                await ExtractArchiveAsync(ServerFilePathTextBox.Text, gameDirectory, _cancellationTokenSource.Token);

                // 更新进度
                UpdateTaskStatus("保存配置...", 95);

                // 设置游戏路径
                App.ManagerConfig.InstallPath = gameDirectory;
                App.ManagerConfig.AkiServerPath = gameDirectory;
                ManagerConfig.SaveAccountInfo();
                ManagerConfig.Save();
                
                UpdateTaskStatus("安装完成！", 100);
                _installStopwatch.Stop();
                
                // 触发安装完成事件
                InstallCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                ErrorTextBlock.Text = "安装已取消。";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"安装失败: {ex.Message}";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
            finally
            {
                InstallButton.IsEnabled = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        // 取消按钮点击事件
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            CancelButton.IsEnabled = false;
            UpdateTaskStatus("正在取消安装...", TotalProgressBar.Value);
        }

        // 更新任务状态
        private void UpdateTaskStatus(string taskDescription, double progress)
        {
            CurrentTaskTextBlock.Text = taskDescription;
            UpdateTotalProgress(progress);
        }

        // 更新总体进度
        private void UpdateTotalProgress(double progressValue)
        {
            TotalProgressBar.Value = progressValue;
            ProgressPercentTextBlock.Text = $"{progressValue:F1}%";
            
            // 计算剩余时间
            if (_installStopwatch != null && _totalBytesProcessed > 0 && progressValue > 0)
            {
                double elapsedSeconds = _installStopwatch.ElapsedMilliseconds / 1000.0;
                double bytesPerSecond = _totalBytesProcessed / elapsedSeconds;
                
                if (bytesPerSecond > 0)
                {
                    long remainingBytes = _totalBytesToProcess - _totalBytesProcessed;
                    double remainingSeconds = remainingBytes / bytesPerSecond;
                    
                    if (remainingSeconds > 0)
                    {
                        TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                        string timeText = remainingTime.TotalHours >= 1 
                            ? $"预计剩余时间: {remainingTime.Hours}小时{remainingTime.Minutes}分钟" 
                            : $"预计剩余时间: {remainingTime.Minutes}分钟{remainingTime.Seconds}秒";
                        
                        TimeRemainingTextBlock.Text = timeText;
                    }
                }
            }
        }

        // 更新文件进度
        private void UpdateFileProgress(string fileName, double progress)
        {
            if (fileName.Length > 50)
            {
                fileName = "..." + fileName.Substring(fileName.Length - 47);
            }
            
            CurrentFileTextBlock.Text = $"当前文件: {fileName}";
            FileProgressBar.Value = progress;
        }
    }
}
