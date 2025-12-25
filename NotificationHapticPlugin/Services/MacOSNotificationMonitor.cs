namespace Loupedeck.NotificationHapticPlugin.Services
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// macOS notification monitor using log stream
    /// </summary>
    public class MacOSNotificationMonitor : INotificationMonitor
    {
        public event EventHandler<NotificationEventArgs> NotificationReceived;

        private Process _logStreamProcess;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isRunning;

        public bool IsRunning => _isRunning;

        public async Task StartAsync()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            PluginLog.Info("Starting macOS notification monitor...");
            
            await Task.Run(() => MonitorNotifications(_cancellationTokenSource.Token));
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            try
            {
                if (_logStreamProcess != null && !_logStreamProcess.HasExited)
                {
                    _logStreamProcess.Kill();
                    await Task.Run(() => _logStreamProcess.WaitForExit(2000));
                }
                _logStreamProcess?.Dispose();
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error stopping macOS notification monitor: {ex.Message}");
            }

            PluginLog.Info("macOS notification monitor stopped");
        }

        private void MonitorNotifications(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    StartLogStreamProcess(cancellationToken);
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Error in macOS notification monitor: {ex.Message}");
                }

                if (!cancellationToken.IsCancellationRequested && _isRunning)
                {
                    Thread.Sleep(2000);
                    PluginLog.Info("Restarting log stream process...");
                }
            }
        }

        private void StartLogStreamProcess(CancellationToken cancellationToken)
        {
            _logStreamProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/log",
                    Arguments = "stream --style syslog --level info --predicate \"process == \\\"NotificationCenter\\\"\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _logStreamProcess.OutputDataReceived += OnLogDataReceived;
            _logStreamProcess.ErrorDataReceived += OnLogErrorReceived;

            _logStreamProcess.Start();
            _logStreamProcess.BeginOutputReadLine();
            _logStreamProcess.BeginErrorReadLine();

            PluginLog.Info("macOS log stream process started");

            // Wait for process exit or cancellation
            while (!_logStreamProcess.HasExited && !cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }

            PluginLog.Info($"macOS log stream process exited with code: {_logStreamProcess.ExitCode}");
        }

        private void OnLogDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            var line = e.Data.Trim();
            if (string.IsNullOrEmpty(line)) return;

            var lower = line.ToLowerInvariant();

            // Filter for NotificationCenter logs
            if (!lower.Contains("[com.apple.unc:application]")) return;

            // Filter for notification presentation
            if (!lower.Contains("queuing action present")) return;

            // Trigger notification event
            NotificationReceived?.Invoke(this, new NotificationEventArgs(
                "macOS System Notification", 
                "System notification detected", 
                "macOS NotificationCenter"));

            PluginLog.Verbose($"macOS notification detected: {line}");
        }

        private void OnLogErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                PluginLog.Warning($"macOS log stream stderr: {e.Data}");
            }
        }
    }
}