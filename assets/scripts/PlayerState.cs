using Godot;

// Snapshot of the player's run progress. Used both as the in-memory carrier across
// the stage-advance reload and as the on-disk save payload (a Resource, so guns/mods
// serialize via ResourceSaver). The Gun/BodyMod resources it references are embedded
// when saved.
public partial class PlayerState : Resource
{
	[Export] public Gun[] Guns {get;set;}
	[Export] public BodyMod[] BodyMods {get;set;}
	[Export] public int MaxHealth {get;set;}
	[Export] public int CurrentHealth {get;set;}
	[Export] public int CurrentExperience {get;set;}
	[Export] public int DamageReduction {get;set;}
	[Export] public int Speed {get;set;}
	[Export] public bool HasSeeEnemyHealth {get;set;}
	[Export] public bool HasLaserSight {get;set;}
	[Export] public int FireDefenseStacks {get;set;}
	[Export] public int IceDefenseStacks {get;set;}
	[Export] public int ElectricDefenseStacks {get;set;}
	[Export] public int OrbitalShieldCount {get;set;}
	[Export] public int OrbitalMinionCount {get;set;}
	[Export] public float ItemMagnetMultiplier {get;set;}
	[Export] public float HealthRegenPerSecond {get;set;}
	[Export] public int MaxDashCharges {get;set;}
	[Export] public int CurrentDashCharges {get;set;}
	[Export] public float ShortDashDistance {get;set;}
	[Export] public float ShortDashCooldown {get;set;}
	[Export] public int MeleeDamageBonus {get;set;}
	[Export] public float MeleeRangeBonus {get;set;}
	[Export] public float MeleeArcBonus {get;set;}
	[Export] public float MeleeSwingSpeedMultiplier {get;set;}
	[Export] public float MeleeLifeSteal {get;set;}
	[Export] public float BlindResistance {get;set;}
	[Export] public float BurningResistance {get;set;}
	[Export] public float SlowResistance {get;set;}
	[Export] public float ShockResistance {get;set;}
	[Export] public int MaxShield {get;set;}
	[Export] public float CurrentShield {get;set;}
	[Export] public int MaxHealthPerKill {get;set;}
	[Export] public float ExperienceMultiplier {get;set;}
	[Export] public float ItemDropChanceMultiplier {get;set;}
}
