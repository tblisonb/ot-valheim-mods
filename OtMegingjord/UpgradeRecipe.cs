using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace OtMegingjord
{
    // Vanilla has no recipe for the belt (it's only bought), and a recipe's requirements scale by
    // level rather than changing per level: Requirement.GetAmount(quality) is a formula over one
    // fixed item. So the belt gets one upgrade-only recipe listing every level's requirements, and
    // GetAmount is patched so each of them counts only at its own level and is 0 everywhere else -
    // which vanilla's requirement list, inventory check and resource consumption all already skip.
    internal static class UpgradeRecipe
    {
        private const string RecipeName = "Recipe_OtMegingjord_BeltStrength";
        private const string ForgeName = "$piece_forge";

        internal static Recipe Recipe;
        private static readonly Dictionary<Piece.Requirement, (int Level, int Amount)> LevelOf =
            new Dictionary<Piece.Requirement, (int, int)>();

        internal static void Register(ObjectDB db, ItemDrop belt)
        {
            if (db.m_recipes.Any(r => r != null && r.name == RecipeName))
            {
                return;
            }
            if (db.m_recipes.Any(r => r != null && r.m_item == belt))
            {
                Plugin.Log.LogError("The game already has a recipe for Megingjord (a game update or another mod?); not adding the upgrade recipe.");
                return;
            }

            // The Forge is a build piece, not something ObjectDB holds, so borrow the station from a
            // vanilla recipe that already uses it.
            var forge = db.m_recipes.Select(r => r != null ? r.m_craftingStation : null)
                .FirstOrDefault(station => station != null && station.m_name == ForgeName);
            if (forge == null)
            {
                Plugin.Log.LogError("No vanilla recipe uses the Forge; can't add the Megingjord upgrade recipe.");
                return;
            }

            if (Recipe == null)
            {
                Recipe = ScriptableObject.CreateInstance<Recipe>();
                Recipe.name = RecipeName;
                Recipe.m_item = belt;
                Recipe.m_amount = 1;
                Recipe.m_craftingStation = forge;
                Recipe.m_repairStation = forge;
                Recipe.m_minStationLevel = 1;
                Recipe.m_noCraftOnlyUpgrade = true;
                Recipe.m_resources = BuildRequirements(db).ToArray();
            }
            db.m_recipes.Add(Recipe);
        }

        private static List<Piece.Requirement> BuildRequirements(ObjectDB db)
        {
            var requirements = new List<Piece.Requirement>();
            foreach (var tier in Tiers.All)
            {
                if (!Items.ByLevel.TryGetValue(tier.Level, out var item))
                {
                    continue;
                }
                Add(requirements, item, tier.Level, 1);

                string materials = Plugin.TierConfigs[tier.Level].Materials.Value;
                foreach (var entry in materials.Split(',').Select(e => e.Trim()).Where(e => e.Length > 0))
                {
                    var parts = entry.Split(':');
                    if (parts.Length != 2 || !int.TryParse(parts[1].Trim(), out int amount) || amount <= 0)
                    {
                        Plugin.Log.LogError($"Level {tier.Level} Materials: can't parse '{entry}', expected PrefabName:Amount.");
                        continue;
                    }
                    var prefab = db.GetItemPrefab(parts[0].Trim());
                    var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                    if (drop == null)
                    {
                        Plugin.Log.LogError($"Level {tier.Level} Materials: no item named '{parts[0].Trim()}'.");
                        continue;
                    }
                    Add(requirements, drop, tier.Level, amount);
                }
            }
            return requirements;
        }

        // m_amount and m_amountPerLevel stay 0: vanilla's recipe discovery skips requirements whose
        // m_amount is 0, so the recipe is known as soon as the Forge is, without needing every
        // level's materials to have been seen; the real amount comes from the GetAmount patch.
        private static void Add(List<Piece.Requirement> requirements, ItemDrop item, int level, int amount)
        {
            var requirement = new Piece.Requirement
            {
                m_resItem = item,
                m_amount = 0,
                m_amountPerLevel = 0,
                m_recover = false,
            };
            LevelOf[requirement] = (level, amount);
            requirements.Add(requirement);
        }

        internal static bool TryGetAmount(Piece.Requirement requirement, int quality, out int amount)
        {
            if (LevelOf.TryGetValue(requirement, out var entry))
            {
                amount = quality == entry.Level ? entry.Amount : 0;
                return true;
            }
            amount = 0;
            return false;
        }
    }

    [HarmonyPatch(typeof(Piece.Requirement), nameof(Piece.Requirement.GetAmount))]
    internal static class RequirementGetAmountPatch
    {
        private static void Postfix(Piece.Requirement __instance, int qualityLevel, ref int __result)
        {
            if (UpgradeRecipe.TryGetAmount(__instance, qualityLevel, out int amount))
            {
                __result = amount;
            }
        }
    }

    // Vanilla needs station level = recipe minimum + (quality - 1), i.e. a level 8 Forge for the
    // last upgrade, which doesn't exist; cap it.
    [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetRequiredStationLevel))]
    internal static class GetRequiredStationLevelPatch
    {
        private static void Postfix(Recipe __instance, ref int __result)
        {
            if (__instance == UpgradeRecipe.Recipe)
            {
                __result = Mathf.Min(__result, Mathf.Max(1, Plugin.MaxForgeLevel.Value));
            }
        }
    }
}
