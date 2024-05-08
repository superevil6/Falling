using Godot;

public partial class Gun : Resource
{
	[Export]
	public float FireRate {get;set;}
	[Export]
	public int Damage {get;set;}
	[Export]
	public int BulletCount {get;set;}
	[Export]
	public float BulletSpread {get;set;}
	[Export]
	public PackedScene BulletType {get;set;}
	[Export]
	public float BulletLifetime {get;set;} 
	[Export]
	public DamageType DamageType {get;set;}

}
