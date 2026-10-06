using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace OtExtensionReach
{
    // Crafting-station upgrade pieces (StationExtension - forge bellows/cooling, workbench
    // chopping block, cauldron spice rack, galdr table rune, black forge/artisan table upgrades,
    // ...) refuse to even be placed - "need station nearby" / "need more space" - unless built
    // within a few meters of their station and clear of other upgrades. Both checks are purely
    // client-side UI gating in Player.UpdatePlacementGhost; nothing server-side re-validates them.
    // The "need station" check and the actual level-bonus range check (CraftingStation.GetLevel)
    // read the exact same StationExtension.m_maxStationDistance field, so stretching that field -
    // rather than bypassing the check outright - keeps the bonus working at the new range instead
    // of the piece becoming purely decorative.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.otextensionreach";
        public const string PluginName = "OtExtensionReach";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<float> MaxStationDistanceMultiplier;
        internal static ConfigEntry<bool> DisableSpaceRequirement;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            MaxStationDistanceMultiplier = Config.Bind(
                "General",
                "MaxStationDistanceMultiplier",
                1f,
                new ConfigDescription(
                    "Multiplies how far a crafting station upgrade can be from its station and still " +
                    "(a) be placeable there and (b) count toward the station's level bonus - both use " +
                    "the same range, so raising this keeps the bonus working at the new distance " +
                    "instead of making the piece purely decorative. 1 = vanilla range. Applies " +
                    "immediately, including to upgrades already built.",
                    new AcceptableValueRange<float>(1f, 20f)));

            DisableSpaceRequirement = Config.Bind(
                "General",
                "DisableSpaceRequirement",
                false,
                "Lets crafting station upgrades be placed right next to each other instead of needing " +
                "clear space between them. Purely a placement-UI restriction - has no effect on " +
                "whether the station's level bonus applies.");

            MaxStationDistanceMultiplier.SettingChanged += (_, _) => StationExtensionCaps.ReapplyAll();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
