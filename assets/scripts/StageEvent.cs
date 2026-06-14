using Godot;

// One scheduled stage event. The StageDirector fires these on the stage's timer.
public partial class StageEvent : Resource
{
	[Export]
	public StageEventType Type {get;set;}
	// Seconds a warning indicator shows before the event actually happens.
	[Export]
	public float WarningDuration {get;set;} = 1f;

	// --- WallContract ---
	[Export]
	public WallSide Side {get;set;} = WallSide.Random;
	// How far the wall moves in, as a fraction of the viewport width (0.1 = 10%).
	[Export(PropertyHint.Range, "0,0.45,0.01")]
	public float ContractFraction {get;set;} = 0.1f;
	// Seconds the wall takes to slide in (and back out, if not permanent).
	[Export]
	public float ContractDuration {get;set;} = 1.5f;
	// If true the wall stays contracted; if false it re-opens after HoldDuration.
	[Export]
	public bool Permanent {get;set;} = false;
	// Seconds to stay fully contracted before re-opening (ignored if Permanent).
	[Export]
	public float HoldDuration {get;set;} = 4f;

	// --- SpawnObstacle ---
	[Export]
	public PackedScene[] ObstacleScenes {get;set;}
	[Export]
	public int ObstacleCount {get;set;} = 1;
}
