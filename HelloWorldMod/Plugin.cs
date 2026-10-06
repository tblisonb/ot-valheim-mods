using BepInEx;

namespace HelloWorldMod
{
    // The three values below (GUID, Name, Version) are what BepInEx shows in its
    // plugin list and log output. The GUID should be globally unique - the usual
    // convention is reverse-domain-ish, e.g. "yourname.valheim.modname".
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.helloworldmod";
        public const string PluginName = "HelloWorldMod";
        public const string PluginVersion = "0.1.0";

        // Awake() runs once, as soon as BepInEx loads this plugin at game startup -
        // long before a world or player exists. Good place for setup and logging.
        private void Awake()
        {
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded!");
        }
    }
}
