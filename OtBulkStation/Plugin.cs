using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OtBulkStation
{
    // Lets a modifier key speed up the input/output of refining stations (smelter, blast
    // furnace, charcoal kiln, cooking stations, stone oven) and fuel-burning light sources
    // (campfire, hearth, bonfire, braziers, sconces, torches):
    //   Use (E)                    - vanilla behavior, unchanged
    //   BatchModifierKey + Use     - insert BatchSize items at once / collect every finished item at once
    //   FillModifierKey + BatchModifierKey + Use - insert as many items as will fit
    // Station input capacities (e.g. charcoal kiln wood, smelter ore/fuel) can also be overridden per prefab.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.otbulkstation";
        public const string PluginName = "OtBulkStation";
        public const string PluginVersion = "1.0.3";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyCode> BatchModifierKey;
        internal static ConfigEntry<KeyCode> FillModifierKey;
        internal static ConfigEntry<int> BatchSize;
        internal static ConfigEntry<string> OreCapacityOverridesRaw;
        internal static ConfigEntry<string> FuelCapacityOverridesRaw;

        internal static Dictionary<string, int> OreCapacityOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        internal static Dictionary<string, int> FuelCapacityOverrides = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            BatchModifierKey = Config.Bind(
                "General",
                "BatchModifierKey",
                KeyCode.LeftShift,
                "Hold this with the station's Use key to insert BatchSize items at once, or (together with " +
                "FillModifierKey) fill the station in one press. Also: hold this alone while collecting cooked " +
                "food to collect everything that's finished in one press, instead of one item at a time.");

            FillModifierKey = Config.Bind(
                "General",
                "FillModifierKey",
                KeyCode.LeftAlt,
                "Hold this together with BatchModifierKey + Use to insert as many items as fit, up to the " +
                "station's capacity, in one press. Defaults to Alt rather than Ctrl because Ctrl is bound to " +
                "crouch by default.");

            BatchSize = Config.Bind(
                "General",
                "BatchSize",
                10,
                new ConfigDescription(
                    "How many items a single BatchModifierKey + Use press inserts.",
                    new AcceptableValueRange<int>(1, 100)));

            OreCapacityOverridesRaw = Config.Bind(
                "Input Capacities",
                "OreCapacityOverrides",
                "",
                "Overrides a station's ore/wood input capacity. Comma-separated prefab_name=amount pairs, e.g. " +
                "\"charcoal_kiln=100,smelter=40,blastfurnace=40\". Leave a station out to keep its vanilla " +
                "capacity. Applies to smelter-type stations only - prefab names: smelter, blastfurnace, " +
                "charcoal_kiln, eitrrefinery. See README.md for the full prefab name reference.");

            FuelCapacityOverridesRaw = Config.Bind(
                "Input Capacities",
                "FuelCapacityOverrides",
                "",
                "Overrides a station's fuel capacity - coal for smelter-type stations, wood for cooking " +
                "stations' fuel slot, or wood/resin for fire-type stations. Same format as " +
                "OreCapacityOverrides, e.g. \"fire_pit=50,hearth=100\" (fire_pit is the campfire's prefab " +
                "name, not \"campfire\"). Prefab names - smelter-type: smelter, blastfurnace (charcoal_kiln " +
                "and eitrrefinery have no separate fuel). Cooking-type: piece_cookingstation, " +
                "piece_cookingstation_iron, piece_oven. Fire-type: fire_pit (campfire), hearth, bonfire, " +
                "piece_brazierfloor01 (standing brazier), piece_brazierceiling01 (hanging brazier), " +
                "piece_walltorch (sconce), piece_groundtorch_wood (standing wood torch), piece_groundtorch " +
                "(standing iron torch). See README.md for the full reference.");

            ReparseCapacities();
            OreCapacityOverridesRaw.SettingChanged += (_, _) => ReparseCapacities();
            FuelCapacityOverridesRaw.SettingChanged += (_, _) => ReparseCapacities();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private static void ReparseCapacities()
        {
            OreCapacityOverrides = ParseOverrides(OreCapacityOverridesRaw.Value, "OreCapacityOverrides");
            FuelCapacityOverrides = ParseOverrides(FuelCapacityOverridesRaw.Value, "FuelCapacityOverrides");
        }

        private static Dictionary<string, int> ParseOverrides(string raw, string settingName)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return dict;
            }

            foreach (var entry in raw.Split(','))
            {
                var parts = entry.Split('=');
                if (parts.Length != 2)
                {
                    Log?.LogWarning(
                        $"{settingName}: ignoring \"{entry.Trim()}\" - expected prefab_name=amount, e.g. \"charcoal_kiln=200\".");
                    continue;
                }

                var name = parts[0].Trim();
                if (name.Length == 0)
                {
                    Log?.LogWarning($"{settingName}: ignoring \"{entry.Trim()}\" - missing prefab name before '='.");
                    continue;
                }

                if (int.TryParse(parts[1].Trim(), out var value) && value > 0)
                {
                    dict[name] = value;
                }
                else
                {
                    Log?.LogWarning($"{settingName}: ignoring \"{entry.Trim()}\" - \"{parts[1].Trim()}\" isn't a positive whole number.");
                }
            }

            return dict;
        }

        internal static bool IsBatchHeld() => ZInput.GetKey(BatchModifierKey.Value, false);

        internal static bool IsFillHeld() => ZInput.GetKey(FillModifierKey.Value, false);

        // How many items a single press should insert, given how much room is left.
        internal static int ResolveInsertCount(int room)
        {
            if (room <= 0)
            {
                return 0;
            }

            if (IsBatchHeld() && IsFillHeld())
            {
                return room;
            }

            if (IsBatchHeld())
            {
                return Math.Min(BatchSize.Value, room);
            }

            return 1;
        }

        private static string KeyName(KeyCode key)
        {
            var s = key.ToString();
            if (s.StartsWith("Left"))
            {
                return s.Substring(4);
            }

            if (s.StartsWith("Right"))
            {
                return s.Substring(5);
            }

            return s;
        }

        internal static string InsertHint()
        {
            if (BatchSize.Value <= 1)
            {
                return "";
            }

            var batchKey = KeyName(BatchModifierKey.Value);
            var fillKey = KeyName(FillModifierKey.Value);
            return Localization.instance.Localize(
                $"\n[<color=yellow><b>{batchKey} + $KEY_Use</b></color>] Add {BatchSize.Value}" +
                $"\n[<color=yellow><b>{fillKey}+{batchKey} + $KEY_Use</b></color>] Fill");
        }

        internal static string CollectAllHint()
        {
            var batchKey = KeyName(BatchModifierKey.Value);
            return Localization.instance.Localize($"\n[<color=yellow><b>{batchKey} + $KEY_Use</b></color>] Collect all");
        }
    }
}
