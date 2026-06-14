using Godot;

// A boss behavior phase. A phase becomes active once the boss's health fraction
// drops to or below HealthThreshold (phases are evaluated in array order, so list
// them from highest threshold to lowest, e.g. 1.0, 0.66, 0.33). While active, its
// Actions are run in a repeating loop.
public partial class BossPhase : Resource
{
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float HealthThreshold {get;set;} = 1.0f;
	// When true, entering this phase overrides the boss's movement.
	[Export]
	public bool OverrideMovement {get;set;} = false;
	[Export]
	public MovementType MovementType {get;set;}
	// New movement speed when OverrideMovement is set; ignored if <= 0.
	[Export]
	public float MovementSpeed {get;set;} = 0f;
	[Export]
	public BossAction[] Actions {get;set;}
}
