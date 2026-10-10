# OtBuildOrientation

Vanilla only lets you spin a build piece around the vertical axis, so a beam
can't lie on its side and stairs can't hang upside down. This mod adds one
more control on top of that.

## Usage

While placing a build piece, hold **Left Alt** and scroll to cycle how the
piece lies:

> Upright → On left side → Face down

Press **F** to turn it over onto the opposite face (holding Alt at the same
time is fine):

| Lies | Flipped |
|---|---|
| Upright | Upside down |
| On left side | On right side |
| Face down | Face up |

For most pieces the flipped version looks the same (a wall upside down is
still a wall), so you only need it when the piece isn't symmetric, like
stairs, or when you care which side of a flat piece shows. While a build
tool is out, F flips instead of triggering your forsaken power; put the
tool away to use the power, or set `FlipModifierKey` (see below).

By default a flip rolls the piece over end to end, keeping the same face
toward you, so a sloped wall comes out with its slope running the same way.
With `FlipHorizontally` on, it also turns the piece around, so the slope
runs the other way. A rotation can't mirror a piece, so the two differ by
which face you see; either is one 180° spin away from the other.

A message in the middle of the screen names the current orientation.
Scrolling without Alt still spins the piece as in vanilla, so together these
cover every 90° orientation. Selecting a different piece resets it to
upright, unless `KeepOrientation` is on.

Snapping works as usual: flipped pieces snap to regular pieces and to each
other, including manual snap-point cycling. Pieces vanilla doesn't let you
rotate, and terrain tools (hoe, cultivator), are left alone. Gamepad input
isn't supported.

A placed piece's rotation is saved with the piece itself, so players
without the mod should see flipped pieces as placed. This follows from how
the game saves pieces but hasn't been tested in multiplayer yet.

## Copying a piece

Vanilla already lets you copy a built piece: with the hammer out, look at
it, hold **Left Shift** and press the remove button (middle mouse by
default), and it becomes your selected build piece. Vanilla only copies the
piece's spin, though. With this mod, a flipped piece is copied in the same
orientation it was built with.

## Configuration

`BepInEx/config/tlisonbee.valheim.otbuildorientation.cfg`, editable directly
or via a mod config manager (e.g. BepInEx Configuration Manager):

- **`ModifierKey`** (default `LeftAlt`) - the key to hold while scrolling.
  Avoid `LeftShift` (vanilla's "place without snapping") and `LeftControl`
  (crouch).
- **`FlipKey`** (default `F`) - press to flip the piece onto its opposite
  face.
- **`FlipModifierKey`** (default `None`) - if set (e.g. `LeftShift`), it
  must be held with `FlipKey` to flip, and plain F triggers the forsaken
  power as usual while building.
- **`FlipHorizontally`** (default `false`) - flipping also turns the piece
  around, reversing which way a slope runs.
- **`CopyOrientation`** (default `true`) - copy a piece's orientation when
  copying it with Left Shift + middle mouse. Off: vanilla behavior, spin
  only.
- **`KeepOrientation`** (default `false`) - keep the chosen orientation
  when you select a different piece, instead of resetting to upright.
  Pieces that can't be oriented are placed normally, and the orientation
  comes back on the next piece that can.

## Source

https://github.com/tblisonb/ot-valheim-mods/tree/main/OtBuildOrientation
