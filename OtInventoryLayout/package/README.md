# OtInventoryLayout

Repacks a container's inventory slots into a more compact display shape, so
you can see the whole thing without scrolling - and a config for where
Ctrl+click puts a new stack.

The 1.0 Wardrobe is 5 columns x 10 rows under the hood, which is taller than
the container UI panel is built to show without scrolling. Rather than just
growing the panel to fit all 10 real rows (which doesn't reliably fit on
screen next to your own, possibly-expanded, inventory), this mod *reflows*
the Wardrobe's 50 slots into 8 columns instead - the same width as the
blackmetal chest - landing on 6 full rows plus 2 slots on a 7th. Only the
on-screen position of each slot changes; the container's actual width/height,
save data, and item positions are completely untouched, and every click/drag
interaction keeps working normally.

## Configuration

Both settings live in
`BepInEx/config/tlisonbee.valheim.otinventorylayout.cfg`, editable directly or
via a mod config manager (e.g. BepInEx Configuration Manager):

- **`DisplayColumnOverrides`** (default `piece_chest_warderobe=8`) -
  comma-separated `prefab_name=columns` pairs. For each listed container
  prefab, repacks its slots into a display grid this many columns wide
  (rows are however many that takes). Leave a prefab out to keep its
  vanilla layout. Works for any container, not just the Wardrobe - just add
  more entries.
- **`StackPlacementMode`** (default `Vanilla`) - which empty slot a
  Ctrl+click (or world pickup, if no existing stack has room) places a new
  stack into. An existing partial stack of the same item is always topped
  up first, regardless of this setting. `Vanilla` keeps unchanged game
  behavior, which depends on item type and can look odd on a reflowed
  container (reported during testing: a stack landing mid-grid instead of
  at the start of a row). `FirstEmptySlot` / `LastEmptySlot` always use the
  first/last empty slot instead, in the same reading order a reflowed
  container's display grid uses. Applies to every inventory, not just
  reflowed containers.

## Source

https://github.com/tblisonb/ot-valheim-mods/tree/main/OtInventoryLayout
