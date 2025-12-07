namespace Loupedeck.NotificationHapticPlugin
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// macOS 알림 수신 시 햅틱 피드백을 트리거하는 액션
    /// macOS의 log stream을 직접 모니터링하여 알림을 감지합니다.
    /// </summary>
    public class NotificationHapticAction : PluginDynamicCommand
    {
        private const string EventName = "notificationReceived";
        private const int DebounceMsec = 1500;

        private Process _logStreamProcess;
        private Thread _monitorThread;
        private volatile bool _isRunning;
        private long _lastTriggerTimeTicks;

        public NotificationHapticAction()
            : base(
                displayName: "Notification Haptic",
                description: "Triggers haptic feedback when macOS notification is received",
                groupName: "Haptics")
        {
        }

        protected override bool OnLoad()
        {
            // 햅틱 이벤트 등록
            this.Plugin.PluginEvents.AddEvent(
                EventName,
                "Notification Received",
                "Plays haptic when macOS notification is received"
            );

            // macOS 알림 모니터링 시작
            StartNotificationMonitor();

            PluginLog.Info("NotificationHapticAction loaded. Monitoring macOS notifications...");

            return true;
        }

        protected override bool OnUnload()
        {
            StopNotificationMonitor();
            PluginLog.Info("NotificationHapticAction unloaded");
            return true;
        }

        private void StartNotificationMonitor()
        {
            _isRunning = true;

            _monitorThread = new Thread(MonitorNotifications)
            {
                IsBackground = true,
                Name = "NotificationMonitor"
            };
            _monitorThread.Start();
        }

        private void MonitorNotifications()
        {
            while (_isRunning)
            {
                try
                {
                    StartLogStreamProcess();
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Error in notification monitor: {ex.Message}");
                }

                // 프로세스가 종료되면 잠시 대기 후 재시작
                if (_isRunning)
                {
                    Thread.Sleep(2000);
                    PluginLog.Info("Restarting log stream process...");
                }
            }
        }

        private void StartLogStreamProcess()
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

            PluginLog.Info("log stream process started");

            // 프로세스가 종료될 때까지 대기
            _logStreamProcess.WaitForExit();

            PluginLog.Info($"log stream process exited with code: {_logStreamProcess.ExitCode}");
        }

        private void OnLogDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            var line = e.Data.Trim();
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            var lower = line.ToLowerInvariant();

            // 1) NotificationCenter 로그 중에서도 com.apple.unc:application 로그만 사용
            if (!lower.Contains("[com.apple.unc:application]"))
            {
                return;
            }

            // 2) 알림 "표시" (present) 인 것만 사용
            if (!lower.Contains("queuing action present"))
            {
                return;
            }

            // 여기까지 왔으면 실제 사용자 알림이 화면에 뜨는 순간
            TriggerHapticWithDebounce(line);
        }

        private void OnLogErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                PluginLog.Warning($"log stream stderr: {e.Data}");
            }
        }

        private void TriggerHapticWithDebounce(string logLine)
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var lastTicks = Interlocked.Read(ref _lastTriggerTimeTicks);
            var elapsedMs = (nowTicks - lastTicks) / TimeSpan.TicksPerMillisecond;

            if (elapsedMs < DebounceMsec)
            {
                return;
            }

            // 디바운스 통과 - 시간 업데이트
            Interlocked.Exchange(ref _lastTriggerTimeTicks, nowTicks);

            // 햅틱 이벤트 발생
            this.Plugin.PluginEvents.RaiseEvent(EventName);
            PluginLog.Info($"🔔 macOS notification detected - Haptic triggered!");
            PluginLog.Verbose($"   Log: {logLine}");
        }

        private void StopNotificationMonitor()
        {
            _isRunning = false;

            try
            {
                if (_logStreamProcess != null && !_logStreamProcess.HasExited)
                {
                    _logStreamProcess.Kill();
                    _logStreamProcess.WaitForExit(2000);
                }
                _logStreamProcess?.Dispose();
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error stopping log stream process: {ex.Message}");
            }

            _monitorThread?.Join(TimeSpan.FromSeconds(3));
            PluginLog.Info("Notification monitor stopped");
        }

        /// <summary>
        /// 수동으로 버튼을 눌렀을 때도 햅틱 발생 (테스트용)
        /// </summary>
        protected override void RunCommand(string actionParameter)
        {
            this.Plugin.PluginEvents.RaiseEvent(EventName);
            PluginLog.Info("Haptic event triggered via manual button press");
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize) =>
            "Notification\nHaptic";
    }
}

