using HarmonyLib;

namespace OtInventoryLayout
{
    // Inventory.FindEmptySlot(topFirst) is private, but it's a plain scanning method - no
    // MonoBehaviour/Unity involvement - so a full-replacement Prefix is a direct, low-risk swap:
    // skipped entirely (returning true) unless StackPlacementMode is set to something other than
    // Vanilla, in which case it reimplements the scan ignoring topFirst, in strict linear slot
    // order (x then y, row by row) either forwards or backwards. That linear order is exactly the
    // order DisplayColumnOverrides reflows a container's slots in (see ContainerReflowPatches.cs),
    // so "first/last empty slot" already means the same thing here as "first/last" in a reflowed
    // container's on-screen grid, with no need for this patch to know about the reflow at all.
    [HarmonyPatch(typeof(Inventory), "FindEmptySlot")]
    internal static class Inventory_FindEmptySlot_Patch
    {
        private static bool Prefix(Inventory __instance, ref Vector2i __result)
        {
            var mode = Plugin.StackPlacementModeConfig.Value;
            if (mode == StackPlacementMode.Vanilla)
            {
                return true;
            }

            var width = __instance.GetWidth();
            var height = __instance.GetHeight();
            var totalCells = width * height;
            var forward = mode == StackPlacementMode.FirstEmptySlot;

            for (var step = 0; step < totalCells; step++)
            {
                var i = forward ? step : totalCells - 1 - step;
                var x = i % width;
                var y = i / width;
                if (__instance.GetItemAt(x, y) == null)
                {
                    __result = new Vector2i(x, y);
                    return false;
                }
            }

            __result = new Vector2i(-1, -1);
            return false;
        }
    }
}
