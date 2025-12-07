# NotificationHaptic Plugin

A Logi Options+ plugin that provides haptic feedback to Logitech mice (such as MX Master 4) when macOS system notifications are triggered.

## ✨ Features

- Automatically detects all macOS system notifications
- Delivers haptic vibration to Logitech devices when notifications occur
- Supports custom haptic waveforms for different devices
- Prevents duplicate notifications with 1.5-second debouncing

## 📦 Installation

### Requirements

- **macOS** (This plugin is macOS-only)
- **Logi Options+** ([Download](https://www.logitech.com/software/logi-options-plus.html))
- **Haptic-enabled Logitech device** (e.g., MX Master 4, MX Master 4S)

### Installation Steps

1. Download the `NotificationHaptic.lplug4` file
2. **Double-click** the file
3. Logi Options+ will automatically open and install the plugin
4. Restart Logi Options+

Or install manually:

```bash
cp NotificationHaptic.lplug4 ~/Library/Application\ Support/Logi/LogiPluginService/Plugins/
```

## 🚀 Usage

1. **Launch Logi Options+**
2. Navigate to your device's button settings
3. Assign the **"Notification Haptic"** action to any button (optional)
4. Now haptic feedback will automatically trigger whenever a macOS notification appears!

> **Note**: Button assignment is optional. Once the plugin is loaded, notification detection starts automatically.

## 🔧 How It Works

1. The plugin monitors the NotificationCenter process using macOS's `log stream` command
2. Filters for "Queuing action present" logs of type `com.apple.unc:application`
3. Triggers a haptic event when filtered logs are detected
4. Logi Options+ sends the haptic signal to the connected device

## 🎵 Haptic Waveforms

- **Default**: `knock` (repetitive knock pattern)
- **MX Master 4**: `ringing` (continuous vibration pattern)

## 📝 Viewing Logs

Plugin logs can be found at:

```
~/Library/Application Support/Logi/LogiPluginService/Logs/
```

## 🛠️ Development

### Building

```bash
cd NotificationHapticPlugin
dotnet build -c Release
```

### Packaging

```bash
logiplugintool pack ./bin/Release ./NotificationHaptic.lplug4
```

### Verification

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

1. Verify that macOS system notifications are enabled
2. Check logs: `~/Library/Application Support/Logi/LogiPluginService/Logs/`

## 💡 Detected Notifications

- Messages (iMessage, SMS)
- Focus mode changes
- Third-party app notifications (Slack, Discord, etc.)
- System app notifications (Calendar, Reminders, etc.)

---

**Enjoy!** 🎉

