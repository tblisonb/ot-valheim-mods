# Valheim Mod Ideas

Each idea keeps its original wording. **Review notes** under each one
(added 2026-10-08) cover inconsistencies, gotchas and open questions,
checked against the decompiled game code (`assembly_valheim.dll`) where
marked *verified*.

## Existing (changes/additions)

### OtBuildOrientation

- Add a config to the mod which allows the user to enable/disable preserving
  the previously-used orientation. For example, if you rotate a piece and
  then switch build pieces, the mod normally resets to the normal upright
  position. I think this should be the default, but the config, if enabled,
  would preserve that orientation when switching pieces.

**Review notes**

- Default as written: reset is the default, and the config (off by default)
  keeps the orientation when switching pieces. That's the reverse of
  `CopyOrientation`, which defaults on. Fine, just confirm it's intended.
- Switching to a piece that can't be flipped (`m_canRotate` off, or a terrain
  tool): suggestion is to keep the stored orientation but not apply it, so it
  comes back on the next piece that can be flipped. `Orientation.Apply`
  already skips pieces that can't orient, so this mostly means not resetting
  `_index` in `OnPlacementGhostSetup` when the config is on.

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
- Belief, not yet verified: Megingjord is only bought from Haldor, so
  there's no recipe to upgrade it with. Vanilla's `Recipe.m_noCraftOnlyUpgrade`
  flag fits this: add an upgrade-only recipe so it stays bought rather than
  crafted. It also needs `m_maxQuality` raised.
- The forge uses its own recipe with idols as the cost
  (`m_upgraderResource`), so the belt needs one of those too. What's the
  level cap past 4?
- Upgrade materials are still undecided.

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
