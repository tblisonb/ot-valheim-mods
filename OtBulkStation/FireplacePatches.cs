using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OtBulkStation
{
    // Fireplace covers every fuel-burning light source: campfire, hearth, bonfire, braziers,
    // sconces, and standing/wall torches - they all share this one component. Unlike Smelter
    // and CookingStation, there's no separate Switch for input; refueling lives directly in
    // Interact(), alongside this component's on/off toggle (for the stations that have one).
    internal static class FireplaceCaps
    {
        internal static void ApplyFuelCap(Fireplace fireplace)
        {
            var name = Utils.GetPrefabName(fireplace.gameObject);
            if (Plugin.FuelCapacityOverrides.TryGetValue(name, out var fuel))
            {
                fireplace.m_maxFuel = fuel;
            }
        }
    }

    [HarmonyPatch(typeof(Fireplace), "Awake")]
    internal static class Fireplace_Awake_Patch
    {
        private static void Postfix(Fireplace __instance) => FireplaceCaps.ApplyFuelCap(__instance);
    }

    [HarmonyPatch(typeof(Fireplace), "GetHoverText")]
    internal static class Fireplace_GetHoverText_Patch
    {
        private static void Prefix(Fireplace __instance) => FireplaceCaps.ApplyFuelCap(__instance);

        private static void Postfix(Fireplace __instance, ref string __result)
        {
            if (__instance.m_canRefill && !__instance.m_infiniteFuel && !__instance.m_canTurnOff && __result.Length > 0)
            {
                __result += Plugin.InsertHint();
            }
        }
    }

    // Batches fuel input the same technique as the Smelter/CookingStation patches: re-invoke
    // the game's own Interact() as many times as the held modifiers call for. There's no
    // GetFuel()-style accessor to compute exact remaining room here, so this just uses the
    // station's capacity as an upper bound on the loop - Interact() already refuses and
    // messages "can't add more" once actually full, which naturally stops the loop early.
    [HarmonyPatch(typeof(Fireplace), "Interact")]
    internal static class Fireplace_Interact_Patch
    {
        private static readonly MethodInfo Original = AccessTools.Method(typeof(Fireplace), "Interact");

        [ThreadStatic] private static bool _busy;

        private static bool Prefix(Fireplace __instance, Humanoid user, bool hold, bool alt, ref bool __result)
        {
            // m_canTurnOff fireplaces treat a plain tap as an on/off toggle rather than a
            // refuel whenever there's still fuel left, with no cheap way from here to tell
            // which branch a given call would take. Re-invoking blindly could toggle the
            // fire on/off repeatedly instead of refueling, so such stations are left alone.
            if (_busy || (object)user != Player.m_localPlayer || !__instance.m_canRefill ||
                __instance.m_infiniteFuel || __instance.m_canTurnOff)
            {
                return true;
            }

            FireplaceCaps.ApplyFuelCap(__instance);
            var room = Mathf.CeilToInt(__instance.m_maxFuel);
            var count = Plugin.ResolveInsertCount(room);
            if (count <= 1)
            {
                return true;
            }

            _busy = true;
            var any = false;
            try
            {
                for (var i = 0; i < count; i++)
                {
                    if (!(bool)Original.Invoke(__instance, new object[] { user, hold, alt }))
                    {
                        break;
                    }

                    any = true;
                }
            }
            finally
            {
                _busy = false;
            }

            __result = any;
            return false;
        }
    }
}
