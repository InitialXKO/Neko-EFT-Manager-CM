using System;
using System.Collections.Generic;
using CommunityToolkit.WinUI.Notifications;
using Windows.UI.Notifications;

public static class DownloadNotificationHelper
{
    private static Dictionary<string, ToastNotification> _activeNotifications = new Dictionary<string, ToastNotification>();

    public static void ShowOrUpdateDownloadNotification(string fileName, double progress, string status)
    {
        string tag = fileName;
        string group = "downloadProgress";

        // 创建 Toast 内容
        var builder = new ToastContentBuilder()
            .AddText("Download Status")
            .AddText($"{fileName} - {status}")
            .AddVisualChild(new AdaptiveProgressBar()
            {
                Title = "Progress",
                Value = new BindableProgressBarValue("progressValue"),
                ValueStringOverride = new BindableString("progressValueString"),
                Status = status
            });

        // 获取 Toast 内容
        var toastContent = builder.GetToastContent();

        // 创建新的 ToastNotification
        var toastNotification = new ToastNotification(toastContent.GetXml())
        {
            Tag = tag,
            Group = group
        };

        // 更新 Toast 内容中的绑定值
        var data = new NotificationData();
        data.Values["progressValue"] = (progress / 100).ToString("F2");
        data.Values["progressValueString"] = $"{progress:0.0}%";

        if (_activeNotifications.ContainsKey(tag))
        {
            // 更新现有通知
            ToastNotificationManagerCompat.CreateToastNotifier().Update(data, tag, group);
        }
        else
        {
            // 显示新的 ToastNotification
            ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotification);

            // 添加到活动通知字典
            _activeNotifications[tag] = toastNotification;
        }
    }

    public static void ShowDownloadCompletedNotification(string fileName)
    {
        ShowOrUpdateDownloadNotification(fileName, 100, "Download completed");
    }

    public static void ShowDownloadFailedNotification(string fileName, string errorMessage)
    {
        ShowOrUpdateDownloadNotification(fileName, 0, $"Download failed: {errorMessage}");
    }
}
