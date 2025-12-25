namespace Loupedeck.NotificationHapticPlugin.Services
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connects to NotificationWatcher.exe via Named Pipe
    /// This is the CLEANEST solution - external app has full Windows API access
    /// </summary>
    public class WindowsNotificationMonitorPipe : INotificationMonitor
    {
        public event EventHandler<NotificationEventArgs> NotificationReceived;

        private volatile bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;
        private NamedPipeClientStream? _pipeClient;
        private DateTime _lastNotificationTime = DateTime.MinValue;
        private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(800);

        public bool IsRunning => _isRunning;

        public async Task StartAsync()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();

            PluginLog.Info("🚀 Starting Named Pipe notification monitoring...");
            PluginLog.Info("   Looking for NotificationWatcher.exe...");

            try
            {
                _ = Task.Run(() => ConnectToPipeAsync(_cancellationTokenSource.Token));
                PluginLog.Info("✅ Pipe monitor started");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"❌ Failed to start pipe monitor: {ex.Message}");
                _isRunning = false;
            }

            await Task.Delay(100);
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            try
            {
                _pipeClient?.Dispose();
            }
            catch { }

            PluginLog.Info("Pipe notification monitor stopped");
            await Task.Delay(100);
        }

        private async Task ConnectToPipeAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isRunning)
            {
                try
                {
                    PluginLog.Info("Attempting to connect to NotificationWatcher pipe...");
                    
                    _pipeClient = new NamedPipeClientStream(
                        ".",
                        "NotificationWatcherPipe",
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    // Try to connect (5 second timeout)
                    await _pipeClient.ConnectAsync(5000, ct);
                    
                    if (_pipeClient.IsConnected)
                    {
                        PluginLog.Info("✅ Connected to NotificationWatcher!");
                        await ReadPipeAsync(ct);
                    }
                }
                catch (TimeoutException)
                {
                    PluginLog.Warning("⚠️ NotificationWatcher.exe not found");
                    PluginLog.Info("   Please start NotificationWatcher.exe manually");
                    PluginLog.Info("   Location: [plugin folder]\\NotificationWatcher.exe");
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Pipe connection error: {ex.Message}");
                }
                finally
                {
                    _pipeClient?.Dispose();
                    _pipeClient = null;
                }

                if (!ct.IsCancellationRequested)
                {
                    PluginLog.Info("Retrying connection in 10 seconds...");
                    await Task.Delay(10000, ct);
                }
            }
        }

        private async Task ReadPipeAsync(CancellationToken ct)
        {
            try
            {
                using var reader = new StreamReader(_pipeClient!);
                
                while (!ct.IsCancellationRequested && _isRunning && _pipeClient.IsConnected)
                {
                    var line = await reader.ReadLineAsync();
                    
                    if (!string.IsNullOrEmpty(line))
                    {
                        ProcessPipeMessage(line);
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Pipe read error: {ex.Message}");
            }
        }

        private void ProcessPipeMessage(string message)
        {
            try
            {
                PluginLog.Verbose($"Received pipe message: {message}");
                
                // Expected format: "NOTIFICATION|2025-12-23T12:34:56.789Z"
                if (message.StartsWith("NOTIFICATION|"))
                {
                    var eventTime = DateTime.Now;
                    
                    if (ShouldTriggerHaptic(eventTime))
                    {
                        TriggerNotificationEvent(
                            "Windows Notification",
                            "Detected by NotificationWatcher.exe",
                            "Named Pipe");
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error processing pipe message: {ex.Message}");
            }
        }

        private bool ShouldTriggerHaptic(DateTime eventTime)
        {
            var now = DateTime.Now;
            
            if (now - _lastNotificationTime > _debounceInterval)
            {
                _lastNotificationTime = eventTime;
                return true;
            }
            
            return false;
        }

        private void TriggerNotificationEvent(string title, string body, string source)
        {
            try
            {
                NotificationReceived?.Invoke(this, new NotificationEventArgs(title, body, source));
                PluginLog.Info($"🔔 Notification received from NotificationWatcher!");
                PluginLog.Info($"   {title}: {body}");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error triggering notification event: {ex.Message}");
            }
        }

        public void TestNotificationTrigger()
        {
            TriggerNotificationEvent(
                "Manual Test",
                "Testing pipe communication",
                "Manual Test");
            
            PluginLog.Info("🧪 Manual test triggered");
        }
    }
}
