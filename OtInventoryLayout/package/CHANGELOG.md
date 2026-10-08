# Changelog

## Unreleased

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
