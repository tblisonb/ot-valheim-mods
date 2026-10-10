using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OtBuildOrientation
{
    // Vanilla only ever rotates a build piece around the vertical axis (scroll wheel, 22.5° steps),
    // so a beam can't lie on its side and stairs can't hang upside down. This adds one more control
    // on top: hold ModifierKey and scroll to cycle how the piece lies (upright, on its side, flat),
    // and press FlipKey to turn it over onto the opposite face (upside down, on its other side,
    // face up). Combined with vanilla's spin that covers every 90° orientation. The orientation is
    // folded into the same rotation vanilla builds the placement ghost from, so snapping, manual
    // snap-point cycling and the placed piece itself all pick it up without any changes of their
    // own. Vanilla's piece copy (LeftShift + remove) is taught to copy the orientation too (see
    // CopyPatches.cs).
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.otbuildorientation";
        public const string PluginName = "OtBuildOrientation";
        public const string PluginVersion = "1.1.1";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyCode> ModifierKey;
        internal static ConfigEntry<KeyCode> FlipKey;
        internal static ConfigEntry<KeyCode> FlipModifierKey;
        internal static ConfigEntry<bool> FlipHorizontally;
        internal static ConfigEntry<bool> CopyOrientation;
        internal static ConfigEntry<bool> KeepOrientation;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ModifierKey = Config.Bind(
                "General",
                "ModifierKey",
                KeyCode.LeftAlt,
                "Hold this and scroll while placing a build piece to cycle how it lies: upright, on " +
                "its side, flat. Scrolling without it keeps vanilla's spin around the vertical axis. " +
                "Avoid LeftShift (vanilla's 'place without snapping') and LeftControl (crouch).");

            FlipKey = Config.Bind(
                "General",
                "FlipKey",
                KeyCode.F,
                "Press this while placing a build piece to turn it over onto its opposite face: " +
                "upside down, on its other side, or face up. Other keys held at the same time (e.g. " +
                "ModifierKey) don't matter. While building, this keypress doesn't trigger the " +
                "forsaken power (vanilla F); put the build tool away to use it.");

            FlipModifierKey = Config.Bind(
                "General",
                "FlipModifierKey",
                KeyCode.None,
                "If set, it must be held together with FlipKey to flip. Only F (or whatever you " +
                "set FlipKey to) with this key held is kept from triggering the forsaken power, so " +
                "e.g. LeftShift leaves plain F free for it while building.");

            FlipHorizontally = Config.Bind(
                "General",
                "FlipHorizontally",
                false,
                "Off: flipping rolls the piece over end to end, keeping the same face toward you, so " +
                "a sloped wall's slope keeps its direction. On: it also turns the piece around, so " +
                "the slope runs the other way and you see its other face. Either is one 180° spin " +
                "away from the other.");

            CopyOrientation = Config.Bind(
                "General",
                "CopyOrientation",
                true,
                "When copying a built piece with vanilla's LeftShift + remove (middle mouse), also copy " +
                "which face of it rests downward, so a flipped piece is picked up flipped. Off: vanilla " +
                "behavior, which copies only the spin (and gets it wrong for a flipped piece).");

            KeepOrientation = Config.Bind(
                "General",
                "KeepOrientation",
                false,
                "Keep the chosen orientation when selecting a different build piece, instead of " +
                "resetting to upright. Pieces that can't be oriented are placed as usual, and the " +
                "orientation comes back on the next one that can.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal static bool IsModifierHeld() => ZInput.GetKey(ModifierKey.Value, false);

        internal static bool IsFlipPressed() =>
            ZInput.GetKeyDown(FlipKey.Value, false) &&
            (FlipModifierKey.Value == KeyCode.None || ZInput.GetKey(FlipModifierKey.Value, false));

    }
}
