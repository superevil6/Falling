using Godot;

// One step in a boss attack pattern. The BossController runs a phase's actions in
// order (looping), holding each action for its Duration before advancing.
public partial class BossAction : Resource
{
	[Export]
	public BossActionType Type {get;set;}
	// Seconds to wait on this action before advancing to the next one. For
	// Telegraph this is the wind-up time; for Wait it is the pause length.
	[Export]
	public float Duration {get;set;} = 0.5f;

	// --- Fire ---
	// Gun to fire; if null, the boss's main Gun is used.
	[Export]
	public Gun OverrideGun {get;set;}
	[Export]
	public float AimOffsetDegrees {get;set;} = 0f;

	// --- Move ---
	[Export]
	public MovementType MovementType {get;set;}

	// --- Summon ---
	[Export]
	public PackedScene[] SummonScenes {get;set;}
	// Spawn offset (relative to the boss) for each entry in SummonScenes; missing
	// entries default to Vector2.Zero.
	[Export]
	public Vector2[] SummonOffsets {get;set;}
}
