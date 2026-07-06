# Status effect icons

Drop one PNG here per status effect, named exactly after the `StatusEffectType` enum
member. These are shown (with the stack count) floating below any player/enemy that
has the status:

- `DamageOverTime.png`
- `Slow.png`
- `ReducedFireRate.png`
- `Blind.png`

Until a file exists for a status, its stacks show as a colored placeholder dot.
Loaded lazily and cached at runtime (see `StatusEffectVisuals.cs`); Godot must have
imported the PNG (open the editor once after adding it) so the matching `.import`
exists.
