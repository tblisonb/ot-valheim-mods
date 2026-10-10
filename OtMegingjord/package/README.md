# OtMegingjord

In vanilla, Megingjord is bought from Haldor and stays a flat +150 carry
weight for the rest of the game. This mod lets you upgrade it at the Forge
through eight levels, one per biome, until carrying stops being a concern.

## How it works

Each upgrade needs one item that Haldor sells, plus materials from that
biome. Haldor starts selling each item once the boss that opens its biome
is defeated, so you can only upgrade after reaching and exploring the
biome. The items come from the Norse myths around Thor and his belt.

| Level | Biome | Haldor sells | After defeating | Price | Also needs | Carry weight |
|---|---|---|---|---|---|---|
| 1 | Black Forest | Megingjord (vanilla) | - | 950 | - | +150 |
| 2 | Swamp | Gríðr's Iron Gauntlet Plates | The Elder | 700 | 30 Iron | +225 |
| 3 | Mountain | Splinter of Gríðarvölr | Bonemass | 1,100 | 30 Silver, 20 Wolf Pelt | +325 |
| 4 | Plains | Handle of Hymir's Cauldron | Moder | 1,500 | 30 Black Metal, 20 Lox Pelt | +450 |
| 5 | Mistlands | Skrýmir's Provision Sack | Yagluth | 2,000 | 20 Carapace, 10 Refined Eitr | +600 |
| 6 | Ashlands | Bone of Tanngnjóstr | The Queen | 2,700 | 30 Flametal, 20 Asksvin Hide | +775 |
| 7 | Deep North | Tanngrisnir's Harness | Fader | 3,500 | 2 Thunderblood Essence | +975 |
| 8 | Endgame | Whisker of the Grey Cat | The final boss | 4,500 | - | +1,700 |

Upgrading to level N needs a level N Forge (level 8 needs Forge level 7,
the highest there is). The carry weight column is the belt's total bonus.

Everything runs on your own game: no server install is needed, and
players without the mod are unaffected. Prices, materials, carry bonuses
and unlock keys are configurable.

## Multiplayer note

The upgrade items only exist for players who have the mod. A player without
it can't see them, and if such a player opens a chest holding them and
moves anything in it, the game drops the items it doesn't recognize when it
saves the chest. Keep upgrade items in your own inventory or in chests only
modded players use. The upgraded belt itself is safe: it's a normal
Megingjord to everyone else.

## Configuration

`BepInEx/config/tlisonbee.valheim.otmegingjord.cfg`, with a section per
level (`Price`, `CarryBonus`, `Materials`, `UnlockKey`) and
`MaxForgeLevel` under `General`. Restart the game after changing
`Materials` or `MaxForgeLevel`.

## Source

https://github.com/tblisonb/ot-valheim-mods/tree/main/OtMegingjord
