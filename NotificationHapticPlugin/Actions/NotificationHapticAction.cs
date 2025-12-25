namespace Loupedeck.NotificationHapticPlugin
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Loupedeck.NotificationHapticPlugin.Services;

    /// <summary>
    /// Cross-platform notification haptic feedback action
    /// Supports both macOS and Windows notification detection
    /// </summary>
    public class NotificationHapticAction : PluginDynamicCommand
    {
        private const string EventName = "notificationReceived";
        private const int DebounceMsec = 1500;

        private INotificationMonitor _notificationMonitor;
        private volatile bool _isRunning;
        private long _lastTriggerTimeTicks;

        public NotificationHapticAction()
            : base(
                displayName: "Notification Haptic",
                description: $"Triggers haptic feedback when {PlatformService.GetPlatformName()} notification is received",
                groupName: "Cross-Platform Haptics")
        {
        }

        protected override bool OnLoad()
        {
            try
            {
                // Haptic event registration
                this.Plugin.PluginEvents.AddEvent(
                    EventName,
                    "Notification Received",
                    $"Plays haptic when {PlatformService.GetPlatformName()} notification is received"
                );

                // Create platform-specific notification monitor
                _notificationMonitor = NotificationMonitorFactory.Create();
                _notificationMonitor.NotificationReceived += OnNotificationReceived;

                // Start notification monitoring
                _ = Task.Run(async () => await StartNotificationMonitoringAsync());

                PluginLog.Info($"NotificationHapticAction loaded for {PlatformService.GetPlatformName()}. Monitoring system notifications...");
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to load NotificationHapticAction: {ex.Message}");
                return false;
            }
        }

        protected override bool OnUnload()
        {
            try
            {
                _ = Task.Run(async () => await StopNotificationMonitoringAsync());
                PluginLog.Info("NotificationHapticAction unloaded");
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error during NotificationHapticAction unload: {ex.Message}");
                return false;
            }
        }

        private async Task StartNotificationMonitoringAsync()
        {
            try
            {
                _isRunning = true;
                await _notificationMonitor.StartAsync();
                PluginLog.Info($"{PlatformService.GetPlatformName()} notification monitoring started successfully");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to start notification monitoring: {ex.Message}");
                _isRunning = false;
            }
        }

        private async Task StopNotificationMonitoringAsync()
        {
            try
            {
                _isRunning = false;
                
                if (_notificationMonitor != null)
                {
                    _notificationMonitor.NotificationReceived -= OnNotificationReceived;
                    await _notificationMonitor.StopAsync();
                }
                
                PluginLog.Info($"{PlatformService.GetPlatformName()} notification monitoring stopped");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error stopping notification monitoring: {ex.Message}");
            }
        }

        private void OnNotificationReceived(object sender, NotificationEventArgs e)
        {
            TriggerHapticWithDebounce(e);
        }

        private void TriggerHapticWithDebounce(NotificationEventArgs notificationArgs)
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var lastTicks = Interlocked.Read(ref _lastTriggerTimeTicks);
            var elapsedMs = (nowTicks - lastTicks) / TimeSpan.TicksPerMillisecond;

            if (elapsedMs < DebounceMsec)
            {
                return;
            }

            // Update debounce timestamp
            Interlocked.Exchange(ref _lastTriggerTimeTicks, nowTicks);

            // Trigger haptic event
            this.Plugin.PluginEvents.RaiseEvent(EventName);
            
            PluginLog.Info($"🔔 {PlatformService.GetPlatformName()} notification detected - Haptic triggered!");
            PluginLog.Verbose($"   Source: {notificationArgs.Source}");
            PluginLog.Verbose($"   Title: {notificationArgs.Title}");
        }

        /// <summary>
        /// Manual trigger when user presses the button (useful for testing)
        /// </summary>
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            // Use platform-specific icon
            var iconName = PlatformService.IsWindows ? "windows_notification" : "macos_notification";
            return PluginResources.ReadImage($"Actions\\{iconName}_{imageSize}.png") 
                   ?? PluginResources.ReadImage($"Actions\\notification_{imageSize}.png");
        }

        protected override void RunCommand(String actionParameter)
        {
            // Manual haptic trigger for testing
            this.Plugin.PluginEvents.RaiseEvent(EventName);
            PluginLog.Info($"🎯 Manual haptic trigger activated on {PlatformService.GetPlatformName()}!");
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize) =>
            $"Notification Haptic ({PlatformService.GetPlatformName()})";
    }
}

