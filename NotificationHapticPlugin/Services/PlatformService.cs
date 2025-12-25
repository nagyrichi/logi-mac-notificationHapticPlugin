namespace Loupedeck.NotificationHapticPlugin.Services
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Platform detection service
    /// </summary>
    public static class PlatformService
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        public static string GetPlatformName()
        {
            if (IsWindows) return "Windows";
            if (IsMacOS) return "macOS";
            return "Unknown";
        }
    }
}