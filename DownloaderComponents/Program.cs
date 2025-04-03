using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DownloaderComponents;

class Program
{
    private static readonly object fileLock = new object();
    private static readonly object progressLock = new object();

    private static long totalDownloadedBytes = 0;
    private static long totalBytes = 0;

    static async Task Main(string[] args)
    {
        //Console.Clear();  // 启动时清空控制台
        if (args.Length < 2)
        {
            Console.WriteLine("使用方法：DownloaderComponents.exe <下载地址> <线程数>");
            return;
        }

        var downloadUrl = args[0];
        var numberOfThreads = int.Parse(args[1]);
        var fileName = Path.GetFileName(downloadUrl);
        var savePath = string.Empty;

        var thread = new Thread(() => { savePath = GetSaveFilePath(fileName); });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (string.IsNullOrEmpty(savePath))
        {
            Console.WriteLine("下载取消。未选择文件路径.");
            return;
        }

        //Console.Clear();
        Console.WriteLine($"下载文件中: {fileName}"); // 固定文件名显示在顶部

        try
        {
            await DownloadFileAsync(downloadUrl, savePath, numberOfThreads);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }

    private static string GetSaveFilePath(string defaultFileName)
    {
        var saveFileDialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter = "All files (*.*)|*.*"
        };

        var result = saveFileDialog.ShowDialog();
        return result == System.Windows.Forms.DialogResult.OK ? saveFileDialog.FileName : null;
    }

    public static async Task DownloadFileAsync(string url, string filePath, int numberOfThreads)
    {
        using (var client = new HttpClient())
        {
            totalBytes = await GetFileSizeAsync(client, url);
            var chunkSize = totalBytes / numberOfThreads;
            var tasks = new Task[numberOfThreads];
            var progressBarWidth = 50;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < numberOfThreads; i++)
            {
                var startByte = i * chunkSize;
                var endByte = (i == numberOfThreads - 1) ? totalBytes - 1 : (i + 1) * chunkSize - 1;
                tasks[i] = DownloadFilePartAsync(client, url, filePath, startByte, endByte, i, progressBarWidth, numberOfThreads);
            }

            var progressTask = Task.Run(async () =>
            {
                while (!Task.WhenAll(tasks).IsCompleted)
                {
                    DisplayTotalProgress(stopwatch.Elapsed, progressBarWidth, numberOfThreads);
                    await Task.Delay(500);
                }
                stopwatch.Stop();
                DisplayTotalProgress(stopwatch.Elapsed, progressBarWidth, numberOfThreads);
            });

            await Task.WhenAll(tasks);
            await progressTask;

            Console.WriteLine("\n下载完成.");
        }
    }

    public static async Task<long> GetFileSizeAsync(HttpClient client, string url)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Head, url);
        var response = await client.SendAsync(requestMessage);

        if (response.IsSuccessStatusCode)
        {
            return response.Content.Headers.ContentLength.GetValueOrDefault();
        }
        else
        {
            throw new Exception($"获取文件大小失败。状态代码: {response.StatusCode}");
        }
    }

    public static async Task DownloadFilePartAsync(HttpClient client, string url, string filePath, long startByte, long endByte, int partIndex, int progressBarWidth, int numberOfThreads)
    {
        long currentDownloadedBytes = 0;
        if (File.Exists(filePath))
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                currentDownloadedBytes = fileStream.Length;
            }
        }

        startByte = Math.Max(startByte + currentDownloadedBytes, 0);
        if (startByte > endByte) return;

        try
        {
            var rangeHeader = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { Range = rangeHeader }
            };

            var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[8192];
            int bytesRead;
            long totalBytesRead = currentDownloadedBytes;

            using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                fileStream.Seek(startByte, SeekOrigin.Begin);

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    lock (fileLock)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                    totalBytesRead += bytesRead;

                    lock (progressLock)
                    {
                        totalDownloadedBytes += bytesRead;
                    }

                    var progress = (double)totalBytesRead / (endByte - startByte + 1);
                    DisplayProgressBar(progress, progressBarWidth, partIndex, numberOfThreads);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载分块错误 {partIndex}: {ex.Message}");
        }
    }

    private static string GetColorForProgress(double progress)
    {
        // 计算红色、绿色和蓝色的值
        int red = (int)(255 * (1 - progress)); // 红色从255渐变到0
        int green = (int)(255 * progress);     // 绿色从0渐变到255
        int blue = 0;                          // 蓝色保持为0

        // 使用 ANSI 转义序列设置前景色
        return $"\x1b[38;2;{red};{green};{blue}m";
    }

    private static void DisplayProgressBar(double progress, int width, int partIndex, int numberOfThreads)
    {
        lock (progressLock)
        {
            // 计算进度条的填充长度和空白长度
            int progressBarFilledLength = (int)(progress * width);
            int progressBarEmptyLength = width - progressBarFilledLength;

            // 定义进度条的不同区间颜色
            string progressBar = "[";

            // 分段填充进度条并设置不同的颜色
            for (int i = 0; i < width; i++)
            {
                double segmentProgress = (double)i / width;
                string color = GetColorForProgress(segmentProgress);

                // 如果进度条当前部分已经填充，则使用填充符号，否则使用空白
                if (i < progressBarFilledLength)
                {
                    progressBar += color + "=" + "\x1b[0m"; // 设置填充部分颜色
                }
                else
                {
                    progressBar += " "; // 设置空白部分
                }
            }

            progressBar += "]";

            // 输出带有颜色的进度条
            Console.SetCursorPosition(0, partIndex + 1);  // 文件名占据0行
            Console.Write(progressBar + $" 分块 {partIndex + 1}: {progress * 100:F2}% ");
        }
    }

    private static void DisplayTotalProgress(TimeSpan elapsedTime, int width, int numberOfThreads)
    {
        lock (progressLock)
        {
            double totalProgress = (double)totalDownloadedBytes / totalBytes;
            int progressBarFilledLength = (int)(totalProgress * width);
            int progressBarEmptyLength = width - progressBarFilledLength;

            // 定义进度条的不同区间颜色
            string progressBar = "[";

            // 分段填充进度条并设置不同的颜色
            for (int i = 0; i < width; i++)
            {
                double segmentProgress = (double)i / width;
                string color = GetColorForProgress(segmentProgress);

                // 如果进度条当前部分已经填充，则使用填充符号，否则使用空白
                if (i < progressBarFilledLength)
                {
                    progressBar += color + "=" + "\x1b[0m"; // 设置填充部分颜色
                }
                else
                {
                    progressBar += " "; // 设置空白部分
                }
            }

            progressBar += "]";

            // 计算整个文件的下载速度
            double downloadSpeed = totalDownloadedBytes / (elapsedTime.TotalSeconds > 0 ? elapsedTime.TotalSeconds : 1);
            string etaText;

            // 如果下载速度过低，避免显示异常的剩余时间
            if (downloadSpeed < 1e-3)  // 设置最小速度阈值，例如 1 KB/s
            {
                etaText = "计算中...";
            }
            else
            {
                // 计算剩余时间
                double remainingTime = (totalBytes - totalDownloadedBytes) / downloadSpeed;
                etaText = $"剩余时间: {TimeSpan.FromSeconds(remainingTime):mm\\:ss}";
            }

            // 更新进度显示
            Console.SetCursorPosition(0, numberOfThreads + 2);
            Console.Write(progressBar + $" 总进度: {totalProgress * 100:F2}% | " +
                               $"已用时: {elapsedTime:mm\\:ss} | " +
                               $"{etaText}      ");
        }
    }

}
