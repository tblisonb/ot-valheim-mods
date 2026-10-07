using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace OtInventoryLayout
{
    // Vanilla's container UI panel is sized to fit about 4 rows at its native column count (e.g.
    // the blackmetal chest's 8x4 layout fits exactly). Containers with more rows than that - the
    // 1.0 Wardrobe is 5x10 - need scrolling to see everything. Rather than just growing the panel
    // taller to show every real row (which for a 10-row container would need a panel taller than
    // fits on screen alongside the player's own, possibly-expanded, inventory), this mod *reflows*
    // a configured container's slots into a more compact display shape - e.g. the Wardrobe's 50
    // slots packed 8-wide become 6 full rows of 8 plus 2 leftover slots on a 7th row, matching the
    // chest's familiar width instead of the Wardrobe's native 5-wide shape. The container's real
    // Inventory width/height (and so its save data and item grid positions) are never touched -
    // only each slot's on-screen position is remapped, after vanilla finishes laying out the grid.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.otinventorylayout";
        public const string PluginName = "OtInventoryLayout";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<string> DisplayColumnOverridesRaw;
        internal static ConfigEntry<bool> LogContainerUiHierarchy;

        internal static Dictionary<string, int> DisplayColumnOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            DisplayColumnOverridesRaw = Config.Bind(
                "General",
                "DisplayColumnOverrides",
                "piece_chestwarderobe=8",
                "Comma-separated prefab_name=columns pairs. For each listed container prefab, repacks " +
                "its slots into a display grid this many columns wide (rows are however many that " +
                "takes - a container's slot count rarely divides evenly, so the last row may be " +
                "partially filled) instead of its native column count. Leave a prefab out to keep its " +
                "vanilla layout. piece_chestwarderobe is the Wardrobe's prefab name (note the game's " +
                "own typo - it's \"warderobe\", not \"wardrobe\") - native layout 5 columns x 10 rows " +
                "(50 slots), repacked at 8 columns into 6 full rows plus 2 slots on a 7th row, the " +
                "same width as the blackmetal chest.");

            LogContainerUiHierarchy = Config.Bind(
                "Debug",
                "LogContainerUiHierarchy",
                false,
                "Dumps the container panel's full UI hierarchy (RectTransform sizes + components) to the " +
                "log every time a container is opened. Diagnostic aid for figuring out exactly what to " +
                "resize for a given container's panel - leave off for normal play.");

            ReparseOverrides();
            DisplayColumnOverridesRaw.SettingChanged += (_, _) => ReparseOverrides();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private static void ReparseOverrides()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var raw = DisplayColumnOverridesRaw.Value;
            if (string.IsNullOrWhiteSpace(raw))
            {
                DisplayColumnOverrides = dict;
                return;
            }

            foreach (var entry in raw.Split(','))
            {
                var parts = entry.Split('=');
                if (parts.Length != 2)
                {
                    Log?.LogWarning(
                        $"DisplayColumnOverrides: ignoring \"{entry.Trim()}\" - expected prefab_name=columns, e.g. \"piece_chestwarderobe=8\".");
                    continue;
                }

                var name = parts[0].Trim();
                if (name.Length == 0)
                {
                    Log?.LogWarning($"DisplayColumnOverrides: ignoring \"{entry.Trim()}\" - missing prefab name before '='.");
                    continue;
                }

                if (int.TryParse(parts[1].Trim(), out var value) && value > 0)
                {
                    dict[name] = value;
                }
                else
                {
                    Log?.LogWarning($"DisplayColumnOverrides: ignoring \"{entry.Trim()}\" - \"{parts[1].Trim()}\" isn't a positive whole number.");
                }
            }

            DisplayColumnOverrides = dict;
        }
    }
}
