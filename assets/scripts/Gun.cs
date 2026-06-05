using Godot;
using System.Collections.Generic;

public partial class Gun : Resource
{
	public string SourceName {get;set;}
	public List<GunUpgrade> AppliedUpgrades {get;set;} = new List<GunUpgrade>();
	[Export]
	public Texture2D GunImage {get;set;}
	[Export]
	public GunType GunType {get; set;}
	[Export]
	public string GunDescription {get; set;}
	[Export]
	public float FireRate {get;set;}
	[Export]
	public int Damage {get;set;}
	[Export]
	public int BulletCount {get;set;}
	[Export]
	public int DirectionalCount {get;set;} = 1;
	[Export]
	public float DirectionalAngle {get;set;} = 0f;
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
	public float ExplosionRadius {get;set;} = 0f;
	[Export]
	public int Split {get;set;}
	[Export]
	public int Ricochet {get;set;}
	[Export]
	public float Wave {get;set;}
	[Export]
	public float Spiral {get;set;} = 0f;
	[Export]
	public bool Growth {get;set;} = false;
	[Export]
	public float GrowthStartSize {get;set;} = 0.3f;
	[Export]
	public float GrowthMaxSize {get;set;} = 2.0f;
	[Export]
	public float GrowthDistance {get;set;} = 600f;
	[Export]
	public float GrowthMinDamageRatio {get;set;} = 0.1f;
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
	public bool IsLightning {get;set;}
	[Export]
	public float LightningRange {get;set;} = 600f;
	[Export]
	public float LightningChainRadius {get;set;} = 220f;
	[Export]
	public int LightningMaxJumps {get;set;} = 3;
	[Export]
	public float LightningAimConeDeg {get;set;} = 30f;
	[Export]
	public bool IsChargeWeapon {get;set;}
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float CriticalChance {get;set;} = 0f;
	[Export]
	public float CriticalMultiplier {get;set;} = 2f;
	[Export]
	public float LifeSteal {get;set;} = 0f;
	[Export]
	public int AcidRoundsCount {get;set;} = 0;
	[Export]
	public int MinDamage {get;set;} = 1;
	[Export]
	public int MaxDamage {get;set;} = 10;
	[Export]
	public float MinSize {get;set;} = 1f;
	[Export]
	public float MaxSize {get;set;} = 3f;
	[Export]
	public float ChargeTime {get;set;} = 1.5f;
	[Export]
	public int DotStacksPerHit {get;set;} = 0;
	[Export]
	public int SlowStacksPerHit {get;set;} = 0;
	[Export]
	public int FireRateStacksPerHit {get;set;} = 0;
	[Export]
	public int BlindStacksPerHit {get;set;} = 0;
	[Export]
	public ElementType Element {get;set;}
	[Export]
	public int ExperiencePerLevel {get;set;} = 10;
	[Export]
	public int CurrentExperience {get;set;}
	[Export]
	public int CurrentLevel {get;set;}
	[Export]
	public GunUpgrade[] GunUpgrades {get; set;}

	public void AddExperience(int amount)
	{
		CurrentExperience += amount;
		while (ExperiencePerLevel > 0 && CurrentExperience >= ExperiencePerLevel) {
			CurrentExperience -= ExperiencePerLevel;
			CurrentLevel++;
		}
	}

	public void ApplyUpgrade(GunUpgrade upgrade)
	{
		if (upgrade == null) return;
		AppliedUpgrades.Add(upgrade);
		switch (upgrade.Type) {
			case GunUpgradeType.Damage: Damage += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.FireRate: FireRate += upgrade.Value; break;
			case GunUpgradeType.BulletCount: BulletCount += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.BulletSpread: BulletSpread += upgrade.Value; break;
			case GunUpgradeType.BulletLifeTime: BulletLifetime += upgrade.Value; break;
			case GunUpgradeType.Pierce: Pierce = upgrade.Value > 0; break;
			case GunUpgradeType.Explode: Explode = upgrade.Value > 0; break;
			case GunUpgradeType.ExplosionRadius: ExplosionRadius += upgrade.Value; break;
			case GunUpgradeType.Split: Split += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.Ricochet: Ricochet += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.Wave: Wave += upgrade.Value; break;
			case GunUpgradeType.SizeMultiplier: SizeMultiplier += upgrade.Value; break;
			case GunUpgradeType.HeatSeeking: HeatSeeking += upgrade.Value; break;
			case GunUpgradeType.Slowing: Slowing += upgrade.Value; break;
			case GunUpgradeType.Element: Element = upgrade.Element; break;
			case GunUpgradeType.BulletSize: BulletSize += upgrade.Value; break;
			case GunUpgradeType.LifeSteal: LifeSteal += upgrade.Value; break;
			case GunUpgradeType.AcidRounds: AcidRoundsCount++; break;
		}
	}
}
