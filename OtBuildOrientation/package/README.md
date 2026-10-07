# OtBuildOrientation

Vanilla only lets you spin a build piece around the vertical axis, so a beam
can't lie on its side and stairs can't hang upside down. This mod adds one
more control on top of that.

## Usage

While placing a build piece, hold **Left Alt** and scroll to cycle which
face of the piece rests downward:

> Upright → On left side → Upside down → On right side → Face down → Face up

A message in the middle of the screen names the current orientation.
Scrolling without Alt still spins the piece as in vanilla, so together the
two cover every 90° orientation. Selecting a different piece resets it to
upright.

Snapping works as usual: flipped pieces snap to regular pieces and to each
other, including manual snap-point cycling. Pieces vanilla doesn't let you
rotate, and terrain tools (hoe, cultivator), are left alone. Gamepad input
isn't supported.

A placed piece's rotation is saved with the piece itself, so players
without the mod should see flipped pieces as placed. This follows from how
the game saves pieces but hasn't been tested in multiplayer yet.

## Configuration

`BepInEx/config/tlisonbee.valheim.otbuildorientation.cfg`, editable directly
or via a mod config manager (e.g. BepInEx Configuration Manager):

- **`ModifierKey`** (default `LeftAlt`) - the key to hold while scrolling.
  Avoid `LeftShift` (vanilla's "place without snapping") and `LeftControl`
  (crouch).

## Source

https://github.com/tblisonb/ot-valheim-mods/tree/main/OtBuildOrientation
