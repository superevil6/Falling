using Godot;
using System.Collections.Generic;

public partial class Gun : Resource
{
	/// <summary>Display name of this weapon, used in menus, logs, and level-up math.</summary>
	[Export]
	public string SourceName {get;set;}
	public List<GunUpgrade> AppliedUpgrades {get;set;} = new List<GunUpgrade>();
	// Base FireRate/ChargeTime captured before the first FireRate upgrade, so a charge
	// weapon's ChargeTime can be kept proportional to how much FireRate has improved.
	private bool chargeRateBaseCaptured = false;
	private float baseFireRate;
	private float baseChargeTime;
	/// <summary>Icon shown for this gun in the UI (selection/upgrade menus, HUD).</summary>
	[Export]
	public Texture2D GunImage {get;set;}
	/// <summary>Which weapon this is. Drives special firing logic keyed off the gun type.</summary>
	[Export]
	public GunType GunType {get; set;}
	/// <summary>Flavor/summary text shown to the player when picking or inspecting this gun.</summary>
	[Export]
	public string GunDescription {get; set;}
	/// <summary>
	/// Selects which attack animation the wielder plays when firing this gun (e.g.
	/// Fire1/Fire2/Fire3). Default uses the generic "Fire"/"Shoot" animation.
	/// </summary>
	[Export]
	public AttackNumber AttackNumber {get; set;}
	/// <summary>Shots per second. Higher = faster firing. Boosted by FireRate upgrades.</summary>
	[Export]
	public float FireRate {get;set;}
	/// <summary>Base damage dealt by a single bullet before crits, elements, and modifiers.</summary>
	[Export]
	public int Damage {get;set;}
	/// <summary>Bullets fired per shot along the same aim line (a burst/stream of shots).</summary>
	[Export]
	public int BulletCount {get;set;}
	/// <summary>Number of distinct aim directions fired each shot (e.g. 2 = front+back). Spread evenly around a circle by MultiDirectionalShot upgrades.</summary>
	[Export]
	public int DirectionalCount {get;set;} = 1;
	/// <summary>Angle (degrees) between the DirectionalCount firing directions.</summary>
	[Export]
	public float DirectionalAngle {get;set;} = 0f;
	/// <summary>
	/// Optional shot-shape for enemy/boss fire (cone, ring, spiral, fixed direction).
	/// Null keeps the legacy DirectionalCount/DirectionalAngle fan.
	/// </summary>
	[Export]
	public FirePattern FirePattern {get;set;}
	/// <summary>Total spread angle (degrees) fanned across the BulletCount shots in one burst.</summary>
	[Export]
	public float MultiBulletAngle {get;set;}
	/// <summary>Random aim jitter (degrees) applied per shot for inaccuracy. 0 = perfectly accurate.</summary>
	[Export]
	public float BulletSpread {get;set;} = 0f;
	/// <summary>Scene instantiated for each bullet this gun fires.</summary>
	[Export]
	public PackedScene BulletType {get;set;}
	/// <summary>Sound played each time this gun fires (player or enemy).</summary>
	[Export]
	public AudioStream FireSound {get;set;}
	/// <summary>Colour of the burst of light flashed at the muzzle each time this gun fires. Personalize per weapon.</summary>
	[Export]
	public Color MuzzleFlashColor {get;set;} = new Color(1f, 0.85f, 0.5f);
	/// <summary>On-screen reach (pixels) of the muzzle flash light. 0 disables the flash for this gun.</summary>
	[Export]
	public float MuzzleFlashRadius {get;set;} = 120f;
	/// <summary>Brightness of the muzzle flash light at the instant of firing.</summary>
	[Export]
	public float MuzzleFlashEnergy {get;set;} = 1.6f;
	/// <summary>Distance (pixels) the muzzle flash sits forward of the shooter, along the aim direction (barrel length).</summary>
	[Export]
	public float MuzzleFlashOffset {get;set;} = 34f;
	/// <summary>How fast bullets travel, in pixels per second.</summary>
	[Export]
	public float BulletSpeed {get;set;} = 2000f;
	/// <summary>Animated sprite frames used for the bullet's visual. Overrides the bullet scene's default look.</summary>
	[Export]
	public SpriteFrames BulletSpriteFrames {get;set;}
	/// <summary>Seconds a bullet lives before despawning (also caps its travel range).</summary>
	[Export]
	public float BulletLifetime {get;set;}
	/// <summary>Visual/hitbox scale of the bullet. 1 = normal size.</summary>
	[Export]
	public float BulletSize {get;set;} = 1f;
	/// <summary>When true, bullets pass through enemies instead of stopping on first hit.</summary>
	[Export]
	public bool Pierce {get;set;}
	/// <summary>When true, bullets detonate on impact, dealing area damage in ExplosionRadius.</summary>
	[Export]
	public bool Explode {get;set;}
	/// <summary>Radius (pixels) of the explosion when Explode is true.</summary>
	[Export]
	public float ExplosionRadius {get;set;} = 0f;
	/// <summary>
	/// Gas rounds: on impact (enemy, obstacle, or wall) the bullet leaves a lingering
	/// gas cloud that damages whoever is inside it over time and then fades.
	/// </summary>
	[Export]
	public bool Gas {get;set;} = false;
	/// <summary>Radius (pixels) of the gas cloud left by gas rounds.</summary>
	[Export]
	public float GasRadius {get;set;} = 60f;
	/// <summary>How long (seconds) the gas cloud lingers before fading.</summary>
	[Export]
	public float GasDuration {get;set;} = 4f;
	/// <summary>Seconds between damage ticks for anyone standing in the gas cloud.</summary>
	[Export]
	public float GasDamageInterval {get;set;} = 0.5f;
	/// <summary>Damage per tick. 0 = derive from the bullet's damage (a quarter, min 1).</summary>
	[Export]
	public int GasDamage {get;set;} = 0;
	/// <summary>Number of extra bullets a bullet splits into on impact/expiry (fragmentation).</summary>
	[Export]
	public int Split {get;set;}
	/// <summary>
	/// "Carrier" bullet: while alive it periodically fires sub-bullets that deal half
	/// its damage. Sub-bullets don't carry the gun, so they never recurse or explode.
	/// </summary>
	[Export]
	public bool BulletShootsBullets {get;set;} = false;
	/// <summary>Seconds between each burst of sub-bullets from a carrier bullet.</summary>
	[Export]
	public float SubBulletInterval {get;set;} = 0.25f;
	/// <summary>Sub-bullets emitted per burst from a carrier bullet.</summary>
	[Export]
	public int SubBulletCount {get;set;} = 1;
	/// <summary>Rotates the emission directions each burst, for a spiral spray.</summary>
	[Export]
	public float SubBulletSpinDegrees {get;set;} = 40f;
	/// <summary>Sub-bullet speed; 0 = derive from the carrier's speed.</summary>
	[Export]
	public float SubBulletSpeed {get;set;} = 0f;
	/// <summary>Scene for sub-bullets; null = reuse the gun's BulletType.</summary>
	[Export]
	public PackedScene SubBulletType {get;set;}
	/// <summary>Number of times a bullet bounces off walls/enemies before dying.</summary>
	[Export]
	public int Ricochet {get;set;}
	/// <summary>Amplitude of a sine-wave flight path. 0 = straight line.</summary>
	[Export]
	public float Wave {get;set;}
	/// <summary>Bends the bullet's path into a spiral. 0 = straight line.</summary>
	[Export]
	public float Spiral {get;set;} = 0f;
	/// <summary>When true, bullets arc outward then return toward the shooter like a boomerang.</summary>
	[Export]
	public bool IsBoomerang {get;set;} = false;
	/// <summary>Farthest distance (pixels) a boomerang bullet reaches before returning.</summary>
	[Export]
	public float BoomerangMaxRadius {get;set;} = 200f;
	/// <summary>Total time (seconds) for a boomerang's full out-and-back flight.</summary>
	[Export]
	public float BoomerangDuration {get;set;} = 1.0f;
	/// <summary>Fraction of the flight (seconds) spent travelling outward before returning.</summary>
	[Export]
	public float BoomerangOutwardTime {get;set;} = 0.3f;
	/// <summary>Which way the boomerang arc curves: 1 = clockwise, -1 = counter-clockwise.</summary>
	[Export]
	public int BoomerangArcDirection {get;set;} = 1;
	/// <summary>When true, the bullet grows as it travels, gaining size (and damage) with distance.</summary>
	[Export]
	public bool Growth {get;set;} = false;
	/// <summary>Bullet size at the moment of firing when Growth is on.</summary>
	[Export]
	public float GrowthStartSize {get;set;} = 0.3f;
	/// <summary>Maximum bullet size reached at full growth distance.</summary>
	[Export]
	public float GrowthMaxSize {get;set;} = 2.0f;
	/// <summary>Distance (pixels) over which a growing bullet ramps from start to max size.</summary>
	[Export]
	public float GrowthDistance {get;set;} = 600f;
	/// <summary>Damage floor as a fraction of full damage while the bullet is still small (0.1 = 10%).</summary>
	[Export]
	public float GrowthMinDamageRatio {get;set;} = 0.1f;
	/// <summary>Flat multiplier applied on top of BulletSize. Added to by SizeMultiplier upgrades.</summary>
	[Export]
	public float SizeMultiplier {get;set;}
	/// <summary>Homing strength: how sharply bullets steer toward enemies. 0 = no homing.</summary>
	[Export]
	public float HeatSeeking {get;set;}
	/// <summary>Movement-slow applied to enemies on hit. 0 = none.</summary>
	[Export]
	public float Slowing {get;set;}
	/// <summary>Flat damage added to (or subtracted from) each hit after other calculations.</summary>
	[Export]
	public int DamageModifier {get;set;}
	/// <summary>When true, this weapon fires a continuous laser beam instead of discrete bullets.</summary>
	[Export]
	public bool IsLaser {get;set;}
	/// <summary>When true, this weapon fires chaining lightning that jumps between enemies.</summary>
	[Export]
	public bool IsLightning {get;set;}
	/// <summary>Max distance (pixels) the lightning reaches for its first target.</summary>
	[Export]
	public float LightningRange {get;set;} = 600f;
	/// <summary>Radius (pixels) searched for the next enemy when lightning chains/jumps.</summary>
	[Export]
	public float LightningChainRadius {get;set;} = 220f;
	/// <summary>Maximum number of enemies a single lightning bolt chains through.</summary>
	[Export]
	public int LightningMaxJumps {get;set;} = 3;
	/// <summary>Half-angle (degrees) of the cone the lightning searches within for its first target.</summary>
	[Export]
	public float LightningAimConeDeg {get;set;} = 30f;
	/// <summary>Base length (pixels) of each rendered segment of the lightning/laser beam.</summary>
	[Export]
	public float BeamSegmentBaseLength {get;set;} = 64f;
	/// <summary>When true, holding fire charges the shot; damage/size scale with charge time (see Min/Max fields).</summary>
	[Export]
	public bool IsChargeWeapon {get;set;}
	/// <summary>Chance (0-1) for a hit to critically strike for CriticalMultiplier damage.</summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float CriticalChance {get;set;} = 0f;
	/// <summary>Damage multiplier applied on a critical hit (2 = double damage).</summary>
	[Export]
	public float CriticalMultiplier {get;set;} = 2f;
	/// <summary>Fraction of damage dealt that heals the wielder. 0 = no lifesteal.</summary>
	[Export]
	public float LifeSteal {get;set;} = 0f;
	/// <summary>When true, the weapon emits a damaging aura around the wielder.</summary>
	[Export]
	public bool AuraDamages {get;set;} = false;
	/// <summary>Number of acid-round stacks applied on hit (corrosive damage-over-time).</summary>
	[Export]
	public int AcidRoundsCount {get;set;} = 0;
	/// <summary>Bullets ignore enemy armor: no reduced damage while an enemy is shielded.</summary>
	[Export]
	public bool ArmorPierce {get;set;} = false;
	/// <summary>Minimum damage of a charge weapon (dealt at zero charge).</summary>
	[Export]
	public int MinDamage {get;set;} = 1;
	/// <summary>Maximum damage of a charge weapon (dealt at full charge).</summary>
	[Export]
	public int MaxDamage {get;set;} = 10;
	/// <summary>Minimum bullet size of a charge weapon (at zero charge).</summary>
	[Export]
	public float MinSize {get;set;} = 1f;
	/// <summary>Maximum bullet size of a charge weapon (at full charge).</summary>
	[Export]
	public float MaxSize {get;set;} = 3f;
	/// <summary>Seconds of holding fire to reach full charge on a charge weapon.</summary>
	[Export]
	public float ChargeTime {get;set;} = 1.5f;
	/// <summary>Damage-over-time (corrosive) stacks applied to the target per hit.</summary>
	[Export]
	public int DotStacksPerHit {get;set;} = 0;
	/// <summary>Slow (ice) stacks applied to the target per hit.</summary>
	[Export]
	public int SlowStacksPerHit {get;set;} = 0;
	/// <summary>Fire-rate-debuff (electric) stacks applied to the target per hit.</summary>
	[Export]
	public int FireRateStacksPerHit {get;set;} = 0;
	/// <summary>Blind stacks applied to the target per hit.</summary>
	[Export]
	public int BlindStacksPerHit {get;set;} = 0;
	/// <summary>Grease stacks applied to the target per hit (reduces the target's own outgoing damage).</summary>
	[Export]
	public int GreaseStacksPerHit {get;set;} = 0;
	/// <summary>Elemental type of this weapon (Corrosive/Ice/Electric/…), driving on-hit status effects.</summary>
	[Export]
	public ElementType Element {get;set;}
	/// <summary>Base experience needed to reach the gun's next level.</summary>
	[Export]
	public int ExperiencePerLevel {get;set;} = 10;
	/// <summary>Extra experience added to the requirement for each subsequent level (curve steepness).</summary>
	[Export]
	public int ExperienceCurveStep {get;set;} = 5;
	/// <summary>Experience accumulated toward the next level. Runtime state; usually starts at 0.</summary>
	[Export]
	public int CurrentExperience {get;set;}
	/// <summary>The gun's current level. Each level-up grants +1 Damage.</summary>
	[Export]
	public int CurrentLevel {get;set;}
	/// <summary>Pool of upgrades this gun can offer/apply as it levels up.</summary>
	[Export]
	public GunUpgrade[] GunUpgrades {get; set;}

	public void AddExperience(int amount)
	{
		CurrentExperience += amount;
		int needed = Helpers.ExperienceForLevel(CurrentLevel, ExperiencePerLevel, ExperienceCurveStep);
		while (needed > 0 && CurrentExperience >= needed) {
			CurrentExperience -= needed;
			CurrentLevel++;
			Damage += 1;
			needed = Helpers.ExperienceForLevel(CurrentLevel, ExperiencePerLevel, ExperienceCurveStep);
		}
	}

	public void ApplyUpgrade(GunUpgrade upgrade)
	{
		if (upgrade == null) return;
		AppliedUpgrades.Add(upgrade);
		switch (upgrade.Type) {
			case GunUpgradeType.Damage: Damage += Mathf.RoundToInt(upgrade.Value); break;
			case GunUpgradeType.FireRate: {
				if (IsChargeWeapon && !chargeRateBaseCaptured) {
					baseFireRate = FireRate;
					baseChargeTime = ChargeTime;
					chargeRateBaseCaptured = true;
				}
				FireRate += upgrade.Value;
				// Shorten the hold-to-charge time in lockstep with the FireRate gain.
				if (IsChargeWeapon && baseFireRate > 0f) {
					float ratio = Mathf.Max(0f, FireRate / baseFireRate);
					ChargeTime = Mathf.Max(0.1f, baseChargeTime * ratio);
				}
				break;
			}
			case GunUpgradeType.BulletCount: {
				int prev = BulletCount;
				BulletCount += Mathf.RoundToInt(upgrade.Value);
				if (IsLaser && prev <= 1 && BulletCount > 1) MultiBulletAngle += 100f;
				break;
			}
			case GunUpgradeType.MultiBulletAngle: MultiBulletAngle += upgrade.Value; break;
			case GunUpgradeType.MultiDirectionalShot: {
				// Add another firing direction, then spread all directions evenly around a
				// full circle so the angle never has to be set by hand: 2 → 180°, 4 → 90°.
				DirectionalCount = Mathf.Max(1, DirectionalCount) + Mathf.Max(1, Mathf.RoundToInt(upgrade.Value));
				DirectionalAngle = 360f / DirectionalCount;
				break;
			}
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
			case GunUpgradeType.Element:
				Element = upgrade.Element;
				AddElementalStacks(upgrade.Element, 1);
				GD.Print($"Gun '{SourceName}' applied Element upgrade → Element {Element}, DoT/Slow/FireRate/Grease per hit: {DotStacksPerHit}/{SlowStacksPerHit}/{FireRateStacksPerHit}/{GreaseStacksPerHit}");
				break;
			case GunUpgradeType.ElementEnhance:
				// Repeatable enhancement: adds another per-hit stack for whichever element the
				// gun already carries, so status stacks climb +1 → +2 → +4 as more are taken.
				AddElementalStacks(Element, 1);
				GD.Print($"Gun '{SourceName}' applied Element Enhance → Element {Element}, DoT/Slow/FireRate/Grease per hit: {DotStacksPerHit}/{SlowStacksPerHit}/{FireRateStacksPerHit}/{GreaseStacksPerHit}");
				break;
			case GunUpgradeType.BulletSize: BulletSize += upgrade.Value; break;
			case GunUpgradeType.CriticalChance: CriticalChance = Mathf.Clamp(CriticalChance + upgrade.Value, 0f, 1f); break;
			case GunUpgradeType.CriticalMultiplier: CriticalMultiplier += upgrade.Value; break;
			case GunUpgradeType.LifeSteal: LifeSteal += upgrade.Value; break;
			case GunUpgradeType.AcidRounds: AcidRoundsCount++; break;
			case GunUpgradeType.ArmorPierce: ArmorPierce = true; break;
			case GunUpgradeType.AuraBullets: AuraDamages = true; break;
		}
	}

	// Adds `count` per-hit status stacks for the given element to the matching field.
	// NonElemental (or an unmapped element) adds nothing.
	private void AddElementalStacks(ElementType element, int count)
	{
		switch (element) {
			case ElementType.Corrosive: DotStacksPerHit += count; break;
			case ElementType.Ice: SlowStacksPerHit += count; break;
			case ElementType.Electric: FireRateStacksPerHit += count; break;
			case ElementType.Grease: GreaseStacksPerHit += count; break;
		}
	}
}
