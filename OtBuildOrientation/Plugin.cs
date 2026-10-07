using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OtBuildOrientation
{
    // Vanilla only ever rotates a build piece around the vertical axis (scroll wheel, 22.5° steps),
    // so a beam can't lie on its side and stairs can't hang upside down. This adds one more control
    // on top: hold ModifierKey and scroll to cycle which face of the piece rests downward (upright,
    // on its left side, upside down, on its right side, face down, face up). Combined with vanilla's
    // spin that covers every 90° orientation. The orientation is folded into the same rotation
    // vanilla builds the placement ghost from, so snapping, manual snap-point cycling and the
    // placed piece itself all pick it up without any changes of their own.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tlisonbee.valheim.otbuildorientation";
        public const string PluginName = "OtBuildOrientation";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyCode> ModifierKey;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ModifierKey = Config.Bind(
                "General",
                "ModifierKey",
                KeyCode.LeftAlt,
                "Hold this and scroll while placing a build piece to cycle which face of the piece " +
                "rests downward. Scrolling without it keeps vanilla's spin around the vertical axis. " +
                "Avoid LeftShift (vanilla's 'place without snapping') and LeftControl (crouch).");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        internal static bool IsModifierHeld() => ZInput.GetKey(ModifierKey.Value, false);
    }
}
