using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace OtMegingjord
{
    // Creates the upgrade items as recolored copies of vanilla items, registers them where the game
    // looks prefabs up (ObjectDB for inventories and recipes, ZNetScene for anything spawned into
    // the world), and adds the belt's upgrade recipe.
    internal static class Items
    {
        // Prefabs live under an inactive holder, so the copies' Awake (ItemDrop, ZNetView) never
        // runs on them; they stay active themselves, so instances the game spawns from them are.
        private static GameObject _holder;
        internal static readonly Dictionary<int, ItemDrop> ByLevel = new Dictionary<int, ItemDrop>();

        private static readonly AccessTools.FieldRef<ZNetScene, Dictionary<int, GameObject>> NamedPrefabs =
            AccessTools.FieldRefAccess<ZNetScene, Dictionary<int, GameObject>>("m_namedPrefabs");

        private static readonly System.Reflection.MethodInfo CloneMethod =
            AccessTools.Method(typeof(object), "MemberwiseClone");

        internal static void EnsurePrefabs(Func<string, GameObject> findPrefab)
        {
            if (ByLevel.Count == Tiers.All.Length)
            {
                return;
            }
            if (_holder == null)
            {
                _holder = new GameObject("OtMegingjord_Prefabs");
                _holder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(_holder);
            }

            foreach (var tier in Tiers.All)
            {
                if (ByLevel.ContainsKey(tier.Level))
                {
                    continue;
                }
                var template = findPrefab(tier.TemplatePrefab);
                if (template == null || template.GetComponent<ItemDrop>() == null)
                {
                    Plugin.Log.LogError($"Template item '{tier.TemplatePrefab}' for {tier.DisplayName} not found; level {tier.Level} can't be upgraded to.");
                    continue;
                }
                ByLevel[tier.Level] = CreateItem(tier, template);
            }
        }

        private static ItemDrop CreateItem(Tier tier, GameObject template)
        {
            var go = UnityEngine.Object.Instantiate(template, _holder.transform, false);
            go.name = tier.PrefabName;

            var drop = go.GetComponent<ItemDrop>();
            var templateShared = template.GetComponent<ItemDrop>().m_itemData.m_shared;
            // Instantiate deep-copies serialized fields, but make sure the copy never shares (and
            // so renames) the template's SharedData.
            if (ReferenceEquals(drop.m_itemData.m_shared, templateShared))
            {
                drop.m_itemData.m_shared = (ItemDrop.ItemData.SharedData)CloneMethod.Invoke(templateShared, null);
            }

            var shared = drop.m_itemData.m_shared;
            shared.m_name = tier.DisplayName;
            shared.m_description = tier.Description;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 10;
            shared.m_weight = 1f;
            // Not worth anything to traders, so it can't be bought and sold back.
            shared.m_value = 0;
            shared.m_teleportable = true;
            shared.m_icons = shared.m_icons.Select(icon => Recolor.Sprite(icon, tier.Tint)).ToArray();
            drop.m_itemData.m_stack = 1;
            drop.m_itemData.m_quality = 1;

            Recolor.Renderers(go, tier.Tint);
            return drop;
        }

        internal static void RegisterInObjectDB(ObjectDB db)
        {
            var belt = db.GetItemPrefab(Tiers.BeltPrefab);
            if (belt == null)
            {
                // The main menu's ObjectDB, or one not filled in yet.
                return;
            }

            EnsurePrefabs(name => db.m_items.FirstOrDefault(item => item != null && item.name == name));

            bool added = false;
            foreach (var drop in ByLevel.Values)
            {
                if (!db.m_items.Contains(drop.gameObject))
                {
                    db.m_items.Add(drop.gameObject);
                    added = true;
                }
            }
            if (added)
            {
                // Rebuilds m_itemByHash / m_itemByData from m_items.
                AccessTools.Method(typeof(ObjectDB), "UpdateRegisters").Invoke(db, null);
            }

            var beltDrop = belt.GetComponent<ItemDrop>();
            beltDrop.m_itemData.m_shared.m_maxQuality = Tiers.MaxLevel;
            UpgradeRecipe.Register(db, beltDrop);
        }

        internal static void RegisterInZNetScene(ZNetScene scene)
        {
            EnsurePrefabs(name => scene.m_prefabs.FirstOrDefault(prefab => prefab != null && prefab.name == name));

            var named = NamedPrefabs(scene);
            foreach (var drop in ByLevel.Values)
            {
                int hash = drop.gameObject.name.GetStableHashCode();
                if (!named.ContainsKey(hash))
                {
                    scene.m_prefabs.Add(drop.gameObject);
                    named.Add(hash, drop.gameObject);
                }
            }
        }

        internal static Tier TierOf(ItemDrop drop) =>
            Tiers.All.FirstOrDefault(tier => ByLevel.TryGetValue(tier.Level, out var d) && d == drop);
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class ObjectDBAwakePatch
    {
        private static void Postfix(ObjectDB __instance) => Items.RegisterInObjectDB(__instance);
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    internal static class ObjectDBCopyOtherDBPatch
    {
        private static void Postfix(ObjectDB __instance) => Items.RegisterInObjectDB(__instance);
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class ZNetSceneAwakePatch
    {
        private static void Postfix(ZNetScene __instance) => Items.RegisterInZNetScene(__instance);
    }
}
