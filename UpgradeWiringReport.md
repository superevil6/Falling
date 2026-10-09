# Upgrade Wiring Report

_Generated 2026-07-11_

## Summary

Every `GunUpgradeType` and `BodyUpgradeType` value defined in code **already has a `.tres`
resource** — nothing needs to be *generated*. The real gap is that **8 existing `.tres` files are
not wired into `Main.tscn`'s upgrade pools**, so the game never offers them at level-up.

The pools live on the `Main` node in [assets/objects/Main.tscn](assets/objects/Main.tscn):

- `PossibleGunUpgrades` (line 109) — currently 29 entries
- `PossibleBodyUpgrades` (line 110) — currently 29 entries

Both are populated from `main.PossibleGunUpgrades` / `main.PossibleBodyUpgrades` and are the only
source `LevelUpMenu` draws from ([LevelUpMenu.cs:138](assets/scripts/LevelUpMenu.cs#L138),
[LevelUpMenu.cs:186](assets/scripts/LevelUpMenu.cs#L186)). A `.tres` on disk that isn't in these
arrays is dead content.

## Missing from the pools (8)

Add each to the matching array in the **Main** node's inspector (Godot editor → open `Main.tscn`
→ select the `Main` node → `Possible Gun Upgrades` / `Possible Body Upgrades`).

### Gun pool — `PossibleGunUpgrades`

| File | Upgrade | Type | Rarity | Notes |
|---|---|---|---|---|
| [Gun/Spread Shot.tres](assets/upgrades/Gun/Spread%20Shot.tres) | Spread Shot | MultiBulletAngle (3) | Common | ⚠ no icon, no uid |
| [Gun/Piercing Rounds.tres](assets/upgrades/Gun/Piercing%20Rounds.tres) | Piercing Rounds | Pierce (5) | Rare | has icon + uid |
| [Gun/Slowing Rounds.tres](assets/upgrades/Gun/Slowing%20Rounds.tres) | Slowing Rounds | Slowing (12) | Uncommon | ⚠ no icon, no uid |
| [Gun/Bullet Magnifier.tres](assets/upgrades/Gun/Bullet%20Magnifier.tres) | Bullet Magnifier | SizeMultiplier (10) | Uncommon | ⚠ no icon, no uid |
| [Gun/Grease Bullets.tres](assets/upgrades/Gun/Grease%20Bullets.tres) | Grease Bullets | Element (14) / Grease | Uncommon | has icon + uid |

### Body pool — `PossibleBodyUpgrades`

| File | Upgrade | BodyUpgradeType | Slot | Rarity | Notes |
|---|---|---|---|---|---|
| [Body/Flame Ward.tres](assets/upgrades/Body/Flame%20Ward.tres) | Flame Ward | CorrosionResistance (16) | Body | Common | ⚠ no icon, no uid |
| [Body/Energy Shield.tres](assets/upgrades/Body/Energy%20Shield.tres) | Energy Shield | Shield (24) | Body | Epic | ⚠ no icon, no uid |
| [Head/Weakness Insight.tres](assets/upgrades/Head/Weakness%20Insight.tres) | Weakness Insight | WeaknessInsight (28) | Head | Uncommon | ⚠ no icon, no uid |

## Heads-up before wiring

6 of the 8 files have **no icon assigned** and were saved **without a `uid`** (`load_steps=2`,
compared to a healthy file like `Ice Bullets.tres` at `load_steps=3` with a uid + texture). These
were likely hand-authored and never opened/saved in the editor. Before or after wiring them in,
you'll probably want to open each in the editor and set its `Upgrade Image` so it doesn't show a
blank slot on the level-up wheel:

- Gun/Spread Shot, Gun/Slowing Rounds, Gun/Bullet Magnifier
- Body/Flame Ward, Body/Energy Shield
- Head/Weakness Insight

(`Grease Bullets` and `Piercing Rounds` are complete — icon + uid — just unwired.)

## Verification method

- Enum definitions: [GunUpgradeType.cs](assets/scripts/GunUpgradeType.cs),
  [BodyUpgradeType.cs](assets/scripts/BodyUpgradeType.cs)
- Every enum value confirmed handled in [Gun.cs](assets/scripts/Gun.cs#L285) and
  [Player.cs](assets/scripts/Player.cs#L345), and confirmed to have ≥1 `.tres`.
- Disk vs. pool diff: 66 `.tres` on disk, 58 wired, 8 unwired (list above). No broken
  references (every wired entry resolves to a real file).
