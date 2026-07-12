using Godot;

public partial class MeleeWeapon : Resource
{
	[Export]
	public Vector2 StartLocation {get;set;}
	[Export]
	public Vector2 EndLocation {get;set;}
	[Export]
	public int Damage {get;set;}
	[Export]
	public float SwingDuration {get;set;} = 0.3f;
	// Minimum time between swings. Also scaled by the player's swing-speed upgrades.
	[Export]
	public float SwingCooldown {get;set;} = 0.5f;
	[Export]
	public float WidthMultiplier {get;set;}
	[Export]
	public float HeightMultiplier {get;set;}
	[Export]
	public float SwingArc {get;set;} = Mathf.Pi * 0.75f;
	[Export]
	public float OffsetDistance {get;set;} = 90f;
	//Attack is meant to hold the animation, and the collider.
	[Export]
	public PackedScene Attack {get;set;}
}
