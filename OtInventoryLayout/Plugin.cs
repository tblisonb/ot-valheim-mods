using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace OtInventoryLayout
{
    // Vanilla's container UI clips to a fixed viewport sized for a 4-row inventory (e.g. the
    // blackmetal chest's 8x4 layout). Containers with more rows than that - the 1.0 Wardrobe is
    // 5x10 - need to be scrolled to see their full contents. This mod lets the viewport grow per
    // container prefab so more (or all) of its rows are visible at once, without changing the
    // container's actual width/height (so save data and item positions are untouched).
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.otinventorylayout";
        public const string PluginName = "OtInventoryLayout";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<string> VisibleRowOverridesRaw;
        internal static ConfigEntry<bool> LogContainerUiHierarchy;

        internal static Dictionary<string, int> VisibleRowOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            VisibleRowOverridesRaw = Config.Bind(
                "General",
                "VisibleRowOverrides",
                "piece_wardrobe=10",
                "Comma-separated prefab_name=rows pairs. For each listed container prefab, grows the " +
                "container panel so up to that many rows are visible without scrolling (capped at the " +
                "container's actual row count). Leave a prefab out to keep its vanilla panel size. " +
                "piece_wardrobe is the Wardrobe's prefab name (5 columns x 10 rows).");

            LogContainerUiHierarchy = Config.Bind(
                "Debug",
                "LogContainerUiHierarchy",
                false,
                "Dumps the container panel's full UI hierarchy (RectTransform sizes + components) to the " +
                "log every time a container is opened. Diagnostic aid for figuring out exactly what to " +
                "resize for a given container's panel - leave off for normal play.");

            ReparseOverrides();
            VisibleRowOverridesRaw.SettingChanged += (_, _) => ReparseOverrides();

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
            var raw = VisibleRowOverridesRaw.Value;
            if (string.IsNullOrWhiteSpace(raw))
            {
                VisibleRowOverrides = dict;
                return;
            }

            foreach (var entry in raw.Split(','))
            {
                var parts = entry.Split('=');
                if (parts.Length != 2)
                {
                    Log?.LogWarning(
                        $"VisibleRowOverrides: ignoring \"{entry.Trim()}\" - expected prefab_name=rows, e.g. \"piece_wardrobe=10\".");
                    continue;
                }

                var name = parts[0].Trim();
                if (name.Length == 0)
                {
                    Log?.LogWarning($"VisibleRowOverrides: ignoring \"{entry.Trim()}\" - missing prefab name before '='.");
                    continue;
                }

                if (int.TryParse(parts[1].Trim(), out var value) && value > 0)
                {
                    dict[name] = value;
                }
                else
                {
                    Log?.LogWarning($"VisibleRowOverrides: ignoring \"{entry.Trim()}\" - \"{parts[1].Trim()}\" isn't a positive whole number.");
                }
            }

            VisibleRowOverrides = dict;
        }
    }
}
