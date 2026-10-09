using Godot;

public partial class MeleeWeapon : Resource
{
	/// <summary>Local start position of the swing arc, relative to the wielder.</summary>
	[Export]
	public Vector2 StartLocation {get;set;}
	/// <summary>Local end position of the swing arc, relative to the wielder.</summary>
	[Export]
	public Vector2 EndLocation {get;set;}
	/// <summary>Damage dealt to each enemy the swing connects with.</summary>
	[Export]
	public int Damage {get;set;}
	/// <summary>Seconds a single swing animation takes from start to finish.</summary>
	[Export]
	public float SwingDuration {get;set;} = 0.3f;
	/// <summary>Minimum time between swings. Also scaled by the player's swing-speed upgrades.</summary>
	[Export]
	public float SwingCooldown {get;set;} = 0.5f;
	/// <summary>Horizontal scale of the attack hitbox/visual. 1 = normal width.</summary>
	[Export]
	public float WidthMultiplier {get;set;}
	/// <summary>Vertical scale of the attack hitbox/visual. 1 = normal height.</summary>
	[Export]
	public float HeightMultiplier {get;set;}
	/// <summary>Angular width of the swing arc, in radians.</summary>
	[Export]
	public float SwingArc {get;set;} = Mathf.Pi * 0.75f;
	/// <summary>Distance (pixels) the swing is offset out in front of the wielder.</summary>
	[Export]
	public float OffsetDistance {get;set;} = 90f;
	/// <summary>Attack scene that holds the swing animation and the collider/hitbox.</summary>
	[Export]
	public PackedScene Attack {get;set;}
}
