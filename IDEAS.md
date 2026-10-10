# Valheim Mod Ideas

Each idea keeps its original wording. **Review notes** under each one
(added 2026-10-08) cover inconsistencies, gotchas and open questions,
checked against the decompiled game code (`assembly_valheim.dll`) where
marked *verified*.

## New (named, concrete mod ideas)

### OtMegaMegingjord

- "Megingjord" is an item in the game worn in the utility slot, and provides
  an additional 150 carry weight, such that the player can carry more/heavier
  items at once without becoming "Encumbered".
- I'd like to make a path to upgrade Megingjord (the same way that
  armor/weapons are upgraded). The player would need to collect/find
  particular items to upgrade it (I'm not sure what these items would be just
  yet) and each upgrade would provide an additional 150 carry weight. The max
  level should remain at 4 (three upgrades from the base megingjord), but
  should also be able to be used at the forge of potential to push the
  weight limit increase even higher.

**Review notes**

- *Verified:* the +150 is a fixed `SE_Stats.m_addMaxCarryWeight` on the
  belt's equip status effect, with no per-level scaling. The mod must
  calculate the bonus from the belt's quality itself (e.g. a patch around
  `Player.GetMaxCarryWeight` / `SE_Stats.ModifyMaxCarryWeight`).
- Megingjord is only bought from Haldor, so there's no recipe to upgrade
  it with. Vanilla's `Recipe.m_noCraftOnlyUpgrade` flag fits this: add an
  upgrade-only recipe so it stays bought rather than crafted. It also needs
  `m_maxQuality` raised (to 8 under the design below).
- ~~Forge of Potential path~~: dropped by the design below (the belt reaches
  level 8 through Haldor instead).

**Design** (agreed 2026-10-09; supersedes the original wording above where
they differ)

One upgrade per biome. Haldor sells a mythology-themed upgrade item for each
level, each unlocked by the boss that opens that item's biome, and each
upgrade also needs that biome's materials, so you have to explore the biome
before you can upgrade. Haldor himself is found in the Black Forest and the
belt isn't boss-gated (vanilla: 950 coins), so the Black Forest counts as
level 1.

| Level | Biome | Haldor item | Unlocked by | Price | Carry added | Belt total | With base 300 |
|---|---|---|---|---|---|---|---|
| 1 | Black Forest | Megingjord (vanilla) | - | 950 | 150 | 150 | 450 |
| 2 | Swamp | Gríðr's Iron Gauntlet Plates | The Elder (`defeated_gdking`) | 700 | +75 | 225 | 525 |
| 3 | Mountain | Splinter of Gríðarvölr | Bonemass (`defeated_bonemass`) | 1,100 | +100 | 325 | 625 |
| 4 | Plains | Handle of Hymir's Cauldron | Moder (`defeated_dragon`) | 1,500 | +125 | 450 | 750 |
| 5 | Mistlands | Skrýmir's Provision Sack | Yagluth (`defeated_goblinking`) | 2,000 | +150 | 600 | 900 |
| 6 | Ashlands | Bone of Tanngnjóstr | The Queen (key TBD) | 2,700 | +175 | 775 | 1,075 |
| 7 | Deep North | Tanngrisnir's Harness | Fader (key TBD) | 3,500 | +200 | 975 | 1,275 |
| 8 | Endgame | Whisker of the Grey Cat | The final boss (key TBD) | 4,500 | +725 | 1,700 | 2,000 |

- **Carry weight:** +75 at level 2, then +25 more per level through level 7.
  Level 8 is a big final jump to a 2,000 total (including base 300), so the
  endgame belt feels like becoming a god. All per-level amounts configurable.
- **Prices:** just under double the first draft (400 ... 2,500), since late
  game piles up coins with little to spend them on (~15,000 by the Deep
  North in the current playthrough). Total for all seven items: 16,000.
  Configurable.
- **Gating:** world boss keys only, nothing per player. Someone who first
  finds Haldor in the Mountains can buy the belt and the level 2 and 3
  items at once. Level 7 is Fader's key alone (it's what opens the Deep
  North); a per-player "has reached the Deep North" check was considered and
  dropped as inconsistent with boss-only gating. The first four key names
  are in the game code (`GlobalKeys` enum); the Queen's, Fader's and the
  final boss's come from the boss prefabs - read them with UnityPy.
- **Station:** the Forge. Vanilla rule (level N needs Forge level N), capped
  at 7, since the Forge can't go higher; level 8 needs Forge 7. Patch
  `Recipe.GetRequiredStationLevel` for this recipe only.
- **Recipe per level:** one of that level's Haldor item, plus that biome's
  materials (agreed quantities: a full stack, 30, of metal; 20 of
  secondary items; configurable):
  - 2 Swamp: Iron 30
  - 3 Mountain: Silver 30, Wolf Pelt 20
  - 4 Plains: Black Metal 30, Lox Pelt 20
  - 5 Mistlands: Carapace 20, Refined Eitr 10
  - 6 Ashlands: Flametal 30, Asksvin Hide 20
  - 7 Deep North: Thunderblood Essence 2 (rare; chosen over Frostfire
    Essence for the thunder tie-in)
  - 8 Endgame: the Haldor item alone. The final boss's drops (Sacrificial
    Blood, Crown Jewel) already have uses, so the boss gate on the item is
    the requirement.
- **Art:** start with a recolor of a vanilla model/icon per item.
- **Server sync:** optional and deferred. The mod must work client-only.
- **Name:** open; "OtMegaMegingjord" may change now that it's a larger,
  mythology-driven mod.
- **Open questions:** which vanilla items to recolor; whether Haldor's
  items stack (sold one at a time is assumed).

**Lore** (sources: the *Prose Edda* - Gylfaginning and Skáldskaparmál - and
the *Poetic Edda*'s Hymiskviða)

- **Megingjörð** ("power-belt") is one of Thor's three treasures, with
  Mjölnir and Járngreipr. Gylfaginning says buckling it on doubles his
  divine strength.
- **Gríðr's gifts (Skáldskaparmál, the Geirröðr story).** Thor sets out for
  the giant Geirröðr's hall without Mjölnir. On the way he stays with the
  friendly giantess Gríðr (mother of the god Víðarr), who warns him about
  Geirröðr and lends him a strength belt, iron gloves and her staff,
  Gríðarvölr. The belt as part of a set of three is what ties the first
  upgrades together.
  - **Járngreipr** (iron gloves, level 2): Thor needs them to grip Mjölnir's
    handle. At Geirröðr's hall the giant flings a glowing lump of iron at
    Thor, who catches it in the gloves and hurls it back, through a pillar
    and through Geirröðr.
  - **Gríðarvölr** (Gríðr's staff, level 3): crossing the river Vimur, which
    Geirröðr's daughter Gjálp makes rise around him, Thor leans on the staff
    against the current while the water reaches his shoulders. He gets out
    by grabbing a rowan, hence "the rowan is Thor's salvation".
- **Hymir's cauldron (Hymiskviða, level 4).** The gods need a cauldron big
  enough to brew ale for a feast at the sea giant Ægir's. Thor and Týr fetch
  one from the giant Hymir, and Thor goes fishing with Hymir, baits with an
  ox head and hooks the world serpent Jörmungandr. Thor carries the cauldron
  home upturned on his head, its handles ringing against his heels - the
  heaviest thing he carries in the myths.
- **Útgarða-Loki's journey (Gylfaginning).** Thor, Loki and the servants
  Þjálfi and Röskva travel to the giant king Útgarða-Loki, who defeats them
  with illusions (fitting for the misty, deceptive Mistlands).
  - **Skrýmir's provision sack (level 5):** on the way they meet the huge
    giant Skrýmir, who offers to carry everyone's food in his bag and binds
    it shut. Thor can't loosen the knot. Skrýmir turns out to be
    Útgarða-Loki in disguise, and the bag was bound with iron wire.
  - **The grey cat (level 8):** among Útgarða-Loki's challenges, Thor tries
    to lift his grey cat and manages only to raise one paw off the floor.
    The cat was really Jörmungandr, which encircles the world, and the
    giants were terrified that Thor raised it at all. (The other challenges
    were a drinking horn whose end lay in the sea, and wrestling Elli, who
    was old age itself.)
- **Thor's goats (Gylfaginning, levels 6 and 7).** Thor's chariot is drawn
  by two goats, **Tanngrisnir** and **Tanngnjóstr** ("teeth-barer" and
  "teeth-grinder"). Staying at a farmer's house, Thor slaughters and cooks
  them, and has the bones laid on the hides. The farmer's son Þjálfi splits
  a thigh bone for the marrow. In the morning Thor raises the goats with
  Mjölnir, and one is lame. In compensation the farmer gives him his
  children, Þjálfi and Röskva, as servants. Death and revival from the bones
  suit the Ashlands; a goat-drawn chariot is the nearest Norse thing to a
  sled through the Deep North's snow. (The myth doesn't say which goat was
  lamed, so "Bone of Tanngnjóstr" is our pick.)

## New (ideas for potential mods but otherwise incomplete and currently unnamed)

- With the hammer out, be able to scroll through different roof/floor/wall
  pieces of different types/materials.

**Review notes**

- Scroll through pieces:
  - Which modifier? Scroll and Alt+scroll are already taken by spin and flip
    (OtBuildOrientation), Shift (no snapping) and Ctrl (crouch) are vanilla's,
    and Q/E cycle snap points.
  - Swap what? Same shape in a different material, or a different shape in
    the same material?
  - The game doesn't record which pieces belong together, so the groups
    would need prefab-name matching, size/snap-point matching, or a
    hand-written list.
  - It might belong in OtBuildOrientation rather than a new mod.
