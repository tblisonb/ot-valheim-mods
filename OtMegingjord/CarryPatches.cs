using HarmonyLib;

namespace OtMegingjord
{
    // The belt's +150 is a fixed SE_Stats.m_addMaxCarryWeight on its equip status effect, and vanilla
    // applies equip effects without the item's quality (Humanoid adds them with itemLevel 0). So the
    // bonus is topped up here from the quality of the belt actually worn, and its tooltip, which
    // reads the same field, shows the value for the quality being looked at.
    internal static class Belt
    {
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> UtilityItem =
            AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_utilityItem");

        private const string BeltName = "$item_beltstrength";

        internal static bool IsBelt(ItemDrop.ItemData item) => item != null && item.m_shared.m_name == BeltName;

        internal static bool IsBeltEffect(StatusEffect effect, out ItemDrop.ItemData wornBelt)
        {
            wornBelt = null;
            if (!(effect.m_character is Humanoid humanoid))
            {
                return false;
            }
            var utility = UtilityItem(humanoid);
            if (!IsBelt(utility) || utility.m_shared.m_equipStatusEffect == null ||
                utility.m_shared.m_equipStatusEffect.NameHash() != effect.NameHash())
            {
                return false;
            }
            wornBelt = utility;
            return true;
        }

        // Set by StatusEffect.SetLevel, which the item tooltip calls on the belt's (prefab) effect
        // with the quality being shown just before asking it for its tooltip text.
        internal static int TooltipQuality;
        internal static StatusEffect TooltipEffect;
    }

    [HarmonyPatch(typeof(SE_Stats), nameof(SE_Stats.ModifyMaxCarryWeight))]
    internal static class ModifyMaxCarryWeightPatch
    {
        private static void Postfix(SE_Stats __instance, ref float limit)
        {
            if (Belt.IsBeltEffect(__instance, out var belt) && belt.m_quality > 1)
            {
                float vanilla = __instance.m_addMaxCarryWeight;
                limit += Plugin.CarryBonusAt(belt.m_quality, vanilla) - vanilla;
            }
        }
    }

    [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.SetLevel))]
    internal static class SetLevelPatch
    {
        private static void Postfix(StatusEffect __instance, int itemLevel)
        {
            Belt.TooltipEffect = __instance;
            Belt.TooltipQuality = itemLevel;
        }
    }

    [HarmonyPatch(typeof(SE_Stats), nameof(SE_Stats.GetTooltipString))]
    internal static class GetTooltipStringPatch
    {
        private static void Prefix(SE_Stats __instance, out float __state)
        {
            __state = __instance.m_addMaxCarryWeight;
            if (Belt.TooltipEffect == __instance && Belt.TooltipQuality > 1 && IsBeltPrefabEffect(__instance))
            {
                __instance.m_addMaxCarryWeight = Plugin.CarryBonusAt(Belt.TooltipQuality, __state);
            }
        }

        private static void Postfix(SE_Stats __instance, float __state)
        {
            __instance.m_addMaxCarryWeight = __state;
            Belt.TooltipEffect = null;
        }

        private static bool IsBeltPrefabEffect(StatusEffect effect)
        {
            var prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(Tiers.BeltPrefab) : null;
            var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null && drop.m_itemData.m_shared.m_equipStatusEffect == effect;
        }
    }
}
