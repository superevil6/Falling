using Godot;

// Describes how an enemy/boss gun lays out its bullets each shot. Assign it to a
// Gun's FirePattern; leave null to keep the legacy DirectionalCount/DirectionalAngle
// fan. Used by Enemy.FireGun (so it applies to both autonomous enemies and bosses).
public partial class FirePattern : Resource
{
	[Export]
	public FirePatternType Type {get;set;} = FirePatternType.Targeted;
	// When true the pattern aims at the player; otherwise it fires toward FixedAngleDegrees.
	[Export]
	public bool AimAtPlayer {get;set;} = true;
	// World angle (degrees) used when AimAtPlayer is false. 0 = right, 90 = down.
	[Export]
	public float FixedAngleDegrees {get;set;} = 90f;
	// Number of bullets in the shape: the cone fan count (Spread), the ring count
	// (Ring), or the number of evenly spaced arms (Spiral).
	[Export]
	public int Count {get;set;} = 1;
	// Total arc (degrees) the bullets span for a Spread cone.
	[Export]
	public float ArcDegrees {get;set;} = 45f;
	// Degrees the aim advances each shot for a Spiral.
	[Export]
	public float SpinDegrees {get;set;} = 15f;
}
