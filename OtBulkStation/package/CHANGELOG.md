# Changelog

## 1.0.4

- New package icon.

## 1.0.3

- Documented the exact internal prefab name for every station
  `OreCapacityOverrides`/`FuelCapacityOverrides` can target (README table).
- Fixed a wrong example: the campfire's prefab name is `fire_pit`, not
  `campfire`.

## 1.0.2

- `OreCapacityOverrides`/`FuelCapacityOverrides` entries that aren't in
  `prefab_name=amount` form (e.g. a bare number) now log a warning instead
  of silently being dropped.

## 1.0.0

Initial release.

- Batch ore/wood/coal input for smelter-type stations (smelter, blast
  furnace, charcoal kiln, eitr refinery).
- Batch food/fuel input and collect-all output for cooking stations
  (cooking station, iron cooking station, stone oven).
- Batch fuel input for fire-type stations (campfire, hearth, bonfire,
  braziers, sconces, torches).
- Configurable modifier keys, batch size, and per-station input-capacity
  overrides.
