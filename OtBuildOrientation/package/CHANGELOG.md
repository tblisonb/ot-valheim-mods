# Changelog

## 1.2.0

- Turn an already-built piece in place: with a build tool out, look at it,
  hold Left Shift (`RotatePieceKey`) and scroll. It turns around its own
  center, 90° per notch by default (`RotatePieceStep`). Ward, no-build zone
  and workbench rules are the same as for removing the piece.
- Other players need the mod to see the piece turn right away; without it
  they see the change after reloading the area.
- Optional server install: a dedicated server with the mod turns away
  players who don't have it (`RequireOnClients`, on by default).

## 1.1.1

- New package icon.

## 1.1.0

- Alt + scroll now steps between three ways of lying (upright, on its side,
  flat) instead of all six faces. **F** (`FlipKey`, optional
  `FlipModifierKey`) turns the piece over onto the opposite face (upside
  down, other side, face up), which most pieces don't need. While a build
  tool is out, F no longer triggers the forsaken power.
- `FlipHorizontally` (default `false`): flipping also turns the piece
  around, so a sloped wall's slope runs the other way.
- `CopyOrientation` (default `true`): vanilla's piece copy (Left Shift +
  middle mouse) now copies a flipped piece's orientation, not just its
  spin.
- `KeepOrientation` (default `false`): keep the chosen orientation when
  selecting a different build piece instead of resetting to upright.

## 1.0.0

Initial release.

- Hold `ModifierKey` (default `LeftAlt`) and scroll while placing a build
  piece to cycle which face rests downward: upright, on its left side, upside
  down, on its right side, face down, face up. Plain scroll keeps vanilla's
  spin, so together they cover every 90° orientation.
- Selecting a different piece resets it to upright.
- Flipped pieces snap to regular ones and to each other (e.g. an upright
  stair snapped flush on top of an upside-down one).
