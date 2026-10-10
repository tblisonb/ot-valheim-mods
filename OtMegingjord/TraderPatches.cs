using System.Collections.Generic;
using HarmonyLib;

namespace OtMegingjord
{
    // Haldor's stock is a list on his Trader component, filtered each time his shop opens by
    // GetAvailableItems (world key required, one-time items already bought). Appending to that
    // filtered list, rather than editing his prefab's list, keeps the change local to this
    // player's view of the shop and picks up config changes (prices, keys) without a restart.
    [HarmonyPatch(typeof(Trader), nameof(Trader.GetAvailableItems))]
    internal static class TraderGetAvailableItemsPatch
    {
        private const string HaldorPrefab = "Haldor";

        private static readonly Dictionary<int, Trader.TradeItem> TradeItems = new Dictionary<int, Trader.TradeItem>();

        private static void Postfix(Trader __instance, List<Trader.TradeItem> __result)
        {
            if (Utils.GetPrefabName(__instance.gameObject.name) != HaldorPrefab || ZoneSystem.instance == null)
            {
                return;
            }

            foreach (var tier in Tiers.All)
            {
                if (!Items.ByLevel.TryGetValue(tier.Level, out var item))
                {
                    continue;
                }
                var config = Plugin.TierConfigs[tier.Level];
                string key = config.UnlockKey.Value.Trim();
                if (key.Length > 0 && !ZoneSystem.instance.GetGlobalKey(key))
                {
                    continue;
                }

                if (!TradeItems.TryGetValue(tier.Level, out var tradeItem))
                {
                    // Unity fills a serialized TradeItem's strings with "", and StoreGui relies on it
                    // (m_tooltip.Length); one built in code has nulls there instead.
                    tradeItem = new Trader.TradeItem
                    {
                        m_prefab = item,
                        m_stack = 1,
                        m_requiredGlobalKey = "",
                        m_name = "",
                        m_tooltip = "",
                        m_buyKey = "",
                        m_incrementKey = "",
                    };
                    TradeItems[tier.Level] = tradeItem;
                }
                tradeItem.m_price = config.Price.Value;
                __result.Add(tradeItem);
            }
        }
    }
}
