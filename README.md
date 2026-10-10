# ValheimMods

BepInEx plugins for Valheim, plus notes on how the pieces fit together.

One repo for all mods here, rather than one repo per mod: they're small,
share this one `libs/` reference folder and build setup, and splitting a
mod out later (e.g. for its own repo) is a `git subtree split` away if one
ever needs it.

Mods authored here are prefixed **`Ot`** (e.g. `OtBulkStation`) to make
ownership obvious at a glance in mod lists/config folders full of other
authors' plugins. Apply the same prefix to any new mod added to this repo.

## What's here

```
OtBuildOrientation/
  OtBuildOrientation.csproj
  Plugin.cs                  # config
  OrientationPatches.cs      # Alt+scroll cycles which face of a build piece rests downward
OtBulkStation/
  OtBulkStation.csproj
  Plugin.cs                 # config + shared helpers
  SmelterPatches.cs          # batch ore/fuel input for smelter-type stations
  CookingStationPatches.cs   # batch food/fuel input + collect-all output for cooking stations
  FireplacePatches.cs        # batch fuel input for campfires, hearths, bonfires, braziers, torches, ...
  package/                   # Thunderstore package sources (see "Publishing to Hexium")
OtExtensionReach/
  OtExtensionReach.csproj
  Plugin.cs                  # config
  StationExtensionPatches.cs # relaxes crafting-station-upgrade placement restrictions
  package/
OtInventoryLayout/
  OtInventoryLayout.csproj
  Plugin.cs                   # config
  ContainerPanelPatches.cs    # hooks container-open; logs prefab/dims + optional UI-hierarchy dump
  ContainerReflowPatches.cs   # repacks a container's slots into a configured column count
  StackPlacementPatches.cs    # configurable first/last-empty-slot placement for Ctrl+click moves
  package/
package.py                   # builds each mod's Thunderstore zip from its package/ folder
libs/                        # reference-only DLLs (not redistributed, see below)
  BepInEx.dll                # from the installed BepInExPack
  0Harmony.dll                # Harmony, BepInEx's runtime-patching library
  assembly_valheim.dll        # the game's own code, for referencing game types
  assembly_utils.dll          # Utils.GetPrefabName, ZInput
  assembly_guiutils.dll       # Localization
  UnityEngine.dll
  UnityEngine.CoreModule.dll
  UnityEngine.UI.dll          # ScrollRect, Mask, Image, etc.
```

`libs/` only holds *reference assemblies* used at compile time (`<Private>false</Private>`
in the csproj, so they're never copied into the build output). At runtime the
real versions already present in your BepInEx/plugins and game folders are
used instead - this just lets the compiler know the game's types exist.

## How modding Valheim works, in short

- Valheim is a Unity game. **BepInEx** is a general-purpose Unity plugin
  loader: a native "doorstop" hook injects a .NET runtime into the game
  process before it fully starts, which then loads any `.dll` plugins it
  finds in `BepInEx/plugins/`.
- A plugin is just a class library with a class that inherits
  `BaseUnityPlugin` and is tagged `[BepInPlugin(guid, name, version)]`.
  BepInEx instantiates it as a Unity component, so you get `Awake()`,
  `Update()`, etc. like any other Unity script.
- To actually change game behavior (not just add new standalone logic), mods
  use **Harmony** to patch methods on the game's existing classes at
  runtime - no game files are ever modified on disk. You write a `Prefix`
  (runs before the original method), `Postfix` (runs after), or full
  replacement, and tag it with `[HarmonyPatch(typeof(SomeGameClass), "MethodName")]`.
  `new Harmony(guid).PatchAll()` scans your assembly and wires up every
  `[HarmonyPatch]` it finds.
- For anything beyond small tweaks (adding items, recipes, prefabs, custom
  skills), the community library **Jötunn** builds on BepInEx + Harmony to
  give you proper APIs instead of reverse-engineering the game yourself.
  Not needed for anything here yet, but worth knowing about next.

## Building

```
cd OtBulkStation && dotnet build -c Release
```

Output: `OtBulkStation/bin/Release/netstandard2.1/OtBulkStation.dll`

(There's a harmless `MSB3277` warning about `System.Net.Http`/`System.IO.Compression`
version conflicts between netstandard2.1 and the game's own assemblies -
cosmetic, doesn't affect loading.)

## Trying it out

The **Modding** Gale profile (`~/.local/share/com.kesomannen.gale/valheim/profiles/Modding`,
cloned from `OnePointOh`) is the one to use for testing these mods. There
are two stages:

**While iterating**, copy the built DLL flat into the profile's plugins
folder:

```
cp OtBulkStation/bin/Release/netstandard2.1/OtBulkStation.dll \
   ~/.local/share/com.kesomannen.gale/valheim/profiles/Modding/BepInEx/plugins/
```

Then launch Valheim through Gale with the **Modding** profile selected. To
iterate: edit the code, `dotnet build -c Release`, re-copy the DLL, relaunch.

**Once it's ready to release**, build the Thunderstore-compatible zip (see
"Publishing to Hexium" below), delete the flat-copied DLL from `plugins/`,
and install the zip into **Modding** through Gale (Gale puts it in its own
`plugins/OtMods-<Mod>/` folder). Don't leave both in place: BepInEx loads
plugins recursively, so a flat copy plus a Gale-installed folder loads the
same plugin twice. The same applies in reverse: if a Gale-installed version
of a mod is already present when you start iterating on it again, disable
or uninstall it in Gale before flat-copying a new build.

**Don't copy test builds into any other profile** (especially `OnePointOh`) -
`Modding` is the designated throwaway.

### Where BepInEx actually writes its log

Gale points Doorstop (the loader hook) straight at
`<profile>/BepInEx/core/BepInEx.Preloader.dll`, and BepInEx resolves all of
its own paths - `plugins/`, `config/`, and `LogOutput.log` - relative to
wherever that core DLL lives. So the log for a given run is always inside
*that profile's own* `BepInEx/LogOutput.log`, never in the bare game folder
(`.../Valheim/`) and never shared between profiles.

## OtBulkStation

Lets modifier keys speed up the input/output of refining stations and
fuel-burning light sources instead of pressing Use (E) over and over:

| Press                | Input (ore/wood/fuel/food slots)  | Output (cooking stations)    |
|-----------------------|-------------------------------------|-------------------------------|
| `E`                    | 1 item (vanilla, unchanged)         | 1 finished item (vanilla)     |
| `Shift + E`            | `BatchSize` items (default 10)      | every finished item at once   |
| `Alt + Shift + E`       | fills the station                   | same as `Shift + E`           |

(`Alt`, not `Ctrl`, because `Ctrl` is bound to crouch by default.)

Covers three game components, automatically and with no per-station
allowlist needed - every prefab built on any of these gets the behavior:

- **`Smelter`** - smelter, blast furnace, charcoal kiln, eitr refinery.
  Batches ore/wood and coal input.
- **`CookingStation`** - cooking station, iron cooking station, stone oven.
  Batches both food input (fills multiple free slots in one press, since
  each slot holds exactly one cooking item - there's no "stack of 10 raw
  meat" to insert the way a smelter has a stack of ore) and fuel input, and
  fixes output collection, which vanilla only ever takes from the *first*
  finished slot per press - with e.g. 3 slots done at once you'd otherwise
  need to press `E` three separate times.
- **`Fireplace`** - campfire, hearth, bonfire, braziers, sconces, torches.
  Batches fuel (wood/resin/coal) input. Stations with an on/off toggle
  (`m_canTurnOff`) are left untouched - see the comment in
  `FireplacePatches.cs` for why.

Both modifier keys, the batch size, and per-station input-capacity
overrides are configurable via the in-game BepInEx Configuration Manager
(already installed in `Modding`) or by editing
`BepInEx/config/tlisonbee.valheim.otbulkstation.cfg` directly.

**How it works, if you're following along in the code:** rather than
reimplementing the game's item lookup/removal/messaging logic, each batching
patch is a Harmony `Prefix` that re-invokes the *original* (unpatched)
method N times via reflection - once per item - guarded by a `_busy` flag so
those re-invocations don't recursively re-trigger the same prefix. This is
the same technique used by `~/Downloads/SmelterUnlimited.cs`, a mod that
inspired this one (it only covered smelter-type stations' ore/fuel input;
OtBulkStation folds in cooking-station food/fuel input, fixed output
collection, fireplace-type fuel input, and a config-driven per-prefab
capacity override instead of one global cap).

Exact signatures and behavior (`Smelter.OnAddOre`, `CookingStation.OnInteract`,
`Fireplace.Interact`, etc.) were confirmed against the installed game build
by decompiling `valheim_Data/Managed/assembly_valheim.dll` with `ilspycmd`
(`dotnet tool install -g ilspycmd`) rather than guessed from memory, since
the internal API shifts between game updates - e.g. that's how the
`Fireplace`/toggle interaction, and the fact `CookingStation.OnInteract`
only drains one slot per press, were actually confirmed rather than assumed.

### Capacity override prefab names

`OreCapacityOverrides` and `FuelCapacityOverrides` key on the station's
*internal* prefab name, which usually isn't the same as its display name -
e.g. the campfire's prefab name is `fire_pit`, not `campfire`. A misspelled
or wrong name just has no effect (logged as a warning, see above); it won't
error.

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

These were confirmed against [the Valheim wiki's "Internal ID" field](https://valheim.weirdgloop.org/)
for each page, not guessed from display names - that wiki is the fastest
way to look up anything not listed here (e.g. a prefab added in a later
game update). Server Devcommands' `info` hover command (already installed
in `Modding`) also works, in-game, against your exact installed version.

## OtExtensionReach

Relaxes where crafting-station upgrade pieces (`StationExtension` - forge
bellows/cooling, workbench chopping block, cauldron spice rack, galdr table
rune, black forge/artisan table upgrades, ...) can be placed, so an
aesthetically-arranged station doesn't fight the vanilla layout rules.

Vanilla blocks placing one of these upgrades unless it's within a few meters
of its station (`$msg_extensionmissingstation`) and clear of other upgrades
(`$msg_needspace`). Both checks run purely client-side, in
`Player.UpdatePlacementGhost` - confirmed by decompiling `assembly_valheim.dll`
with `ilspycmd` - and nothing server-side re-validates them, so this is a
client-only gameplay tweak (same as `OtBulkStation`).

Two configs, in `BepInEx/config/tlisonbee.valheim.otextensionreach.cfg`:

- `MaxStationDistanceMultiplier` (default `1`, range `1`-`20`) - multiplies
  `StationExtension.m_maxStationDistance`, the exact field vanilla uses both
  to gate placement *and* to decide whether the upgrade counts toward the
  station's level bonus (`CraftingStation.GetLevel`). Raising it extends both
  together, so a far-flung upgrade still actually works instead of becoming
  decorative - unlike just suppressing the placement error, which would let
  you build somewhere the bonus silently never applies. Applies immediately,
  including to upgrades already built (re-applied on every config change, no
  relog needed).
- `DisableSpaceRequirement` (default `true`, was `false` in `1.0.0`) - lets
  upgrades be placed right next to each other. Purely cosmetic UI gating;
  doesn't affect the bonus.

**Multiplayer note:** since both checks (and the level-bonus calculation
itself) run per-client, every player should run this mod for consistent
crafting menus - a player without it won't see an upgrade built beyond
vanilla range as counted toward the station's level.

## OtInventoryLayout

Confirmed working in-game at `1.0.0`: the Wardrobe correctly reflows into
6 full rows of 8 plus 2 leftover slots on a 7th row (the `1.0.0` default
`DisplayColumnOverrides` setting), buttons/backdrop art track the panel at
any configured width, and Ctrl+click placement lands where expected under
all three `StackPlacementMode` settings (see near the end of this section).

Vanilla's container UI panel is sized to fit about 4 rows at its native
column count (e.g. the blackmetal chest's 8x4 layout fits exactly). The 1.0
Wardrobe (`piece_chest_warderobe` - note the game's own typo, "warderobe" not
"wardrobe") is 5 columns x 10 rows, so more than half of it is hidden below
the fold every time it's opened.

The first version of this mod just grew the panel taller to show every real
row. Dropped that approach: a 10-row-tall panel doesn't actually fit on
screen next to the player's own inventory once that's expanded past vanilla's
base 8x4 (up to 8x6 as of 1.0, more with inventory-expansion mods). Instead,
this **reflows** a configured container's slots into a more compact display
shape instead of growing to fit its native one - the Wardrobe's 50 slots,
packed 8-wide (the same width as the blackmetal chest, which already fits
the panel with no changes needed), become 6 full rows plus 2 leftover slots
on a 7th row (since changed to a 10-wide default: exactly 5 full rows,
which fits at the standard UI scale and looks better), via
`DisplayColumnOverrides` in
`BepInEx/config/tlisonbee.valheim.otinventorylayout.cfg` (comma-separated
`prefab_name=columns`, e.g. `piece_chest_warderobe=8`). Only each slot's
*on-screen position* is remapped (linear index order preserved, just
re-wrapped at a different column count) - the container's actual
`Inventory` width/height, and every click/drag/hover lookup that's keyed on
real grid coordinates, are completely untouched, so save data and item grid
positions carry no risk, and there's no multiplayer desync risk the way
changing real inventory dimensions would have.

Implementation: a Harmony `Postfix` on `InventoryGrid.UpdateGui` (scoped to
only the container grid, never the player's own) runs after vanilla finishes
building/positioning `InventoryElement`s at their real grid coordinates, and
just overwrites each element's `anchoredPosition` to its remapped display
coordinate. The panel background (`InventoryGui.m_container`) grows in both
axes to fit the extra display rows/columns, relative to a baseline size
captured from the panel's first-ever open this session (needed so the panel
correctly *shrinks back* when a later-opened container isn't configured,
rather than staying stretched from whatever was opened before it).

Growing the panel isn't enough on its own, though - every other direct
child of the panel that's anchored to its own center rather than stretched
or pinned to a corner (the `TakeAll`/`StackAll`/`SortAll` buttons, the
scrollbar track, and the `sunken` inset backdrop art) has its position
*frozen* at a fixed pixel offset from that center anchor, which Unity
doesn't auto-adjust when the parent resizes. Whether that needs correcting,
and how, turned out to differ by axis:

- **Y** does need correcting. The grid is anchored to the panel's *top*
  edge and only grows downward (so scrolling makes sense) - but the center
  anchor these children use drifts away from that fixed top edge as the
  panel grows taller. Pushing their Y offset out by half the height growth
  cancels that drift and keeps each one's distance from the top constant.
- **X** needs no correction at all. The grid's own slots are horizontally
  *centered* within the panel, so the grid's visual center already
  coincides exactly with the same center-anchor point these children use -
  a child's X offset from that point already tracks the widening grid
  correctly on its own. The first attempt at this (mirroring the Y fix -
  holding a constant distance from the panel's *left* edge instead) was
  wrong: it held every button in its old screen position while the grid
  visibly widened out from under them, which was the exact "buttons don't
  track the width" symptom from testing.

Worked out by hand from Unity's `RectTransform.rect` formula
(`rect = (-size*pivot, size)`, Container's pivot being `(0,1)`); `sunken`
additionally grows in size, matching the panel, since (confirmed via
`LogContainerUiHierarchy`, see below) it and the panel border art are all
9-sliced Unity UI sprites that resize cleanly without distortion, while the
buttons/scrollbar only need repositioning, not resizing.

Known gaps: gamepad d-pad/stick navigation isn't remapped, so it still
steps through slots in native column order rather than the reflowed one -
not an issue for mouse play, but worth knowing. The player's own inventory
panel (`InventoryGui.m_player`) isn't wired up at all, per the original
scoping decision to leave it out for now. A configured column/row count
large enough to run the panel off the edge of the screen (or into the
player's own inventory panel) isn't clamped or warned about - this mod
trusts the configured value the same way `OtBulkStation`'s capacity
overrides do.

A `LogContainerUiHierarchy` debug config (off by default) dumps the full
RectTransform/component tree under the container panel to the log every
time a container opens - how the real panel structure (not just the
decompiled C#, which doesn't show Unity prefab wiring like child names,
anchors, or background art) was confirmed. `ContainerPanelPatches.cs` also
always logs `prefab=... width=... height=...` on every container open
(regardless of that debug flag), which is what actually caught two
successive prefab-name mismatches that silently no-op'd every override
before them: first `piece_wardrobe` vs. the real `piece_chest_warderobe`,
then `piece_chestwarderobe` (matching the in-game localization string,
which has no underscore) vs. the real prefab name, which does. Lesson for
adding more overrides later: trust this log line's `prefab=` value over any
localization string or wiki "Internal ID" field - they don't always match
the literal `GameObject` name exactly.

Built to extend to other containers later just by adding more
`prefab_name=columns` entries - no code changes needed for a new prefab.

### StackPlacementMode

Ctrl+click moves an item to the other inventory (player <-> container)
without picking a slot by hand - vanilla decides the slot for you via
`Inventory.FindEmptySlot(topFirst)`: `topFirst` is `true` for weapons,
tools, shields, utility items, and trinkets (first empty slot, scanning
rows top to bottom, left to right), and `false` for everything else (first
empty slot of the *last* row - still left to right within that row, not
simply "the last slot overall"). An existing partial stack of the same
item is always topped up first, before either rule is even consulted -
that part needed no changes.

Both rules scan slots in the exact same linear order (row by row, by real
grid coordinate) that `DisplayColumnOverrides` reflows a container's
display grid in - which is what makes the `topFirst=true` case already
land in the visually-first reflowed slot with no fix needed. The
`topFirst=false` case doesn't fare as well: "first empty slot of the last
*real* row" can land anywhere once that row's been re-wrapped into the
middle of a wider display grid - reported from testing as a stack landing
in the Wardrobe's display row 5 instead of the start of a row, as expected.

`StackPlacementMode` (`General` section, default `Vanilla`) replaces that
per-item-type rule outright:

- **`Vanilla`** - unchanged, including the quirk above.
- **`FirstEmptySlot`** - always the first empty slot in that same linear
  order, regardless of item type.
- **`LastEmptySlot`** - always the *last* empty slot in that order (unlike
  vanilla's `topFirst=false`, which only ever means "last row").

Implementation is a Harmony `Prefix` on `Inventory.FindEmptySlot` that,
when not `Vanilla`, replaces the scan outright rather than touching
`topFirst`'s logic - and needs no awareness of `DisplayColumnOverrides` or
reflowed display coordinates at all, since reflowing preserves linear slot
order by construction (it only changes how that order wraps into
rows/columns - see the `ContainerReflowPatches.cs` section above). Applies
to every `Inventory`, not just configured containers - including the
player's own inventory and bag-of-holding-style mods, since vanilla's rule
lives on the base `Inventory` class, not anything container-specific. If
that scope turns out to be too broad in practice (e.g. you only want this
for reflowed containers), it's a one-line narrowing, not a rewrite - say so.

## OtBuildOrientation

Confirmed working in-game at `1.0.0`, including snapping between flipped
and regular pieces (stone stairs, grausten pieces). `1.1.0` (axis + flip,
`FlipHorizontally`, `CopyOrientation`, `KeepOrientation`) confirmed in-game
before release.

Vanilla only rotates a build piece around the vertical axis (scroll wheel,
22.5° steps), so a beam can't lie on its side and stairs can't hang upside
down. Holding `ModifierKey` (default `LeftAlt`) while scrolling instead
cycles which face of the piece rests downward: upright, on its left side,
upside down, on its right side, face down, face up (as of `1.0.0`; since
split into three axes plus a flip key, see "Axis + flip" below). Together with vanilla's
spin, that covers every 90° orientation. A center-screen message names the
current one, and selecting a different piece resets it to upright.

Alt, because the obvious alternatives are taken in build mode: `LeftShift`
is vanilla's "place without snapping" (`AltPlace`) and `LeftCtrl` is crouch.

**How it works:** `Player.UpdatePlacementGhost` builds the ghost's rotation
exactly once, as `Quaternion.Euler(0, 22.5 * m_placeRotation, 0)`, and
everything after that reuses it: positioning the ghost against surfaces,
manual snap-point offsets, snap-point matching, and the rotation
`PlacePiece` reads off the ghost. A transpiler multiplies the chosen face
into that one value (`spin * face`, so faces are relative to the piece's
own facing), and all of those pick it up with no changes of their own. A
second transpiler swaps `Player.UpdatePlacement`'s one
`ZInput.GetMouseScrollWheel()` call for a filter that, while the modifier
is held, consumes the scroll (so vanilla doesn't also spin) and steps the
face instead. Both transpilers check that vanilla still makes exactly one
such call and log an error instead of patching if a game update changes
that. Pieces vanilla won't spin (`m_canRotate` off) and terrain ops
(`m_groundPiece`) are left alone.

One vanilla check needed loosening for flipped pieces to snap to each other.
Before accepting a snap, `Player.IsOverlappingOtherPiece` rejects it if an
identically-named piece already sits within 5cm of the snapped position,
regardless of rotation unless the piece sets `m_allowRotatedOverlap` (stone
stairs and beams don't). A stone stair's pivot is the center of its flat
bottom face, so after flipping it upside down that face and its pivot are on
top, and an upright stair snapped flush onto it lands on exactly the same
pivot. Every such snap was being thrown away as a "duplicate." A postfix now
only counts a same-position piece as a duplicate if its up direction matches
too. Unflipped pairs always match, so vanilla's verdict is unchanged for
them, and two pieces flipped the same way still block each other.

Snap-point layouts came from reading the prefabs straight out of the game's
asset bundles (`valheim_Data/StreamingAssets/SoftRef/Bundles/`) with
[UnityPy](https://github.com/K0lb3/UnityPy): snap points are child
`Transform`s named `$hud_snappoint_*`, and `Piece` fields such as
`m_allowRotatedOverlap` read out via the MonoBehaviour typetree. E.g.
`stone_stair` is 2m x 2m and rises 1m from front (+Z) to back (-Z), with snap
points along its bottom-front, bottom-back and top-back edges.

**Copy orientation (`1.1.0`):** vanilla already has pick
block: `Player.CopyPiece`, bound to `AltPlace` (`LeftShift`) + `Remove`
(middle mouse). It selects the hovered piece in the build menu and copies
its spin, recovered from the piece's yaw, which is wrong for a flipped
piece. With `CopyOrientation` on (default), a `CopyPiece` postfix searches
every spin step x face for the combination closest to the piece's actual
rotation. `CopyPiece` doesn't return the piece, so a
`SetSelectedPiece(Piece)` postfix captures it while `CopyPiece` runs.

**Axis + flip (`1.1.0`):** the six faces pair up by axis
(upright/upside down, left/right side, face down/face up), and for most
pieces the second of each pair looks like the first. Since no geometry
check can tell which pairs a player sees as the same (a wooden wall's
outline is symmetric front to back, but its two faces differ, and snap points
and bounds can't see textures), the choice is left to the player: Alt+scroll
steps between the three axes, landing on the first face of each, and
`FlipKey` (default F, optionally with `FlipModifierKey`) swaps to the other
face of the current axis. Vanilla fires the forsaken power on
`GetButtonDown("GP")` (F) regardless of modifiers, so a `StartGuardianPower`
prefix swallows it on the frame the flip key goes down while in place mode.
Each face's partner is a 180° roll about the piece's forward axis, which
keeps a sloped wall's slope direction; `FlipHorizontally` adds a 180° spin
(`Ry180 * Rz180 = Rx180`, a tip about the side axis), which reverses it.
True mirroring isn't possible with a rotation.

**Keep orientation (`1.1.0`):** with `KeepOrientation` on
(default off), `OnPlacementGhostSetup` skips its reset to upright when the
selected prefab changes. `Orientation.Apply` already ignores the face for
pieces that can't orient (`m_canRotate` off, `m_groundPiece`), so the face is
held through those and reapplied on the next piece that can.

The placed rotation is stored on the piece itself, so players without the
mod should still see flipped pieces correctly. That's expected from how
vanilla saves pieces but not yet confirmed. Gamepad input isn't handled.

## Publishing to Hexium

[Hexium](https://hexium.gg) accepts Thunderstore-compatible mod packages -
no separate per-mod repo is required; `website_url` in the manifest can
point anywhere, including a subfolder of this monorepo. A package is a zip
containing, at the root:

- `manifest.json` - `name` (letters/digits/underscores only, no spaces or
  dashes), `description` (<=256 chars), `version_number` (`Major.Minor.Patch`),
  `website_url`, `dependencies` (array, empty is fine; don't declare
  BepInExPack_Valheim, it's assumed)
- `icon.png` - exactly 256x256
- `README.md` - the mod's page content (can differ from this repo's README)
- `CHANGELOG.md` - optional
- the built plugin DLL

Everything except the DLL is tracked in each mod's `package/` folder, with
one exception: `package/manifest.json` has no `version_number`. To build
the zips:

```
./package.py                  # every mod
./package.py OtInventoryLayout  # just the named mod(s)
```

For each mod this builds the Release DLL, stages `package/*` plus the DLL in
`<Mod>/dist/pkg/`, and zips it to `<Mod>/dist/<Mod>-<version>.zip`
(`dist/` is gitignored). The version comes from `PluginVersion` in
`Plugin.cs` and is written into the staged `manifest.json`, so there's one
place to bump it. The script refuses to package if `package/CHANGELOG.md`
has no `## <version>` heading for that version, or if the manifest's name or
description break Hexium's rules above.

Package after committing the release: the compiler stamps the current git
commit hash into the DLL (`AssemblyInformationalVersion`). That hash is the
only difference between two builds of the same code.

To release a new version: bump `PluginVersion`, add a `## <version>`
section to `package/CHANGELOG.md`, commit, tag
`<modname-lowercase>-v<version>` (e.g. `otinventorylayout-v1.0.0`), then
run `./package.py <Mod>` and upload the zip.

Max package size 512 MB. Uploading itself is via Hexium's web UI under your
account's team (mods belong to a team, not a bare user) - there's no
documented CLI/API for publishing a new mod version (the API token Hexium
documents is for exporting Gale modpacks, a different feature). See
[hexium.gg/packaging](https://hexium.gg/packaging) and
[hexium.gg/faq](https://hexium.gg/faq) for specifics, since these may change.

## Where to go next

- [BepInEx Valheim pack on Thunderstore](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) -
  the loader itself, already installed on this machine.
- [Jötunn docs](https://valheim-modding.github.io/Jotunn/) - the
  higher-level modding library for adding real content (items, recipes,
  prefabs) instead of patching internals by hand.
- ["Build a Valheim Mod: BepInEx, Harmony, and Jotunn" guide](https://supercraft.host/wiki/valheim/build_a_valheim_mod/) -
  a fuller walkthrough covering adding a custom item end-to-end and
  publishing to Thunderstore.
