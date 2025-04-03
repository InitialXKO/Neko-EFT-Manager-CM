using CommunityToolkit.WinUI.Notifications;
using System;
using Windows.UI.Notifications;

public static class ToastNotificationHelper
{
    private static ToastNotification _currentNotification;

    public static void ShowNotification(string title, string content, string buttonContent, Action<string> buttonAction, string argumentKey, string argumentValue)
    {
        // 创建 Toast 内容

        var builder = new ToastContentBuilder()
            .AddArgument(argumentKey, argumentValue)
            .AddText(title)
            .AddText(content)
            .AddButton(new ToastButton()
                .SetContent(buttonContent)
                .AddArgument("action", buttonContent.ToLower())
                .SetBackgroundActivation());

        // 获取 Toast 内容
        var toastContent = builder.GetToastContent();

        // 隐藏当前通知（如果有）
        if (_currentNotification != null)
        {
            ToastNotificationManagerCompat.History.Remove(_currentNotification.Tag);
        }

        // 创建新的 ToastNotification
        var toastNotification = new ToastNotification(toastContent.GetXml())
        {
            Tag = Guid.NewGuid().ToString()
        };

        // 显示新的 ToastNotification
        ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotification);

        // 更新当前通知
        _currentNotification = toastNotification;

        // 执行按钮动作
        buttonAction?.Invoke(argumentValue);
    }
}
