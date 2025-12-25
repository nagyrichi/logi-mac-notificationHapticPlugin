# NotificationHaptic Plugin

A cross-platform Logi Options+ plugin that provides haptic feedback to Logitech mice (such as MX Master 4) when system notifications are triggered on **macOS** and **Windows**.

## ✨ Features

- **Cross-platform support**: Works on both macOS and Windows
- Automatically detects system notifications on both platforms
- Delivers haptic vibration to Logitech devices when notifications occur
- Supports custom haptic waveforms for different devices
- Prevents duplicate notifications with 1.5-second debouncing
- Platform-specific notification detection:
  - **macOS**: Uses `log stream` to monitor NotificationCenter
  - **Windows**: Monitors Event Log and system processes for notification activity

## 📦 Installation

### Requirements

- **macOS** or **Windows 10/11**
- **Logi Options+** ([Download](https://www.logitech.com/software/logi-options-plus.html))
- **Haptic-enabled Logitech device** (e.g., MX Master 4, MX Master 4S)

### Installation Steps

**For macOS and Windows:**

1. Download the `NotificationHaptic.lplug4` file.
2. **Double-click** the file.
3. Logi Options+ will open.
4. **Important**: You must finalize the installation manually:
   - Go to your device settings (e.g., MX Master 4).
   - Click on **"Smart Actions"** or **"Plugins"** in the sidebar.
   - Click **"INSTALL AND UNINSTALL PLUGINS"** (or Manage Plugins).
   - You should see a confirmation message that "NotificationHaptic" is installed.
5. The plugin is now active!

**Additional step for Windows users:**

6. Download and run `NotificationWatcher.exe` from the Release folder
7. The app will appear in the system tray
8. Right-click the tray icon and enable **"Run at Startup"** to launch it automatically
9. Verify the status shows "Plugin Connected" when Logi Options+ is running

**Manual installation (macOS only):**

```bash
cp NotificationHaptic.lplug4 ~/Library/Application\ Support/Logi/LogiPluginService/Plugins/
```

**Manual installation (Windows):**

```cmd
copy NotificationHaptic.lplug4 "%LOCALAPPDATA%\Logi\LogiPluginService\Plugins\"
```

## 🚀 Usage

1. **Launch Logi Options+**
2. Navigate to your device's button settings
3. Assign the **"Notification Haptic"** action to any button (optional)
4. Now haptic feedback will automatically trigger whenever a system notification appears on macOS or Windows!

> **Note**: Button assignment is optional. Once the plugin is loaded, notification detection starts automatically on both platforms.

## 🔧 How It Works

### macOS
1. The plugin monitors the NotificationCenter process using macOS's `log stream` command
2. Filters for "Queuing action present" logs of type `com.apple.unc:application`
3. Triggers a haptic event when filtered logs are detected

### Windows
1. **Requires NotificationWatcher.exe** - a companion tray application that monitors Windows notifications
2. The tray app uses Windows `UserNotificationListener` API to detect new notifications in real-time
3. Communicates with the plugin via Named Pipes
4. Sends haptic signals to the Logitech device when notifications are detected

**Important for Windows users**: 
- Launch `NotificationWatcher.exe` from the Release folder after installing the plugin
- The app runs in the system tray and must be running for notifications to be detected
- Right-click the tray icon to enable "Run at Startup" for automatic launching
- The tray icon shows connection status (Connected/Disconnected) with the plugin

## 🎵 Haptic Waveforms

- **Default**: `knock` (repetitive knock pattern)
- **MX Master 4**: `ringing` (continuous vibration pattern)

## 📝 Viewing Logs

Plugin logs can be found at:

**macOS:**
```
~/Library/Application Support/Logi/LogiPluginService/Logs/
```

**Windows:**
```
%LOCALAPPDATA%\Logi\LogiPluginService\Logs\
```

## 🛠️ Development

### Building

**On any platform:**
```bash
cd NotificationHapticPlugin
dotnet build -c Release
```

### Packaging

**On any platform:**
```bash
logiplugintool pack ./bin/Release ./NotificationHaptic.lplug4
```

### Verification

**On any platform:**
```bash
logiplugintool verify ./NotificationHaptic.lplug4
```

## 📄 License

MIT License - Copyright © 2025 hyunjun.kim

## 🐛 Troubleshooting

### When Haptics Don't Work

1. Verify that Logi Options+ is running
2. Check that your device is properly connected
3. Confirm that your device supports haptics
4. Restart Logi Options+

### When Notifications Aren't Detected

**macOS:**
1. Verify that macOS system notifications are enabled
2. Check logs: `~/Library/Application Support/Logi/LogiPluginService/Logs/`

**Windows:**
1. **Ensure NotificationWatcher.exe is running** (check system tray)
2. Verify the tray icon shows "Plugin Connected" status
3. Check Windows notifications are enabled in Settings > System > Notifications
4. Grant notification access permission when prompted by NotificationWatcher
5. Check NotificationWatcher logs: `%TEMP%\NotificationWatcher.log`
6. Check plugin logs: `%LOCALAPPDATA%\Logi\LogiPluginService\Logs\`

## 💡 Detected Notifications

**macOS:**
- Messages (iMessage, SMS)
- Focus mode changes
- Third-party app notifications (Slack, Discord, etc.)
- System app notifications (Calendar, Reminders, etc.)

**Windows:**
- Windows Toast notifications (via UserNotificationListener API)
- Application notifications (Teams, Outlook, Slack, etc.)
- System notifications
- Any notification that appears in the Windows Action Center

---

**Enjoy!** 🎉

