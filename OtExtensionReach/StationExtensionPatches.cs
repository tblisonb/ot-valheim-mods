using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace OtExtensionReach
{
    // StationExtension.m_maxStationDistance drives both the "need station nearby" placement check
    // and the actual level-bonus range check (CraftingStation.GetExtensions), so scaling this one
    // field extends both consistently - no need to touch either check's logic.
    internal static class StationExtensionCaps
    {
        private static readonly FieldInfo AllExtensionsField =
            AccessTools.Field(typeof(StationExtension), "m_allExtensions");

        // Remembers each extension's original (prefab-authored) distance so the multiplier can be
        // changed repeatedly at runtime without compounding, and so upgrade pieces that ship with
        // different base ranges keep their relative differences.
        private static readonly ConditionalWeakTable<StationExtension, object> BaseDistance = new ConditionalWeakTable<StationExtension, object>();

        internal static void Apply(StationExtension extension)
        {
            if (!BaseDistance.TryGetValue(extension, out var boxedBase))
            {
                boxedBase = extension.m_maxStationDistance;
                BaseDistance.Add(extension, boxedBase);
            }

            extension.m_maxStationDistance = (float)boxedBase * Plugin.MaxStationDistanceMultiplier.Value;
        }

        internal static void ReapplyAll()
        {
            if (AllExtensionsField.GetValue(null) is List<StationExtension> allExtensions)
            {
                foreach (var extension in allExtensions)
                {
                    if (extension != null)
                    {
                        Apply(extension);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StationExtension), "Awake")]
    internal static class StationExtension_Awake_Patch
    {
        private static void Postfix(StationExtension __instance) => StationExtensionCaps.Apply(__instance);
    }

    // The "needs more space" placement block - purely cosmetic UI gating, no effect on the bonus.
    [HarmonyPatch(typeof(StationExtension), "OtherExtensionInRange")]
    internal static class StationExtension_OtherExtensionInRange_Patch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!Plugin.DisableSpaceRequirement.Value)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
