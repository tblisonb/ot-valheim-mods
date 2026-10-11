# Changelog

## 1.1.1

- A container with more rows than fit on screen (e.g. the tombstone of a
  player with an expanded inventory) no longer runs off the bottom of the
  screen. The panel now grows only as far as the screen allows, and the rest
  scrolls with a scrollbar, the same way vanilla handles large containers.

## 1.1.0

- New package icon.
- `DisplayColumnOverrides` now defaults the Wardrobe to 10 columns (exactly
  5 full rows) instead of 8. Existing config files keep whatever value they
  already have.

## 1.0.0

Initial release.

- `DisplayColumnOverrides` - repacks a configured container's slots into a
  display grid at a chosen column count, instead of its native shape.
  Default: the Wardrobe (`piece_chest_warderobe`) at 8 columns, matching
  the blackmetal chest's width.
- `StackPlacementMode` - controls which empty slot Ctrl+click (or a world
  pickup) places a new stack into: `Vanilla` (unchanged), `FirstEmptySlot`,
  or `LastEmptySlot`.
