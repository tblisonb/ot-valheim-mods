using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BulkStation
{
    // Smelter covers the smelter, blast furnace, charcoal kiln, and eitr refinery - they all
    // share this one component, just with different conversions/capacities set on the prefab.
    internal static class SmelterCaps
    {
        internal static void Apply(Smelter smelter)
        {
            var name = Utils.GetPrefabName(smelter.gameObject);
            if (Plugin.OreCapacityOverrides.TryGetValue(name, out var ore))
            {
                smelter.m_maxOre = ore;
            }

            if (smelter.m_fuelItem != null && Plugin.FuelCapacityOverrides.TryGetValue(name, out var fuel))
            {
                smelter.m_maxFuel = fuel;
            }
        }
    }

    [HarmonyPatch(typeof(Smelter), "Awake")]
    internal static class Smelter_Awake_Patch
    {
        private static void Postfix(Smelter __instance) => SmelterCaps.Apply(__instance);
    }

    [HarmonyPatch(typeof(Smelter), "OnHoverAddOre")]
    internal static class Smelter_OnHoverAddOre_Patch
    {
        private static void Prefix(Smelter __instance) => SmelterCaps.Apply(__instance);

        private static void Postfix(ref string __result) => __result += Plugin.InsertHint();
    }

    [HarmonyPatch(typeof(Smelter), "OnHoverAddFuel")]
    internal static class Smelter_OnHoverAddFuel_Patch
    {
        private static void Prefix(Smelter __instance) => SmelterCaps.Apply(__instance);

        private static void Postfix(ref string __result) => __result += Plugin.InsertHint();
    }

    // Batches ore/wood input: re-invokes the game's own OnAddOre as many times as the held
    // modifiers call for, instead of reimplementing its item-lookup/removal/message logic.
    [HarmonyPatch(typeof(Smelter), "OnAddOre")]
    internal static class Smelter_OnAddOre_Patch
    {
        private static readonly MethodInfo Original = AccessTools.Method(typeof(Smelter), "OnAddOre");
        private static readonly Func<Smelter, int> GetQueueSize =
            AccessTools.MethodDelegate<Func<Smelter, int>>(AccessTools.Method(typeof(Smelter), "GetQueueSize"));

        // Guards against the manual re-invocations below re-entering this same prefix.
        [ThreadStatic] private static bool _busy;

        private static bool Prefix(Smelter __instance, Switch sw, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (_busy || item != null || (object)user != Player.m_localPlayer)
            {
                return true;
            }

            SmelterCaps.Apply(__instance);
            var room = __instance.m_maxOre - GetQueueSize(__instance);
            var count = Plugin.ResolveInsertCount(room);
            if (count <= 1)
            {
                return true; // vanilla single-press (or already-full) behavior, incl. its messages
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

    // Batches fuel (coal) input the same way.
    [HarmonyPatch(typeof(Smelter), "OnAddFuel")]
    internal static class Smelter_OnAddFuel_Patch
    {
        private static readonly MethodInfo Original = AccessTools.Method(typeof(Smelter), "OnAddFuel");
        private static readonly Func<Smelter, float> GetFuel =
            AccessTools.MethodDelegate<Func<Smelter, float>>(AccessTools.Method(typeof(Smelter), "GetFuel"));

        [ThreadStatic] private static bool _busy;

        private static bool Prefix(Smelter __instance, Switch sw, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (_busy || item != null || (object)user != Player.m_localPlayer)
            {
                return true;
            }

            SmelterCaps.Apply(__instance);
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
}
