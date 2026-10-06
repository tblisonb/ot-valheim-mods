# ValheimMods

Two BepInEx plugins for Valheim - a "hello world" and a real gameplay mod -
plus notes on how the pieces fit together.

One repo for all mods here, rather than one repo per mod: they're small,
share this one `libs/` reference folder and build setup, and splitting a
mod out later (e.g. for its own Thunderstore release) is a trivial
`git subtree split` away if one ever grows to need it.

## What's here

```
HelloWorldMod/
  HelloWorldMod.csproj      # SDK-style .NET project, targets netstandard2.1
  Plugin.cs                 # the hello-world mod
BulkStation/
  BulkStation.csproj
  Plugin.cs                 # config + shared helpers
  SmelterPatches.cs          # batch ore/fuel input for smelter-type stations
  CookingStationPatches.cs   # batch food/fuel input + collect-all output for cooking stations
  FireplacePatches.cs        # batch fuel input for campfires, hearths, bonfires, braziers, torches, ...
libs/                        # reference-only DLLs (not redistributed, see below)
  BepInEx.dll                # from the installed BepInExPack
  0Harmony.dll                # Harmony, BepInEx's runtime-patching library
  assembly_valheim.dll        # the game's own code, for referencing game types
  assembly_utils.dll          # Utils.GetPrefabName, ZInput
  assembly_guiutils.dll       # Localization
  UnityEngine.dll
  UnityEngine.CoreModule.dll
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
  Not needed for this hello-world, but worth knowing about next.

## What `HelloWorldMod` does

`Awake()` logs a line to the BepInEx console/log confirming the plugin
loaded - the classic "hello world" signal. It originally also patched
`Player.OnSpawned` to pop "Hello, Valheim!" on screen on spawn, as a visible
proof it was working end-to-end; that patch has been removed now that
`BulkStation` is the real mod in active use, but the log line and project
stick around as the minimal reference example.

## Building

Both already built once in this session:

```
cd HelloWorldMod && dotnet build -c Release
cd ../BulkStation && dotnet build -c Release
```

Output: `<ProjectName>/bin/Release/netstandard2.1/<ProjectName>.dll`

(There's a harmless `MSB3277` warning about `System.Net.Http`/`System.IO.Compression`
version conflicts between netstandard2.1 and the game's own assemblies -
cosmetic, doesn't affect loading.)

## Trying it out

The **Modding** Gale profile (`~/.local/share/com.kesomannen.gale/valheim/profiles/Modding`,
cloned from `OnePointOh`) is the one to use for testing these mods. Built
DLLs go in:

```
~/.local/share/com.kesomannen.gale/valheim/profiles/Modding/BepInEx/plugins/
```

```
cp HelloWorldMod/bin/Release/netstandard2.1/HelloWorldMod.dll \
   BulkStation/bin/Release/netstandard2.1/BulkStation.dll \
   ~/.local/share/com.kesomannen.gale/valheim/profiles/Modding/BepInEx/plugins/
```

Then launch Valheim through Gale with the **Modding** profile selected. To
iterate: edit the code, `dotnet build -c Release`, re-copy the DLL, relaunch.

**Don't copy test builds into any other profile** (especially `OnePointOh`) -
`Modding` is the designated throwaway.

### Where BepInEx actually writes its log

Gale points Doorstop (the loader hook) straight at
`<profile>/BepInEx/core/BepInEx.Preloader.dll`, and BepInEx resolves all of
its own paths - `plugins/`, `config/`, and `LogOutput.log` - relative to
wherever that core DLL lives. So the log for a given run is always inside
*that profile's own* `BepInEx/LogOutput.log`, never in the bare game folder
(`.../Valheim/`) and never shared between profiles.

The first test run's "Hello, Valheim!" worked correctly, and the game's own
line *was* written to disk:

```
[Info   :   BepInEx] Loading [HelloWorldMod 0.1.0] (tlisonbee.valheim.helloworldmod)
[Info   :HelloWorldMod] HelloWorldMod v0.1.0 loaded!
```

It just ended up in `OnePointOh/BepInEx/LogOutput.log`, since that's the
profile that test was actually run on, before `Modding` was cloned off of
it. Nothing to fix - just check `Modding/BepInEx/LogOutput.log` after a run
on that profile from now on.

## BulkStation

Lets modifier keys speed up refining stations and fuel-burning light
sources instead of pressing Use (E) over and over:

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
`BepInEx/config/tlisonbee.valheim.bulkstation.cfg` directly.

**How it works, if you're following along in the code:** rather than
reimplementing the game's item lookup/removal/messaging logic, each batching
patch is a Harmony `Prefix` that re-invokes the *original* (unpatched)
method N times via reflection - once per item - guarded by a `_busy` flag so
those re-invocations don't recursively re-trigger the same prefix. This is
the same technique used by `~/Downloads/SmelterUnlimited.cs`, the mod you'd
already downloaded that inspired this one (it only covered smelter-type
stations' ore/fuel input; BulkStation folds in cooking-station food/fuel
input, fixed output collection, fireplace-type fuel input, and a
config-driven per-prefab capacity override instead of one global cap).

Exact signatures and behavior (`Smelter.OnAddOre`, `CookingStation.OnInteract`,
`Fireplace.Interact`, etc.) were confirmed against your installed game build
by decompiling `valheim_Data/Managed/assembly_valheim.dll` with `ilspycmd`
(`dotnet tool install -g ilspycmd`) rather than guessed from memory, since
the internal API shifts between game updates - e.g. that's how the
`Fireplace`/toggle interaction, and the fact `CookingStation.OnInteract`
only drains one slot per press, were actually confirmed rather than assumed.

### Capacity override prefab names

`OreCapacityOverrides` and `FuelCapacityOverrides` key on the station's
prefab name (e.g. `charcoal_kiln=100,smelter=40,blastfurnace=40`;
`FuelCapacityOverrides` also applies to `Fireplace`-type stations, e.g.
`campfire=50,hearth=100`). The names used in the config comments are the
commonly-documented vanilla ones, but double-check in-game if an override
doesn't seem to apply - look at the station with Server Devcommands' `info`
hover command (already installed in `Modding`) to see its exact prefab
name, and adjust the config to match. A misspelled name just has no effect;
it won't error.

## Where to go next

- [BepInEx Valheim pack on Thunderstore](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) -
  the loader itself, already installed on this machine.
- [Jötunn docs](https://valheim-modding.github.io/Jotunn/) - the
  higher-level modding library for adding real content (items, recipes,
  prefabs) instead of patching internals by hand.
- ["Build a Valheim Mod: BepInEx, Harmony, and Jotunn" guide](https://supercraft.host/wiki/valheim/build_a_valheim_mod/) -
  a fuller walkthrough covering adding a custom item end-to-end and
  publishing to Thunderstore.
