# OtArmorStand

In vanilla, you dress an armor stand by putting each piece on your hotbar
and pressing its number while looking at the stand, and pressing Use drops
every armor piece on the ground at once. This mod opens the stand like a
chest instead.

## Usage

Press **Use** (E) on an armor stand to open it. It shows up as a small
chest with one cell per slot, laid out like the body:

```
 Cape    Helmet  Weapon
 Shield  Chest   Belt
         Legs
```

Empty cells show a faded black-and-white icon of what goes there, and hovering one names
the slot.

- **Drag** gear in or out like any chest. Dropping a piece on the wrong
  cell puts it in the right one, and shift-clicking it from your inventory
  works too.
- **Take all** empties the stand into your inventory.
- **Swap armor** (the button where Place stacks normally is, or
  **Left Shift + E** on the stand without opening it) trades the stand's
  helmet, chest, legs, cape and belt for what you're wearing. Your pieces
  go onto the stand and its pieces are equipped. Empty stand slots are
  skipped, so you keep wearing whatever you have there.

Putting a hotbar item on the stand with its number key still works as in
vanilla.

Only one player can have a stand open at a time, like a chest.

## Configuration

`BepInEx/config/tlisonbee.valheim.otarmorstand.cfg`:

| Setting | Default | |
|---|---|---|
| `AltUseSwaps` | `true` | Left Shift + E on a stand swaps armor instead of opening it (the alternate-place key, rebindable in the game's controls). |
| `ShowSwapButton` | `true` | Show the Swap armor button on an open stand. |
| `Placeholders.Opacity` | `0.45` | Opacity of the black-and-white icons in empty cells (0 hides them). |
| `Placeholders.Brightness` | `0.6` | How bright those icons are; lower is darker. |

## Multiplayer

Runs entirely on your own game; no server install is needed. The stand
stores its gear the same way vanilla does, so players without the mod see
the same stand and can still use it the vanilla way.
