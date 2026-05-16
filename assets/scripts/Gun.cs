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
	public float BulletSpeed {get;set;} = 2000f;
	[Export]
	public SpriteFrames BulletSpriteFrames {get;set;}
	[Export]
	public float BulletLifetime {get;set;}
	[Export]
	public float BulletSize {get;set;} = 1f;
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
	public ElementType Element {get;set;}
	[Export]
	public int ExperiencePerLevel {get;set;} = 10;
	[Export]
	public int CurrentExperience {get;set;}
	[Export]
	public int CurrentLevel {get;set;}
	[Export]
	public int SkillPoints {get;set;}
	[Export]
	public GunUpgrade[] GunUpgrades {get; set;}

	public void AddExperience(int amount)
	{
		CurrentExperience += amount;
		while (ExperiencePerLevel > 0 && CurrentExperience >= ExperiencePerLevel) {
			CurrentExperience -= ExperiencePerLevel;
			CurrentLevel++;
			SkillPoints++;
		}
	}

	public void ApplyUpgrade(GunUpgrade upgrade)
	{
		if (upgrade == null) return;
		switch (upgrade.Type) {
			case GunUpgradeType.Damage: Damage += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.FireRate: FireRate += upgrade.Value; break;
			case GunUpgradeType.BulletCount: BulletCount += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.BulletSpread: BulletSpread += upgrade.Value; break;
			case GunUpgradeType.BulletLifeTime: BulletLifetime += upgrade.Value; break;
			case GunUpgradeType.Pierce: Pierce = upgrade.Value > 0; break;
			case GunUpgradeType.Explode: Explode = upgrade.Value > 0; break;
			case GunUpgradeType.Split: Split += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.Ricochet: Ricochet += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.Wave: Wave += upgrade.Value; break;
			case GunUpgradeType.SizeMultiplier: SizeMultiplier += upgrade.Value; break;
			case GunUpgradeType.HeatSeeking: HeatSeeking += upgrade.Value; break;
			case GunUpgradeType.Slowing: Slowing += upgrade.Value; break;
			case GunUpgradeType.Element: Element = upgrade.Element; break;
			case GunUpgradeType.BulletSize: BulletSize += upgrade.Value; break;
		}
	}
}
