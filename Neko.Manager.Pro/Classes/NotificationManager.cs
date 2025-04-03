using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes
{
    public class NotificationService
    {
        private static NotificationService _instance;
        private StackedNotificationsBehavior _notificationQueue;
        private Notification _currentNotification;

        private NotificationService() { }

        public static NotificationService Instance => _instance ??= new NotificationService();

        public void Initialize(StackedNotificationsBehavior notificationQueue)
        {
            _notificationQueue = notificationQueue;
        }

        public void ShowNotification(string title, string message, InfoBarSeverity severity)
        {
            if (_notificationQueue == null)
            {
                throw new InvalidOperationException("NotificationQueue has not been initialized.");
            }

            // Close the previous notification if any
            if (_currentNotification != null)
            {
                _notificationQueue.Remove(_currentNotification);
            }

            // Create a new notification
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Severity = severity
            };

            // Show the new notification
            _notificationQueue.Show(notification);

            // Keep track of the current notification
            _currentNotification = notification;

            _ = Task.Delay(5000).ContinueWith(t =>
            {
                _notificationQueue.DispatcherQueue.TryEnqueue(() =>
                {
                    _notificationQueue.Remove(notification);
                });
            });
        }
        
    }
}
