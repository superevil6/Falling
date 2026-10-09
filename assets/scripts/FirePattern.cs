using Godot;

// Describes how an enemy/boss gun lays out its bullets each shot. Assign it to a
// Gun's FirePattern; leave null to keep the legacy DirectionalCount/DirectionalAngle
// fan. Used by Enemy.FireGun (so it applies to both autonomous enemies and bosses).
public partial class FirePattern : Resource
{
	/// <summary>Shape the bullets are laid out in each shot: Targeted, Spread cone, Ring, or Spiral.</summary>
	[Export]
	public FirePatternType Type {get;set;} = FirePatternType.Targeted;
	/// <summary>When true the pattern aims at the player; otherwise it fires toward FixedAngleDegrees.</summary>
	[Export]
	public bool AimAtPlayer {get;set;} = true;
	/// <summary>World angle (degrees) used when AimAtPlayer is false. 0 = right, 90 = down.</summary>
	[Export]
	public float FixedAngleDegrees {get;set;} = 90f;
	/// <summary>
	/// Number of bullets in the shape: the cone fan count (Spread), the ring count
	/// (Ring), or the number of evenly spaced arms (Spiral).
	/// </summary>
	[Export]
	public int Count {get;set;} = 1;
	/// <summary>Total arc (degrees) the bullets span for a Spread cone.</summary>
	[Export]
	public float ArcDegrees {get;set;} = 45f;
	/// <summary>Degrees the aim advances each shot for a Spiral.</summary>
	[Export]
	public float SpinDegrees {get;set;} = 15f;
}
