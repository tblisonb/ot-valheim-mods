# OtBulkStation

Lets modifier keys speed up the input/output of refining stations and
fuel-burning light sources instead of pressing Use (E) over and over.

| Press                 | Input (ore/wood/fuel/food slots) | Output (cooking stations)  |
|------------------------|-----------------------------------|------------------------------|
| `E`                    | 1 item (vanilla, unchanged)      | 1 finished item (vanilla)   |
| `Shift + E`            | `BatchSize` items (default 10)   | every finished item at once |
| `Alt + Shift + E`      | fills the station                | same as `Shift + E`         |

(`Alt`, not `Ctrl`, because `Ctrl` is bound to crouch by default.)

Covers three game components, automatically and with no per-station
allowlist needed - every prefab built on any of these gets the behavior:

- **Smelter-type stations** (smelter, blast furnace, charcoal kiln, eitr
  refinery) - batches ore/wood and coal input.
- **Cooking stations** (cooking station, iron cooking station, stone oven) -
  batches food input (fills multiple free slots in one press) and fuel
  input, and fixes output collection, which vanilla only ever takes from
  the *first* finished slot per press - with e.g. 3 slots done at once
  you'd otherwise need to press `E` three separate times.
- **Fire-type stations** (campfire, hearth, bonfire, braziers, sconces,
  torches) - batches fuel (wood/resin/coal) input. Stations with an
  on/off toggle are left untouched to avoid misfiring the toggle.

## Configuration

Both modifier keys, the batch size, and per-station input-capacity
overrides are configurable via a mod config manager (e.g. BepInEx
Configuration Manager) or by editing
`BepInEx/config/tlisonbee.valheim.otbulkstation.cfg` directly.

`OreCapacityOverrides` and `FuelCapacityOverrides` key on the station's
*internal* prefab name, which usually isn't the same as its display name -
e.g. the campfire's prefab name is `fire_pit`, not `campfire`. A misspelled
or wrong name just has no effect (logged as a warning); it won't error.

| Station (display name)   | Prefab name                | Capacity setting    |
|----------------------------|------------------------------|-----------------------|
| Smelter                    | `smelter`                   | Ore + Fuel (coal)     |
| Blast furnace               | `blastfurnace`               | Ore + Fuel (coal)     |
| Charcoal kiln                | `charcoal_kiln`              | Ore only (no fuel slot) |
| Eitr refinery                 | `eitrrefinery`                | Ore only (no fuel slot) |
| Cooking station                | `piece_cookingstation`         | Fuel (wood) only - food input is slot-based, not capacity-overridden |
| Iron cooking station             | `piece_cookingstation_iron`      | Fuel (wood) only |
| Stone oven                        | `piece_oven`                      | Fuel (wood) only |
| Campfire                             | `fire_pit`                          | Fuel (wood/resin) |
| Hearth                                 | `hearth`                              | Fuel (wood/resin) |
| Bonfire                                  | `bonfire`                                | Fuel (wood/resin) |
| Standing brazier                           | `piece_brazierfloor01`                   | Fuel (wood/resin) |
| Hanging brazier                              | `piece_brazierceiling01`                   | Fuel (wood/resin) |
| Sconce                                         | `piece_walltorch`                            | Fuel (wood/resin) |
| Standing wood torch                              | `piece_groundtorch_wood`                       | Fuel (wood/resin) |
| Standing iron torch                                | `piece_groundtorch`                              | Fuel (wood/resin) |

Example: `FuelCapacityOverrides = fire_pit=50,hearth=100,smelter=40`.

## Source

https://github.com/tblisonb/ot-valheim-mods/tree/main/OtBulkStation
