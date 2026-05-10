using Godot;

public partial class Gun : Resource
{
	[Export]
	public GunType GunType {get; set;}
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
	[Export]
	public bool Pierce {get;set;}
	[Export]
	public bool Explode {get;set;}
	[Export]
	public int Split {get;set;}
	[Export]
	public int Ricochet {get;set;}
	[Export]
	public float Wave {get;set;}
	[Export]
	public float SizeMultiplier {get;set;}
	[Export]
	public float HeatSeeking {get;set;}
	[Export]
	public float Slowing {get;set;}
	[Export]
	public int DamageModifier {get;set;}
	[Export]
	public bool IsLaser {get;set;}
	[Export]
	public int ExperiencePerLevel {get;set;} = 10;
	[Export]
	public int CurrentExperience {get;set;}
	[Export]
	public int CurrentLevel {get;set;}
	[Export]
	public int SkillPoints {get;set;}

	public void AddExperience(int amount)
	{
		CurrentExperience += amount;
		while (ExperiencePerLevel > 0 && CurrentExperience >= ExperiencePerLevel) {
			CurrentExperience -= ExperiencePerLevel;
			CurrentLevel++;
			SkillPoints++;
		}
	}
}
