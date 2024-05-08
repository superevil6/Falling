using Godot;

public partial class EnemyStatBlock : Resource
{
	[Export]
	public int MaxHealth {get;set;}
	[Export]
	public Gun Gun {get;set;}
	[Export]
	public MeleeWeapon Melee {get;set;}
	[Export]
	public float MovementSpeed {get;set;}
	[Export]
	public float Size {get;set;}
	[Export]
	public Texture2D EnemySprite {get;set;}
	[Export]
	public DamageType DamageTypeStrength {get;set;}
	[Export]
	public DamageType DamageTypeWeakness {get;set;}
}
