# Changelog

## Unreleased

- Pick block: with the hammer out, hold `CopyModifierKey` (default
  `LeftControl`) and press remove (middle mouse) to select the build piece
  you're looking at, in its orientation and spin. Never removes the piece.
- Vanilla's own copy (Left Shift + middle mouse) now copies a piece's
  orientation too, not just its spin.

## 1.0.0

Initial release.

- Hold `ModifierKey` (default `LeftAlt`) and scroll while placing a build
  piece to cycle which face rests downward: upright, on its left side, upside
  down, on its right side, face down, face up. Plain scroll keeps vanilla's
  spin, so together they cover every 90° orientation.
- Selecting a different piece resets it to upright.
- Flipped pieces snap to regular ones and to each other (e.g. an upright
  stair snapped flush on top of an upside-down one).
