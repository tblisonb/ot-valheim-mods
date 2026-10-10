using UnityEngine;

namespace OtMegingjord
{
    // One upgrade level of the belt: the item Haldor sells for it (a recolored copy of a vanilla
    // item) and the shipped defaults for its config. The lore behind each item is in IDEAS.md.
    internal sealed class Tier
    {
        public int Level;
        public string Biome;
        public string PrefabName;
        public string DisplayName;
        public string Description;
        public string TemplatePrefab;
        public Color Tint;
        public string UnlockKey;
        public int Price;
        public int CarryBonus;
        public string Materials;
    }

    internal static class Tiers
    {
        public const string BeltPrefab = "BeltStrength";
        public const int MaxLevel = 8;

        // Unlock keys are the global keys each boss sets on defeat, read from the boss prefabs (the
        // later ones aren't in the GlobalKeys enum). Each item unlocks with the boss that opens its
        // biome; the final boss's key is the one set by its last phase (FrozenKing_p3), which is
        // also what the Bog Witch's stock checks.
        public static readonly Tier[] All =
        {
            new Tier
            {
                Level = 2, Biome = "Swamp",
                PrefabName = "OtMegingjord_GauntletPlates",
                DisplayName = "Gríðr's Iron Gauntlet Plates",
                Description = "Plates from Járngreipr, the iron gloves the giantess Gríðr lent Thor. He caught the giant Geirröðr's glowing iron in them and threw it back.",
                TemplatePrefab = "IronScrap", Tint = new Color(0.55f, 0.68f, 0.85f),
                UnlockKey = "defeated_gdking", Price = 700, CarryBonus = 75, Materials = "Iron:30",
            },
            new Tier
            {
                Level = 3, Biome = "Mountain",
                PrefabName = "OtMegingjord_GridarvolrSplinter",
                DisplayName = "Splinter of Gríðarvölr",
                Description = "A splinter of Gríðr's staff. Thor leaned on it against the flooding river Vimur while the water rose to his shoulders.",
                TemplatePrefab = "FineWood", Tint = new Color(0.85f, 0.92f, 1.0f),
                UnlockKey = "defeated_bonemass", Price = 1100, CarryBonus = 100, Materials = "Silver:30, WolfPelt:20",
            },
            new Tier
            {
                Level = 4, Biome = "Plains",
                PrefabName = "OtMegingjord_CauldronHandle",
                DisplayName = "Handle of Hymir's Cauldron",
                Description = "From the giant Hymir's cauldron, which Thor carried home upturned on his head, its handles ringing against his heels.",
                TemplatePrefab = "Chain", Tint = new Color(0.9f, 0.62f, 0.3f),
                UnlockKey = "defeated_dragon", Price = 1500, CarryBonus = 125, Materials = "BlackMetal:30, LoxPelt:20",
            },
            new Tier
            {
                Level = 5, Biome = "Mistlands",
                PrefabName = "OtMegingjord_SkrymirSack",
                DisplayName = "Skrýmir's Provision Sack",
                Description = "Bound with iron wire by a giant who was never what he seemed. Even Thor could not untie it.",
                TemplatePrefab = "LeatherScraps", Tint = new Color(0.62f, 0.45f, 0.9f),
                UnlockKey = "defeated_goblinking", Price = 2000, CarryBonus = 150, Materials = "Carapace:20, Eitr:10",
            },
            new Tier
            {
                Level = 6, Biome = "Ashlands",
                PrefabName = "OtMegingjord_TanngnjostrBone",
                DisplayName = "Bone of Tanngnjóstr",
                Description = "A bone of one of Thor's goats, eaten at night and raised whole from bone and hide by morning, all but one lame leg.",
                TemplatePrefab = "BoneFragments", Tint = new Color(1.0f, 0.5f, 0.28f),
                UnlockKey = "defeated_queen", Price = 2700, CarryBonus = 175, Materials = "FlametalNew:30, AskHide:20",
            },
            new Tier
            {
                Level = 7, Biome = "Deep North",
                PrefabName = "OtMegingjord_TanngrisnirHarness",
                DisplayName = "Tanngrisnir's Harness",
                Description = "Harness of the goat that draws Thor's chariot, its thunder rolling behind.",
                TemplatePrefab = "DeerHide", Tint = new Color(0.5f, 0.85f, 1.0f),
                UnlockKey = "defeated_fader", Price = 3500, CarryBonus = 200, Materials = "OrbThunderBlood:2",
            },
            new Tier
            {
                Level = 8, Biome = "Endgame",
                PrefabName = "OtMegingjord_GreyCatWhisker",
                DisplayName = "Whisker of the Grey Cat",
                Description = "From Útgarða-Loki's grey cat, which was the world serpent in disguise. Thor lifted one of its paws off the floor.",
                TemplatePrefab = "Feathers", Tint = new Color(1.0f, 0.86f, 0.32f),
                UnlockKey = "defeated_frozenking_p", Price = 4500, CarryBonus = 725, Materials = "",
            },
        };
    }
}
