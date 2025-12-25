using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotificationWatcher
{
    public class NotificationWatcherContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private UserNotificationListener? _listener;
        private CancellationTokenSource _cts;
        private int _notificationCount = 0;
        private DateTime _lastNotification = DateTime.MinValue;
        private NamedPipeServerStream? _pipeServer;
        private StreamWriter? _pipeWriter;
        private StreamWriter? _logWriter;
        private readonly string _logPath;
        private bool _isPluginConnected = false;

        public NotificationWatcherContext()
        {
            _cts = new CancellationTokenSource();
            
            // Setup log file in TEMP
            _logPath = Path.Combine(Path.GetTempPath(), "NotificationWatcher.log");
            
            try
            {
                _logWriter = new StreamWriter(_logPath, append: true) { AutoFlush = true };
                LogInfo("=== NotificationWatcher Started ===");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create log: {ex.Message}", "Error");
            }
            
            // Setup tray icon
            var icon = LoadIconFromResource();
            _trayIcon = new NotifyIcon()
            {
                Icon = icon ?? SystemIcons.Application,
                ContextMenuStrip = CreateContextMenu(),
                Visible = true,
                Text = "Notification Watcher - Starting..."
            };

            LogInfo("Tray icon created, starting initialization...");
            
            // Start init
            Task.Run(async () => 
            {
                try
                {
                    await InitializeAsync();
                    UpdateTrayText("Active");
                    UpdateMenuStatus("Active - Connected");
                    LogInfo("Initialization complete - Active");
                }
                catch (Exception ex)
                {
                    LogError($"FATAL: {ex}");
                    UpdateTrayText("Error!");
                    UpdateMenuStatus("Error!");
                    ShowBalloon("Init Failed", $"See: {_logPath}", ToolTipIcon.Error);
                }
            });
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();
            
            var statusItem = new ToolStripMenuItem("Status: Initializing...") { Enabled = false };
            menu.Items.Add(statusItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            var countItem = new ToolStripMenuItem("Notifications: 0") { Enabled = false, Name = "CountItem" };
            menu.Items.Add(countItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            var startupItem = new ToolStripMenuItem("Run at Startup", null, OnToggleStartup)
            {
                Name = "StartupItem",
                Checked = IsStartupEnabled()
            };
            menu.Items.Add(startupItem);
            
            menu.Items.Add(new ToolStripSeparator());
            
            var logItem = new ToolStripMenuItem("Open Log", null, (s, e) => 
            {
                try
                {
                    if (File.Exists(_logPath))
                        System.Diagnostics.Process.Start("notepad.exe", _logPath);
                }
                catch { }
            });
            menu.Items.Add(logItem);
            
            var exitItem = new ToolStripMenuItem("Exit", null, OnExit);
            menu.Items.Add(exitItem);
            
            return menu;
        }

        private async Task InitializeAsync()
        {
            LogInfo("InitializeAsync started");
            UpdateTrayText("Requesting access...");
            
            try
            {
                LogInfo("Trying UserNotificationListener.RequestAccessAsync...");
                
                var accessStatus = await Task.Run(async () => 
                {
                    try
                    {
                        var status = await UserNotificationListener.Current.RequestAccessAsync();
                        return status;
                    }
                    catch (Exception ex)
                    {
                        LogError($"RequestAccessAsync exception: {ex.GetType().Name} - {ex.Message}");
                        throw;
                    }
                });
                
                LogInfo($"Access status: {accessStatus}");
                
                if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
                {
                    LogInfo("Access GRANTED! Setting up listener...");
                    _listener = UserNotificationListener.Current;
                    _listener.NotificationChanged += OnNotificationChanged;
                    
                    UpdateTrayText("Active (Listener)");
                    ShowBalloon("Active", "Using Notification Listener API", ToolTipIcon.Info);
                }
                else
                {
                    LogInfo($"Access DENIED: {accessStatus}. Starting polling...");
                    UpdateTrayText("Active (Polling)");
                    ShowBalloon("Access Denied", $"Status: {accessStatus}\nUsing polling fallback", ToolTipIcon.Warning);
                    _ = Task.Run(() => StartPollingAsync());
                }
            }
            catch (Exception ex)
            {
                LogError($"Init error: {ex.GetType().Name} - {ex.Message}");
                UpdateTrayText("Active (Polling)");
                _ = Task.Run(() => StartPollingAsync());
            }

            // Start pipe server
            LogInfo("Starting pipe server...");
            _ = Task.Run(() => StartPipeServerAsync(_cts.Token));
        }

        private async Task StartPollingAsync()
        {
            LogInfo("Starting UserNotificationListener polling mode...");
            
            if (_listener == null)
            {
                LogError("Listener is null! Cannot poll.");
                return;
            }
            
            var knownNotificationIds = new HashSet<uint>();
            
            // Get initial notifications
            try
            {
                var initialNotifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                foreach (var notif in initialNotifications)
                {
                    knownNotificationIds.Add(notif.Id);
                }
                LogInfo($"Initial notification count: {knownNotificationIds.Count}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to get initial notifications: {ex.GetType().Name} - {ex.Message}");
            }
            
            int checkCounter = 0;
            
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                    checkCounter++;
                    
                    if (checkCounter % 10 == 0)
                    {
                        LogInfo($"[Check #{checkCounter}] Current notifications: {notifications.Count}");
                    }
                    
                    // Check for new notifications
                    int newCount = 0;
                    foreach (var notif in notifications)
                    {
                        if (!knownNotificationIds.Contains(notif.Id))
                        {
                            knownNotificationIds.Add(notif.Id);
                            newCount++;
                            LogInfo($"NEW notification detected! ID: {notif.Id}");
                        }
                    }
                    
                    if (newCount > 0)
                    {
                        OnNotificationDetected("Listener Polling", newCount);
                    }
                    
                    // Ha nincsenek notifikációk és volt számláló, reseteljük
                    if (notifications.Count == 0 && _notificationCount > 0)
                    {
                        LogInfo("No notifications present, resetting counter");
                        _notificationCount = 0;
                        UpdateTrayText("Active");
                        UpdateCountMenu(0);
                        knownNotificationIds.Clear();
                    }
                    // Cleanup old IDs (keep set manageable)
                    else if (knownNotificationIds.Count > 100)
                    {
                        knownNotificationIds.Clear();
                        foreach (var notif in notifications)
                        {
                            knownNotificationIds.Add(notif.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Polling error: {ex.GetType().Name} - {ex.Message}");
                }

                await Task.Delay(300, _cts.Token);
            }
        }

        private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
        {
            if (args.ChangeKind == UserNotificationChangedKind.Added)
            {
                LogInfo("Notification detected via Listener API");
                OnNotificationDetected("Listener", 1);
            }
        }

        private void OnNotificationDetected(string source, int count)
        {
            // Debounce
            if ((DateTime.Now - _lastNotification).TotalMilliseconds < 800)
                return;

            _lastNotification = DateTime.Now;
            _notificationCount += count;
            
            UpdateTrayText($"Active - {_notificationCount} total");
            UpdateCountMenu(_notificationCount);
            
            LogInfo($"🔔 NOTIFICATION from {source} (count: {count}, total: {_notificationCount})");
            
            // Signal plugin
            SignalPlugin();
        }

        private async Task StartPipeServerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    LogInfo("Creating pipe server...");
                    _isPluginConnected = false;
                    UpdateMenuStatus("Active - Waiting for Plugin");
                    
                    _pipeServer = OperatingSystem.IsWindows() 
                        ? new NamedPipeServerStream(
                            "NotificationWatcherPipe",
                            PipeDirection.Out,
                            1,
                            PipeTransmissionMode.Message,
                            PipeOptions.Asynchronous)
                        : throw new PlatformNotSupportedException();

                    LogInfo("Waiting for plugin connection...");
                    await _pipeServer.WaitForConnectionAsync(ct);
                    LogInfo("✅ Plugin CONNECTED!");
                    _isPluginConnected = true;
                    _pipeWriter = new StreamWriter(_pipeServer) { AutoFlush = true };
                    UpdateMenuStatus("Active - Plugin Connected");

                    // Heartbeat - rendszeresen ellenőrizzük a kapcsolatot
                    int heartbeatCounter = 0;
                    while (_isPluginConnected && !ct.IsCancellationRequested)
                    {
                        await Task.Delay(1000, ct);
                        heartbeatCounter++;
                        
                        // Minden 5 másodpercben küldünk egy heartbeat-et
                        if (heartbeatCounter >= 5)
                        {
                            heartbeatCounter = 0;
                            try
                            {
                                _pipeWriter?.WriteLine("HEARTBEAT");
                                LogInfo("Heartbeat sent");
                            }
                            catch (Exception ex)
                            {
                                LogError($"Heartbeat failed: {ex.Message}");
                                _isPluginConnected = false;
                                break;
                            }
                        }
                    }
                    
                    LogInfo("Plugin disconnected");
                    UpdateMenuStatus("Active - Disconnected");
                }
                catch (Exception ex)
                {
                    LogError($"Pipe server error: {ex.Message}");
                }
                finally
                {
                    _pipeWriter?.Dispose();
                    _pipeWriter = null;
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }

                if (!ct.IsCancellationRequested)
                    await Task.Delay(1000, ct);
            }
        }

        private void SignalPlugin()
        {
            try
            {
                if (_isPluginConnected && _pipeWriter != null && _pipeServer != null && _pipeServer.IsConnected)
                {
                    var message = $"NOTIFICATION|{DateTime.Now:O}";
                    _pipeWriter.WriteLine(message);
                    LogInfo($"Signal sent to plugin: {message}");
                }
                else
                {
                    LogInfo("Plugin not connected, signal not sent");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to signal plugin: {ex.Message}");
                // Write hiba = disconnect
                _isPluginConnected = false;
                LogInfo("Detected disconnection via write error");
            }
        }

        private void UpdateTrayText(string text)
        {
            _trayIcon.Text = $"Notification Watcher - {text}";
        }

        private void UpdateMenuStatus(string status)
        {
            try
            {
                var menu = _trayIcon.ContextMenuStrip;
                if (menu != null && menu.Items.Count > 0 && menu.Items[0] is ToolStripMenuItem statusItem)
                {
                    if (menu.InvokeRequired)
                    {
                        menu.Invoke(new Action(() => statusItem.Text = $"Status: {status}"));
                    }
                    else
                    {
                        statusItem.Text = $"Status: {status}";
                    }
                }
            }
            catch { }
        }

        private void UpdateCountMenu(int count)
        {
            var item = _trayIcon.ContextMenuStrip?.Items.Find("CountItem", false).FirstOrDefault();
            if (item != null)
                item.Text = $"Notifications: {count}";
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            _trayIcon.ShowBalloonTip(3000, title, text, icon);
        }

        private Icon? LoadIconFromResource()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "NotificationWatcher.icon32.png";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var bitmap = new System.Drawing.Bitmap(stream);
                    return Icon.FromHandle(bitmap.GetHicon());
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to load icon: {ex.Message}");
            }
            return null;
        }

        private void LogInfo(string message)
        {
            var logMessage = $"[INFO] {DateTime.Now:HH:mm:ss.fff} - {message}";
            Console.WriteLine(logMessage);
            
            try
            {
                _logWriter?.WriteLine(logMessage);
            }
            catch { }
        }

        private void LogError(string message)
        {
            var logMessage = $"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}";
            Console.WriteLine(logMessage);
            
            try
            {
                _logWriter?.WriteLine(logMessage);
            }
            catch { }
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue("NotificationWatcher") != null;
            }
            catch
            {
                return false;
            }
        }

        private void OnToggleStartup(object? sender, EventArgs e)
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    LogError("Cannot get executable path");
                    return;
                }

                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null)
                {
                    LogError("Cannot access registry startup key");
                    return;
                }

                var isEnabled = IsStartupEnabled();
                if (isEnabled)
                {
                    key.DeleteValue("NotificationWatcher", false);
                    LogInfo("Startup disabled");
                    ShowBalloon("Startup", "Auto-start disabled", ToolTipIcon.Info);
                }
                else
                {
                    key.SetValue("NotificationWatcher", $"\"{exePath}\"");
                    LogInfo("Startup enabled");
                    ShowBalloon("Startup", "Auto-start enabled", ToolTipIcon.Info);
                }

                // Update menu
                if (sender is ToolStripMenuItem menuItem)
                {
                    menuItem.Checked = !isEnabled;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to toggle startup: {ex.Message}");
                MessageBox.Show($"Failed to change startup setting: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            LogInfo("Shutting down...");
            _cts.Cancel();
            
            if (_listener != null)
                _listener.NotificationChanged -= OnNotificationChanged;
            
            _pipeServer?.Dispose();
            _trayIcon.Visible = false;
            _logWriter?.Dispose();
            
            Application.Exit();
        }
    }
}
