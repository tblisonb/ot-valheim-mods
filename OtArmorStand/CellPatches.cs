using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OtArmorStand
{
    // Dresses the container grid for a stand: cells with no slot (the holes in the body layout)
    // are hidden, and empty slot cells show a faded icon of what goes there plus a tooltip naming
    // it. InventoryGrid only rebuilds its cells when the grid's size changes, and the container
    // grid is shared by every chest, so whatever is changed here is undone as soon as the grid
    // shows another inventory, before it's drawn.
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class CellPatches
    {
        private const string GhostName = "OtArmorStand_Ghost";

        private static readonly AccessTools.FieldRef<InventoryGrid, List<InventoryElement>> Elements =
            AccessTools.FieldRefAccess<InventoryGrid, List<InventoryElement>>("m_elements");

        private static readonly HashSet<InventoryGrid> Dressed = new HashSet<InventoryGrid>();
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();

        private static void Postfix(InventoryGrid __instance)
        {
            var session = StandSession.Current;
            if (session == null || __instance.GetInventory() != session.Inventory)
            {
                if (Dressed.Remove(__instance))
                {
                    Undress(__instance);
                }
                return;
            }

            Dressed.Add(__instance);
            foreach (InventoryElement element in Elements(__instance))
            {
                ArmorStand.ArmorStandSlot slot = session.SlotAt(element.Position);
                if (element.gameObject.activeSelf != (slot != null))
                {
                    element.gameObject.SetActive(slot != null);
                }
                if (slot == null)
                {
                    continue;
                }
                Image ghost = Ghost(element, create: true);
                if (element.m_used)
                {
                    ghost.enabled = false;
                    continue;
                }
                Describe(slot, out string prefab, out string label);
                ghost.sprite = Icon(prefab);
                ghost.enabled = ghost.sprite != null;
                // Read every frame so edits through a config manager show up live.
                float brightness = Mathf.Clamp01(Plugin.PlaceholderBrightness.Value);
                ghost.color = new Color(brightness, brightness, brightness, Mathf.Clamp01(Plugin.PlaceholderOpacity.Value));
                element.m_tooltip.m_topic = label;
            }
        }

        private static void Undress(InventoryGrid grid)
        {
            foreach (InventoryElement element in Elements(grid))
            {
                if (element == null)
                {
                    continue;
                }
                element.gameObject.SetActive(true);
                Image ghost = Ghost(element, create: false);
                if (ghost != null)
                {
                    Object.Destroy(ghost.gameObject);
                }
            }
        }

        // A copy of the cell's icon rect, just behind the icon and not catching clicks.
        private static Image Ghost(InventoryElement element, bool create)
        {
            Transform parent = element.m_icon.transform.parent;
            Transform existing = parent.Find(GhostName);
            if (existing != null || !create)
            {
                return existing != null ? existing.GetComponent<Image>() : null;
            }
            var go = new GameObject(GhostName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            var iconRect = element.m_icon.rectTransform;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(iconRect.GetSiblingIndex());
            rect.anchorMin = iconRect.anchorMin;
            rect.anchorMax = iconRect.anchorMax;
            rect.pivot = iconRect.pivot;
            rect.sizeDelta = iconRect.sizeDelta;
            rect.anchoredPosition = iconRect.anchoredPosition;
            rect.localScale = iconRect.localScale;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        // Which vanilla item's icon stands in for the slot, and its name.
        private static void Describe(ArmorStand.ArmorStandSlot slot, out string prefab, out string label)
        {
            switch (slot.m_slot)
            {
                case VisSlot.Helmet: prefab = "HelmetBronze"; label = "Helmet"; return;
                case VisSlot.Chest: prefab = "ArmorBronzeChest"; label = "Chest"; return;
                case VisSlot.Legs: prefab = "ArmorBronzeLegs"; label = "Legs"; return;
                case VisSlot.Shoulder: prefab = "CapeDeerHide"; label = "Cape"; return;
                case VisSlot.Utility: prefab = "BeltStrength"; label = "Belt"; return;
            }
            if (slot.m_supportedTypes.Contains(ItemDrop.ItemData.ItemType.Shield))
            {
                prefab = "ShieldWood";
                label = "Shield";
            }
            else
            {
                prefab = "SwordBronze";
                label = "Weapon";
            }
        }

        private static Sprite Icon(string prefab)
        {
            if (Icons.TryGetValue(prefab, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }
            GameObject go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            ItemDrop drop = go != null ? go.GetComponent<ItemDrop>() : null;
            sprite = drop != null ? Grayscale.Sprite(drop.m_itemData.GetIcon()) : null;
            if (sprite == null)
            {
                Plugin.Log.LogWarning($"No icon for slot placeholder {prefab}; that slot's cell stays blank.");
            }
            Icons[prefab] = sprite;
            return sprite;
        }
    }
}
