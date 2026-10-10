using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace OtMegingjord
{
    // Vanilla's Megingjord (BeltStrength) is bought from Haldor, can't be upgraded and always adds
    // +150 carry weight. This gives it seven upgrade levels, one per biome after the Black Forest.
    // Each level needs a mythology-themed item that Haldor starts selling once the boss opening that
    // biome is dead, plus that biome's materials, and is done at the Forge. The carry bonus grows
    // with the belt's quality. Everything is client-side: new items are registered locally, the belt
    // recipe is added to the local ObjectDB, and Haldor's extra stock is added to the local
    // player's view of his shop.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.otmegingjord";
        public const string PluginName = "OtMegingjord";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<int> MaxForgeLevel;
        internal static readonly Dictionary<int, TierConfig> TierConfigs = new Dictionary<int, TierConfig>();

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            MaxForgeLevel = Config.Bind(
                "General",
                "MaxForgeLevel",
                7,
                "Upgrading the belt to level N needs a Forge of level N, capped at this value, since " +
                "a vanilla Forge can't go past 7. Restart the game after changing it.");

            foreach (var tier in Tiers.All)
            {
                string section = $"Level {tier.Level} ({tier.Biome})";
                TierConfigs[tier.Level] = new TierConfig
                {
                    Price = Config.Bind(section, "Price", tier.Price,
                        $"Coins Haldor charges for {tier.DisplayName}."),
                    CarryBonus = Config.Bind(section, "CarryBonus", tier.CarryBonus,
                        $"Carry weight added by upgrading the belt to level {tier.Level}, on top of the " +
                        "levels below it (vanilla's level 1 adds 150)."),
                    Materials = Config.Bind(section, "Materials", tier.Materials,
                        $"Materials needed for the level {tier.Level} upgrade besides {tier.DisplayName}, " +
                        "as comma-separated PrefabName:Amount pairs. Restart the game after changing it."),
                    UnlockKey = Config.Bind(section, "UnlockKey", tier.UnlockKey,
                        $"World (global) key that must be set before Haldor sells {tier.DisplayName}. " +
                        "Empty: always sold."),
                };
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        // Total bonus of a belt at the given quality: vanilla's own value for level 1, plus each
        // configured level up to it.
        internal static float CarryBonusAt(int quality, float vanillaBonus)
        {
            float total = vanillaBonus;
            for (int level = 2; level <= quality && level <= Tiers.MaxLevel; level++)
            {
                if (TierConfigs.TryGetValue(level, out var config))
                {
                    total += config.CarryBonus.Value;
                }
            }
            return total;
        }
    }

    internal sealed class TierConfig
    {
        public ConfigEntry<int> Price;
        public ConfigEntry<int> CarryBonus;
        public ConfigEntry<string> Materials;
        public ConfigEntry<string> UnlockKey;
    }
}
