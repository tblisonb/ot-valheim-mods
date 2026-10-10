# Changelog

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
