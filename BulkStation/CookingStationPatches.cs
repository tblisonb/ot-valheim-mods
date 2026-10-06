using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BulkStation
{
    // CookingStation covers the cooking station, iron cooking station, and stone oven. Food
    // items each occupy one discrete slot (m_slots) while they cook, so "batching" input means
    // filling multiple free slots in one press rather than stacking a quantity into one slot
    // the way a smelter's ore queue does.
    internal static class CookingStationCaps
    {
        internal static void ApplyFuelCap(CookingStation station)
        {
            if (!station.m_useFuel)
            {
                return;
            }

            var name = Utils.GetPrefabName(station.gameObject);
            if (Plugin.FuelCapacityOverrides.TryGetValue(name, out var fuel))
            {
                station.m_maxFuel = fuel;
            }
        }
    }

    [HarmonyPatch(typeof(CookingStation), "Awake")]
    internal static class CookingStation_Awake_Patch
    {
        private static void Postfix(CookingStation __instance) => CookingStationCaps.ApplyFuelCap(__instance);
    }

    [HarmonyPatch(typeof(CookingStation), "OnHoverFuelSwitch")]
    internal static class CookingStation_OnHoverFuelSwitch_Patch
    {
        private static void Prefix(CookingStation __instance) => CookingStationCaps.ApplyFuelCap(__instance);

        private static void Postfix(ref string __result) => __result += Plugin.InsertHint();
    }

    // Batches fuel input, same technique as the Smelter patches.
    [HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch")]
    internal static class CookingStation_OnAddFuelSwitch_Patch
    {
        private static readonly MethodInfo Original = AccessTools.Method(typeof(CookingStation), "OnAddFuelSwitch");
        private static readonly Func<CookingStation, float> GetFuel =
            AccessTools.MethodDelegate<Func<CookingStation, float>>(AccessTools.Method(typeof(CookingStation), "GetFuel"));

        [ThreadStatic] private static bool _busy;

        private static bool Prefix(CookingStation __instance, Switch sw, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (_busy || item != null || (object)user != Player.m_localPlayer || !__instance.m_useFuel)
            {
                return true;
            }

            CookingStationCaps.ApplyFuelCap(__instance);
            var room = Mathf.FloorToInt(__instance.m_maxFuel - GetFuel(__instance));
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
                    if (!(bool)Original.Invoke(__instance, new object[] { sw, user, null }))
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

    // Adds a hint to the tooltip the first time it's shown, and a "collect all" hint once food
    // is ready. HoverText() is private and refreshed every second by the station itself, and
    // feeds both the no-switch hover text and the add-food switch's hover text.
    [HarmonyPatch(typeof(CookingStation), "HoverText")]
    internal static class CookingStation_HoverText_Patch
    {
        private static readonly Func<CookingStation, bool> HaveDoneItem =
            AccessTools.MethodDelegate<Func<CookingStation, bool>>(AccessTools.Method(typeof(CookingStation), "HaveDoneItem"));

        private static void Postfix(CookingStation __instance, ref string __result)
        {
            __result += HaveDoneItem(__instance) ? Plugin.CollectAllHint() : Plugin.InsertHint();
        }
    }

    // OnInteract is a dual-purpose entry point: if anything is done cooking, it collects one
    // slot's worth and returns; otherwise it finds a cookable item in the player's inventory
    // and adds it to one free slot. Both branches normally only act once per press - this
    // re-invokes OnInteract repeatedly while the modifier is held, draining every done slot in
    // the first case, or filling every free slot the inventory can supply in the second.
    [HarmonyPatch(typeof(CookingStation), "OnInteract")]
    internal static class CookingStation_OnInteract_Patch
    {
        private static readonly MethodInfo Original = AccessTools.Method(typeof(CookingStation), "OnInteract");
        private static readonly Func<CookingStation, bool> HaveDoneItem =
            AccessTools.MethodDelegate<Func<CookingStation, bool>>(AccessTools.Method(typeof(CookingStation), "HaveDoneItem"));

        [ThreadStatic] private static bool _busy;

        private static bool Prefix(CookingStation __instance, Humanoid user, ref bool __result)
        {
            if (_busy || (object)user != Player.m_localPlayer || !Plugin.IsBatchHeld())
            {
                return true;
            }

            if (HaveDoneItem(__instance))
            {
                // Collect branch: drain every slot that's currently done. m_slots is small (a
                // handful at most), so the guard just prevents a runaway loop, not a real cap.
                __result = RunLoop(__instance, user, () => HaveDoneItem(__instance), guard: 64);
                return false;
            }

            // Insert branch: there's no exact "free slot count" accessor to read from here, so
            // m_slots.Length is used as a loop upper bound - OnInteract's own "no room"/"nothing
            // cookable" checks naturally stop the loop once it's actually out of slots or items.
            var room = __instance.m_slots.Length;
            var count = Plugin.ResolveInsertCount(room);
            if (count <= 1)
            {
                return true;
            }

            __result = RunLoop(__instance, user, null, guard: count);
            return false;
        }

        // Re-invokes the original OnInteract up to `guard` times, stopping early if it ever
        // returns false (nothing left to do) or, when `keepGoing` is given, once that turns false.
        private static bool RunLoop(CookingStation instance, Humanoid user, Func<bool> keepGoing, int guard)
        {
            _busy = true;
            var any = false;
            try
            {
                var i = 0;
                while ((keepGoing?.Invoke() ?? true) && i++ < guard)
                {
                    if (!(bool)Original.Invoke(instance, new object[] { user }))
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

            return any;
        }
    }
}
