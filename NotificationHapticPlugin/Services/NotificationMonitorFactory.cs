namespace Loupedeck.NotificationHapticPlugin.Services
{
    using System;

    /// <summary>
    /// Factory to create platform-specific notification monitors
    /// </summary>
    public static class NotificationMonitorFactory
    {
        public static INotificationMonitor Create()
        {
            if (PlatformService.IsWindows)
            {
                PluginLog.Info("Creating Windows Notification Monitor (Named Pipe)");
                PluginLog.Info("Connects to NotificationWatcher.exe via Named Pipe");
                return new WindowsNotificationMonitorPipe();
            }
            
            if (PlatformService.IsMacOS)
            {
                PluginLog.Info("Creating macOS notification monitor");
                return new MacOSNotificationMonitor();
            }

            throw new PlatformNotSupportedException($"Platform not supported: {PlatformService.GetPlatformName()}");
        }
    }
}