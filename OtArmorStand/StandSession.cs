using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OtArmorStand
{
    internal enum StandAction
    {
        Open,
        Swap,
    }

    // A stand's slots mirrored into a temporary Inventory, so the vanilla container panel can show
    // and edit them. The stand's ZDO stays the only real storage: every frame, cells whose item
    // changed are written back to the stand with vanilla's own keys (<slot>_item, <slot>_variant,
    // <slot>_itemData) and visual RPC, exactly as vanilla's hotbar attach does. That only works
    // while this client owns the stand's ZDO, so a session is only created after ownership is
    // confirmed, and ownership isn't handed away while the panel is open (see RequestOwnPatch).
    internal class StandSession
    {
        private const int Width = 5;
        private const float OwnershipTimeout = 2f;

        // Cell order for the 5-wide panel: armor across the top row, hands and back below.
        // Slots not listed here (another mod's stand, say) take the remaining cells in order.
        private static readonly VisSlot[] CellOrder =
        {
            VisSlot.Helmet, VisSlot.Chest, VisSlot.Legs, VisSlot.Shoulder, VisSlot.Utility,
            VisSlot.HandRight, VisSlot.HandLeft, VisSlot.BackLeft, VisSlot.BackRight,
        };

        private static readonly VisSlot[] ArmorSlots =
        {
            VisSlot.Helmet, VisSlot.Chest, VisSlot.Legs, VisSlot.Shoulder, VisSlot.Utility,
        };

        private static readonly AccessTools.FieldRef<ArmorStand, ZNetView> StandView =
            AccessTools.FieldRefAccess<ArmorStand, ZNetView>("m_nview");
        private static readonly AccessTools.FieldRef<Container, ZNetView> ContainerView =
            AccessTools.FieldRefAccess<Container, ZNetView>("m_nview");
        private static readonly AccessTools.FieldRef<Container, Inventory> ContainerInventory =
            AccessTools.FieldRefAccess<Container, Inventory>("m_inventory");
        private static readonly MethodInfo UpdateSupports = AccessTools.Method(typeof(ArmorStand), "UpdateSupports");

        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> HelmetItem =
            AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_helmetItem");
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> ChestItem =
            AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_chestItem");
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> LegItem =
            AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_legItem");
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> ShoulderItem =
            AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_shoulderItem");
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> UtilityItem =
            AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_utilityItem");

        // The session whose panel is showing, if any.
        internal static StandSession Current;

        private static ArmorStand _pendingStand;
        private static StandAction _pendingAction;
        private static float _pendingDeadline;

        internal readonly ArmorStand Stand;
        internal readonly ZNetView View;
        internal readonly Inventory Inventory;
        internal readonly Container Stub;

        private readonly Vector2i[] _cellOfSlot;
        private readonly int[] _slotAtCell;
        private readonly ItemDrop.ItemData[] _written;
        // Slots holding an item whose prefab isn't known here (from a mod this client lacks). They're
        // left alone rather than mirrored, so writing back can't clear them.
        private readonly bool[] _locked;

        private StandSession(ArmorStand stand)
        {
            Stand = stand;
            View = StandView(stand);

            int slotCount = stand.m_slots.Count;
            int cellCount = Math.Max(CellOrder.Length, slotCount);
            int height = (cellCount + Width - 1) / Width;
            _cellOfSlot = new Vector2i[slotCount];
            _slotAtCell = new int[Width * height];
            for (int i = 0; i < _slotAtCell.Length; i++)
            {
                _slotAtCell[i] = -1;
            }
            var placed = new bool[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                int cell = Array.IndexOf(CellOrder, stand.m_slots[i].m_slot);
                if (cell >= 0 && _slotAtCell[cell] < 0)
                {
                    Place(i, cell);
                    placed[i] = true;
                }
            }
            for (int i = 0; i < slotCount; i++)
            {
                if (!placed[i])
                {
                    Place(i, Array.IndexOf(_slotAtCell, -1));
                }
            }

            // m_bkg is only decoration, and InventoryGui never reads it for containers.
            Inventory = new Inventory(stand.m_name, null, Width, height);
            _written = new ItemDrop.ItemData[slotCount];
            _locked = new bool[slotCount];
            ZDO zdo = View.GetZDO();
            for (int i = 0; i < slotCount; i++)
            {
                int hash = zdo.GetInt(ItemKey(i));
                if (hash == 0)
                {
                    continue;
                }
                GameObject prefab = ObjectDB.instance.GetItemPrefab(hash);
                if (prefab == null)
                {
                    _locked[i] = true;
                    continue;
                }
                ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                item.m_dropPrefab = prefab;
                item.m_stack = 1;
                ItemDrop.LoadFromZDO(item, zdo, i);
                Inventory.AddItem(item, _cellOfSlot[i]);
                _written[i] = item;
            }

            // InventoryGui.Show wants a Container. This one stays inactive so its Awake never runs:
            // no RPCs registered on the stand's ZNetView, no ZDO save of its own, no drop-on-destroy,
            // and it's invisible to FindObjectsOfType, so other mods scanning for chests (quick
            // stacking, crafting from containers) don't see it. Parenting it to the stand closes the
            // panel if the stand is destroyed, and keeps the panel's distance check working.
            var go = new GameObject("OtArmorStand_Panel");
            go.SetActive(false);
            go.transform.SetParent(stand.transform, false);
            Stub = go.AddComponent<Container>();
            Stub.m_name = stand.m_name;
            ContainerView(Stub) = View;
            ContainerInventory(Stub) = Inventory;
        }

        private void Place(int slot, int cell)
        {
            _slotAtCell[cell] = slot;
            _cellOfSlot[slot] = new Vector2i(cell % Width, cell / Width);
        }

        private static int ItemKey(int slot) => (slot + "_item").GetStableHashCode();

        private static int VariantKey(int slot) => (slot + "_variant").GetStableHashCode();

        internal static ZNetView ViewOf(ArmorStand stand) => StandView(stand);

        // Asks for ownership of the stand (vanilla's RPC_RequestOwn, which every client has) and
        // runs the action once it arrives; UpdatePending polls for it.
        internal static void Request(ArmorStand stand, StandAction action)
        {
            ZNetView view = StandView(stand);
            if (view == null || !view.IsValid())
            {
                return;
            }
            if (!view.IsOwner())
            {
                view.InvokeRPC("RPC_RequestOwn");
            }
            _pendingStand = stand;
            _pendingAction = action;
            _pendingDeadline = Time.time + OwnershipTimeout;
            UpdatePending();
        }

        internal static void UpdatePending()
        {
            if (_pendingStand == null)
            {
                return;
            }
            ZNetView view = StandView(_pendingStand);
            if (view == null || !view.IsValid() || Player.m_localPlayer == null)
            {
                _pendingStand = null;
                return;
            }
            if (view.IsOwner())
            {
                ArmorStand stand = _pendingStand;
                _pendingStand = null;
                Run(stand, _pendingAction);
            }
            else if (Time.time > _pendingDeadline)
            {
                // The owner refused (it has the panel open) or didn't answer.
                _pendingStand = null;
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
            }
        }

        private static void Run(ArmorStand stand, StandAction action)
        {
            Player player = Player.m_localPlayer;
            if (Current != null && Current.Stand == stand)
            {
                if (action == StandAction.Swap)
                {
                    Current.Swap(player);
                    Current.Sync();
                }
                return;
            }

            var session = new StandSession(stand);
            if (action == StandAction.Open)
            {
                Close();
                Current = session;
                InventoryGui.instance.Show(session.Stub);
            }
            else
            {
                session.Swap(player);
                session.Sync();
                session.Dispose();
            }
        }

        // Called every frame from InventoryGui.Update: writes changes back, and ends the session
        // once the panel shows something else or the stand is gone.
        internal static void Tick(Container shown)
        {
            if (Current == null)
            {
                return;
            }
            if (Current.Stand == null || !Current.View.IsValid())
            {
                Current = null;
                return;
            }
            Current.Sync();
            if (shown != Current.Stub)
            {
                Current.Dispose();
                Current = null;
            }
            else if (!Current.View.IsOwner())
            {
                // Shouldn't happen while RequestOwnPatch holds the stand, but if ownership moved
                // anyway, edits here could no longer be saved.
                InventoryGui.instance.Hide();
            }
        }

        internal static void Close()
        {
            if (Current == null)
            {
                return;
            }
            if (Current.Stand != null && Current.View.IsValid())
            {
                Current.Sync();
            }
            Current.Dispose();
            Current = null;
        }

        private void Dispose()
        {
            if (Stub != null)
            {
                Object.Destroy(Stub.gameObject);
            }
        }

        internal bool IsCellOf(Inventory inventory) => inventory == Inventory;

        // Whether the item may sit in the given cell: the vanilla attach rules for that cell's slot.
        internal bool Fits(Vector2i cell, ItemDrop.ItemData item)
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= Width || cell.y * Width + cell.x >= _slotAtCell.Length)
            {
                return false;
            }
            int slot = _slotAtCell[cell.y * Width + cell.x];
            return slot >= 0 && !_locked[slot] && CanAttach(Stand.m_slots[slot], item);
        }

        // The first empty cell the item fits, or (-1, -1).
        internal Vector2i FindCell(ItemDrop.ItemData item)
        {
            for (int slot = 0; slot < _cellOfSlot.Length; slot++)
            {
                Vector2i cell = _cellOfSlot[slot];
                if (Inventory.GetItemAt(cell.x, cell.y) == null && Fits(cell, item))
                {
                    return cell;
                }
            }
            return new Vector2i(-1, -1);
        }

        // Vanilla's ArmorStand.CanAttach plus the check in ArmorStand.UseItem that anything other
        // than chest and legs has an "attach" model to show.
        private static bool CanAttach(ArmorStand.ArmorStandSlot slot, ItemDrop.ItemData item)
        {
            if (item.m_dropPrefab == null)
            {
                return false;
            }
            var shared = item.m_shared;
            var type = shared.m_attachOverride != ItemDrop.ItemData.ItemType.None ? shared.m_attachOverride : shared.m_itemType;
            if (slot.m_supportedTypes.Count > 0 && !slot.m_supportedTypes.Contains(type))
            {
                return false;
            }
            if (shared.m_itemType == ItemDrop.ItemData.ItemType.Legs || shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
            {
                return true;
            }
            Transform root = item.m_dropPrefab.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                string name = root.GetChild(i).gameObject.name;
                if (name == "attach" || name == "attach_skin")
                {
                    return true;
                }
            }
            return false;
        }

        // Writes every slot whose item changed since the last write back to the stand.
        internal void Sync()
        {
            ZDO zdo = View.GetZDO();
            bool cleared = false, attached = false;
            for (int slot = 0; slot < _written.Length; slot++)
            {
                if (_locked[slot])
                {
                    continue;
                }
                Vector2i cell = _cellOfSlot[slot];
                ItemDrop.ItemData item = Inventory.GetItemAt(cell.x, cell.y);
                if (item == _written[slot])
                {
                    continue;
                }
                if (!View.IsOwner())
                {
                    Plugin.Log.LogWarning($"Lost ownership of {Stand.name} before its changes were saved.");
                    return;
                }
                _written[slot] = item;
                if (item == null)
                {
                    zdo.Set(ItemKey(slot), 0);
                    View.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", slot, 0, 0);
                    cleared = true;
                }
                else
                {
                    int hash = item.m_dropPrefab.name.GetStableHashCode();
                    zdo.Set(ItemKey(slot), hash);
                    zdo.Set(VariantKey(slot), item.m_variant);
                    ItemDrop.SaveToZDO(item, zdo, slot);
                    View.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", slot, hash, item.m_variant);
                    Game.instance.IncrementPlayerStat(PlayerStatType.ArmorStandUses);
                    attached = true;
                }
            }
            if (cleared)
            {
                // SetVisualItem skips this when clearing a slot; vanilla's DropItem calls it itself.
                UpdateSupports.Invoke(Stand, null);
            }
            if (attached)
            {
                Stand.m_effects.Create(Stand.transform.position, Quaternion.identity);
            }
        }

        // Trades each armor piece on the stand for the one the player is wearing in that slot.
        // Empty stand slots are skipped, leaving the player's piece on. The worn piece goes onto the
        // stand and the stand's piece takes its place in the inventory, so no free slot is needed
        // unless the player wears nothing there (or wears something the stand can't hold).
        internal void Swap(Player player)
        {
            Inventory inventory = player.GetInventory();
            int swapped = 0, noRoom = 0;
            foreach (VisSlot visSlot in ArmorSlots)
            {
                int slot = Stand.m_slots.FindIndex(s => s.m_slot == visSlot);
                if (slot < 0 || _locked[slot])
                {
                    continue;
                }
                Vector2i cell = _cellOfSlot[slot];
                ItemDrop.ItemData standItem = Inventory.GetItemAt(cell.x, cell.y);
                if (standItem == null)
                {
                    continue;
                }

                ItemDrop.ItemData worn = Worn(player, visSlot);
                if (worn != null && CanAttach(Stand.m_slots[slot], worn))
                {
                    Vector2i wornPos = worn.m_gridPos;
                    player.UnequipItem(worn, triggerEquipEffects: false);
                    inventory.RemoveItem(worn);
                    Inventory.RemoveItem(standItem);
                    Inventory.AddItem(worn, cell);
                    inventory.AddItem(standItem, wornPos);
                }
                else
                {
                    if (!inventory.HaveEmptySlot())
                    {
                        noRoom++;
                        continue;
                    }
                    Inventory.RemoveItem(standItem);
                    inventory.AddItem(standItem);
                }
                player.EquipItem(standItem);
                swapped++;
            }

            if (noRoom > 0)
            {
                player.Message(MessageHud.MessageType.Center, "$inventory_full");
            }
            else if (swapped == 0)
            {
                player.Message(MessageHud.MessageType.Center, "No armor on the stand to swap");
            }
        }

        private static ItemDrop.ItemData Worn(Player player, VisSlot slot)
        {
            switch (slot)
            {
                case VisSlot.Helmet: return HelmetItem(player);
                case VisSlot.Chest: return ChestItem(player);
                case VisSlot.Legs: return LegItem(player);
                case VisSlot.Shoulder: return ShoulderItem(player);
                case VisSlot.Utility: return UtilityItem(player);
                default: return null;
            }
        }
    }
}
