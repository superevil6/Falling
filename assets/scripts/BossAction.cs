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

	// --- RainLasers ---
	// Number of large lasers that rain from the top of the screen.
	[Export]
	public int RainLaserCount {get;set;} = 4;
	// Wind-up between the warning indicators appearing and the lasers falling.
	[Export]
	public float RainWarningDuration {get;set;} = 1f;
	[Export]
	public int RainLaserDamage {get;set;} = 3;
	[Export]
	public float RainLaserThickness {get;set;} = 60f;
	// How long each falling laser stays active.
	[Export]
	public float RainLaserDuration {get;set;} = 1.2f;
	// Travel time for the small beam to reach the top before the rain begins.
	[Export]
	public float RainFlareTime {get;set;} = 0.4f;
	[Export]
	public Color RainLaserColor {get;set;} = new Color(1f, 0.3f, 0.3f);

	// --- DropMine ---
	[Export]
	public int MineDamage {get;set;} = 30;
	[Export]
	public float MineRadius {get;set;} = 160f;
	// Mine scene to drop; null uses the default Mine.tscn.
	[Export]
	public PackedScene MineScene {get;set;}

	// Attack animation to play for this action (e.g. Fire1). Used by non-gun
	// actions like DropMine; gun Fire actions take their animation from the gun.
	[Export]
	public AttackNumber AttackNumber {get;set;}
}
