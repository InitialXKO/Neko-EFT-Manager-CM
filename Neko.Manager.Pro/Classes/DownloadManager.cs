using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class DownloadManager
{
    private readonly HttpClient _httpClient;

    public event Action<string, long, long, DownloadNotification> DownloadProgressChanged;
    public event Action<string, bool, string, DownloadNotification> DownloadCompleted;

    public DownloadManager()
    {
        _httpClient = new HttpClient();
    }

    public async Task DownloadFileAsync(string url, string destinationPath, DownloadNotification notification)
    {
        try
        {
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var totalBytesRead = 0L;
                    var buffer = new byte[8192];
                    var isMoreToRead = true;

                    do
                    {
                        var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            notification.Progress = 100;
                            DownloadProgressChanged?.Invoke(url, totalBytesRead, totalBytes, notification);
                            continue;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        if (canReportProgress)
                        {
                            var progress = (double)totalBytesRead / totalBytes * 100;
                            notification.Progress = progress;
                            notification.Status = $"Downloading... ({totalBytesRead} / {totalBytes} bytes)";
                            DownloadProgressChanged?.Invoke(url, totalBytesRead, totalBytes, notification);
                        }
                    }
                    while (isMoreToRead);
                }
            }

            DownloadCompleted?.Invoke(url, true, null, notification);
        }
        catch (Exception ex)
        {
            DownloadCompleted?.Invoke(url, false, ex.Message, notification);
        }
    }
}
