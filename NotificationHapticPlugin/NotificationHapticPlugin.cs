namespace Loupedeck.NotificationHapticPlugin
{
    using System;
    using System.Threading.Tasks;
    using Loupedeck.NotificationHapticPlugin.Services;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class NotificationHapticPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public NotificationHapticPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            PluginLog.Info("=================================================");
            PluginLog.Info("🎉 NotificationHaptic Plugin Loading...");
            PluginLog.Info($"   Platform: {PlatformService.GetPlatformName()}");
            PluginLog.Info("=================================================");
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            PluginLog.Info("NotificationHaptic Plugin Unloading...");
        }
    }
}

