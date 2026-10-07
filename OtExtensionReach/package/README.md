# OtExtensionReach

Relaxes where crafting-station upgrade pieces (forge bellows/cooling,
workbench chopping block, cauldron spice rack, galdr table rune, black
forge/artisan table upgrades, ...) can be placed, so an
aesthetically-arranged station doesn't fight the vanilla layout rules.

Vanilla blocks placing one of these upgrades unless it's within a few meters
of its station ("Need closer to station") and clear of other upgrades ("Need
more space"). Both checks are purely client-side placement gating - nothing
server-side re-validates them - so this is a client-only tweak, same as
OtBulkStation.

## Configuration

Both settings live in
`BepInEx/config/tlisonbee.valheim.otextensionreach.cfg`, editable directly or
via a mod config manager (e.g. BepInEx Configuration Manager):

- **`MaxStationDistanceMultiplier`** (default `1`, range `1`-`20`) -
  multiplies the station's actual upgrade range, the same value vanilla uses
  both to gate placement *and* to decide whether the upgrade counts toward
  the station's level bonus. Raising it extends both together, so a
  far-flung upgrade still actually works instead of becoming decorative.
  Applies immediately, including to upgrades already built.
- **`DisableSpaceRequirement`** (default `false`) - lets upgrades be placed
  right next to each other. Purely cosmetic placement gating; doesn't affect
  the bonus.

**Multiplayer note:** since the level-bonus calculation runs per-client,
every player should run this mod for consistent crafting menus - a player
without it won't see an upgrade built beyond vanilla range as counted toward
the station's level.

## Source

https://github.com/tblisonb/ot-valheim-mods/tree/main/OtExtensionReach
