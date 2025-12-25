namespace Loupedeck.NotificationHapticPlugin.Services
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for cross-platform notification monitoring
    /// </summary>
    public interface INotificationMonitor
    {
        event EventHandler<NotificationEventArgs> NotificationReceived;
        Task StartAsync();
        Task StopAsync();
        bool IsRunning { get; }
    }

    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }

        public NotificationEventArgs(string title, string body, string source)
        {
            Title = title;
            Body = body;
            Source = source;
            Timestamp = DateTime.UtcNow;
        }
    }
}