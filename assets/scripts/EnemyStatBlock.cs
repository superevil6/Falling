using Godot;

public partial class EnemyStatBlock : Resource
{
	[Export]
	public int MaxHealth {get;set;}
	[Export]
	public int DamageReduction {get;set;}
	[Export]
	public Gun Gun {get;set;}
	[Export]
	public MeleeWeapon Melee {get;set;}
	[Export]
	public MovementType MovementType {get;set;}
	[Export]
	public int SpawnMovementSpeed { get; set; } = 5;
	[Export]
	public bool InstantSpawn { get; set; } = false;
	[Export]
	public float MovementSpeed {get;set;}
	[Export]
	public float MovementLimit {get;set;}
	[Export]
	public float Size {get;set;}
	[Export]
	public Texture2D EnemySprite {get;set;}
	[Export]
	public AttackDirection AttackDirection {get; set;}
	[Export]
	public ItemDrop[] ItemDrops {get;set;}
	[Export]
	public bool IsBoss { get; set; }
	[Export]
	public bool TeleportMovement {get;set;} = false;
	[Export]
	public float TeleportHesitationTime {get;set;} = 0.5f;
	[Export]
	public bool DropsBombs {get;set;} = false;
	[Export]
	public int BombDamage {get;set;} = 5;
	[Export]
	public float BombRadius {get;set;} = 100f;
	[Export]
	public float BombFuseTime {get;set;} = 2f;
	[Export]
	public float BombCooldown {get;set;} = 5f;
	[Export]
	public int BombMaxCount {get;set;} = 3;
	[Export]
	public bool UsesMines {get;set;} = false;
}
