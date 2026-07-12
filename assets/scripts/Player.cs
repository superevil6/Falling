using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public int MaxHealth {get;set;}
	public int CurrentHealth;
	public int CurrentExperience;
	public int DamageReduction;
	public bool HasSeeEnemyHealth = true;
	public bool HasLaserSight = false;
	public bool HasWeaknessInsight = false;
	public int CorrosiveDefenseStacks = 0;
	public int OrbitalShieldCount = 0;
	private float orbitalAngle = 0f;
	[Export]
	public float OrbitalRadius {get;set;} = 90f;
	[Export]
	public float OrbitalRotationSpeed {get;set;} = 2.5f;
	private readonly System.Collections.Generic.List<OrbitalShield> orbitalShields = new();
	public int OrbitalMinionCount = 0;
	private float orbitalMinionAngle = 0f;
	[Export]
	public float OrbitalMinionRadius {get;set;} = 130f;
	[Export]
	public float OrbitalMinionRotationSpeed {get;set;} = 1.8f;
	[Export]
	public PackedScene MinionBullet {get;set;}
	[Export]
	public int MinionDamage {get;set;} = 1;
	[Export]
	public float MinionFireRate {get;set;} = 1.5f;
	[Export]
	public float MinionRange {get;set;} = 500f;
	[Export]
	public float MinionBulletSpeed {get;set;} = 800f;
	[Export]
	public float MinionBulletLifetime {get;set;} = 1.5f;
	private readonly System.Collections.Generic.List<OrbitalMinion> orbitalMinions = new();
	public int IceDefenseStacks = 0;
	public int ElectricDefenseStacks = 0;
	public float ItemMagnetMultiplier = 1f;
	public float HealthRegenPerSecond = 0f;
	private float healthRegenAccumulator = 0f;
	// Melee upgrade modifiers, applied when spawning a MeleeAttack (see MeleeAttack()).
	public int MeleeDamageBonus = 0;
	public float MeleeRangeBonus = 0f;
	public float MeleeArcBonus = 0f;
	public float MeleeSwingSpeedMultiplier = 1f;
	public float MeleeCooldownMultiplier = 1f;
	public float MeleeLifeSteal = 0f;
	// Status resistances (0..0.95); mirrored into StatusEffects via SyncResistances().
	public float BlindResistance = 0f;
	public float CorrosionResistance = 0f;
	public float SlowResistance = 0f;
	public float ShockResistance = 0f;
	// Rechargeable shield that absorbs damage before health.
	public int MaxShield = 0;
	public float CurrentShield = 0f;
	[Export]
	public float ShieldRegenDelay {get;set;} = 3f;
	[Export]
	public float ShieldRegenPerSecond {get;set;} = 5f;
	private float shieldRegenTimer = 0f;
	[Export]
	// Seconds of invincibility granted after taking a hit. Set to 0 to disable i-frames.
	public float InvincibilityTimer {get;set;} = 0.5f;
	// Remaining i-frame time; ticks down each physics frame. Hits are ignored while > 0.
	private float invincibilityRemaining = 0f;
	// Permanent MaxHealth gained per enemy killed (MaxHealthOnKill upgrade).
	public int MaxHealthPerKill = 0;
	public float ExperienceMultiplier = 1f;
	public float ItemDropChanceMultiplier = 1f;
	// Extra level-up choices granted by upgrades; added on top of LevelUpMenu.PickCount.
	public int PickCountBonus = 0;
	public StatusEffectController StatusEffects = new StatusEffectController();
	private float wallTouchTickTimer = 0f;
	private float gunCoolDown;
	private float meleeCoolDown;
	[Export]
	public int Speed { get;set;} = 400;
	// Fixed loadout capacity: 4 gun slots (the Top/Right/Bottom/Left rack and the
	// gun_1..gun_4 hotkeys) and 4 body-mod slots. The boss-reward menu fills empty
	// slots, so the arrays must always be this long with null holes for spare room.
	public const int GunSlots = 4;
	public const int BodyModSlots = 4;
	[Export]
	public Gun[] Guns {get;set;} = new Gun[GunSlots];
	[Export]
	public BodyMod[] BodyMods {get;set;} = new BodyMod[BodyModSlots];
	[Export]
	public BodyUpgrade[] BodyUpgrades {get; set;}
	private int currentGunIndex = 0;
	public Gun Gun => (Guns != null && Guns.Length > 0 && currentGunIndex >= 0 && currentGunIndex < Guns.Length) ? Guns[currentGunIndex] : null;
	[Export]
	public MeleeWeapon Melee {get;set;}
	// public List<Area2D> Bullets = new List<Area2D>();
	public Vector2 ScreenSize;
	public AnimatedSprite2D animatedSprite2D;
	private RandomNumberGenerator rng = new RandomNumberGenerator();
	private bool CanWallKick = false;
	[Export]
	public float WallKickPriorityTime {get;set;} //This is the amount of time before the player will leave the while if pointing away from it.
	private float RemainingWallKickPriorityTime;
	private bool IsDashing = false;
	[Export]
	public float DashDuration {get;set;} = 1;
	[Export]
	public float KnockbackDuration {get;set;} = 0.2f;
	private Vector2 knockbackVelocity = Vector2.Zero;
	private float knockbackTimer = 0f;
	[Export]
	public float ShortDashDuration {get;set;} = 0.15f;
	[Export]
	public float ShortDashCooldown {get;set;} = 1f;
	[Export]
	public int MaxDashCharges {get;set;} = 1;
	private int currentDashCharges;
	private float dashRegenTimer = 0f;
	[Export]
	public float ShortDashDistance {get;set;} = 80f;
	private bool isShortDash = false;
	private float RemainingDashTime;
	private Vector2 DashDirection;
	private float CurrentSpeedMultiplier = 1.0f;
	public bool IsTouchingWall;
	[Export]
	public float AfterImageInterval {get;set;} = 0.05f;
	[Export]
	public float AfterImageLifetime {get;set;} = 0.4f;
	[Export]
	public float AfterImageStartAlpha {get;set;} = 0.6f;
	private float TimeSinceLastAfterImage = 0f;
	private Shader afterImageShader;
	private Line2D activeLaser;
	private readonly System.Collections.Generic.List<Node2D> laserSegments = new();
	private double laserWaveTime; // advances the wavy-laser animation (Wave upgrade)
	private Line2D laserSightLine;
	private Line2D activeLightning;
	private readonly System.Collections.Generic.List<Node2D> lightningSegments = new();
	private float lightningFlashTimer = 0f;
	private float chargeAmount = 0f;
	private float chargePulseTime = 0f;
	private bool playingDamageAnim = false; // while true, the one-shot hit animation owns the sprite
	private bool isDead = false; // set once health hits 0; freezes input and plays the death sequence
	private bool gameOverShown = false; // guards the one-shot game-over menu
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		currentDashCharges = MaxDashCharges;
		CurrentHealth = MaxHealth;
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		rng.Randomize();
		if (Guns != null) {
			for (int i = 0; i < Guns.Length; i++) {
				if (Guns[i] != null) {
					// Prefer a name the gun already carries (e.g. restored from a save); only
					// fall back to the source .tres filename for fresh scene-defined guns.
					// Deriving from ResourcePath unconditionally renames every gun after a
					// load, because saved guns are embedded sub-resources whose ResourcePath
					// is the save file itself (e.g. "savegame_Story").
					string existing = Guns[i].SourceName;
					string srcName = !string.IsNullOrEmpty(existing)
						? existing
						: System.IO.Path.GetFileNameWithoutExtension(Guns[i].ResourcePath);
					Guns[i] = (Gun)Guns[i].Duplicate();
					Guns[i].SourceName = srcName;
				}
			}
		}
		if (BodyMods != null) {
			for (int i = 0; i < BodyMods.Length; i++) {
				if (BodyMods[i] != null) BodyMods[i] = (BodyMod)BodyMods[i].Duplicate();
			}
		}
		NormalizeLoadout();
		GetParent().GetNode<TextureProgressBar>("Health Bar").MaxValue = MaxHealth;
		GetParent().GetNode<TextureProgressBar>("Health Bar").Value = CurrentHealth;
		GetParent().GetNode<Label>("Experience Counter").Text = $"XP: {CurrentExperience}";
		UpdateGunLabel();
		afterImageShader = GD.Load<Shader>("res://assets/shaders/AfterImage.gdshader");
		var statusDisplay = new StatusEffectDisplay();
		statusDisplay.Controller = StatusEffects;
		AddChild(statusDisplay);
	}

	// Pads Guns/BodyMods up to their fixed slot counts, preserving existing entries and
	// leaving the rest null. The scene (and older saves) can ship arrays sized exactly to
	// their contents, which would leave the boss-reward menu no empty slot to fill.
	private void NormalizeLoadout()
	{
		Guns = EnsureCapacity(Guns, GunSlots);
		BodyMods = EnsureCapacity(BodyMods, BodyModSlots);
	}

	private static T[] EnsureCapacity<T>(T[] arr, int capacity) where T : class
	{
		if (arr != null && arr.Length >= capacity) return arr;
		var grown = new T[capacity];
		if (arr != null) System.Array.Copy(arr, grown, arr.Length);
		return grown;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (isDead) return; // dead: input is frozen; the Explode animation owns the sprite
		bool gunSwitched = false;
		if (Input.IsActionJustPressed("gun_1")) { currentGunIndex = 0; gunSwitched = true; }
		else if (Input.IsActionJustPressed("gun_2")) { currentGunIndex = 1; gunSwitched = true; }
		else if (Input.IsActionJustPressed("gun_3")) { currentGunIndex = 2; gunSwitched = true; }
		else if (Input.IsActionJustPressed("gun_4")) { currentGunIndex = 3; gunSwitched = true; }
		if (gunSwitched) {
			UpdateGunLabel();
			CancelCharge();
		}
		#region Shooting
		Vector2 aimDirection = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
		bool wantsToFire = Input.IsActionPressed("gun") && aimDirection != Vector2.Zero && Gun != null;
		if (lightningFlashTimer > 0f) {
			lightningFlashTimer -= (float)delta;
			if (lightningFlashTimer <= 0f) ClearLightning();
		}
		if (Gun != null && Gun.IsChargeWeapon) {
			ClearLaser();
			ClearLightning();
			UpdateChargeState(aimDirection, (float)delta);
		} else if (Gun != null && Gun.IsLaser && wantsToFire) {
			ClearLightning();
			UpdateLaserBeam(aimDirection);
		} else if (Gun != null && Gun.IsLightning) {
			ClearLaser();
			if (wantsToFire) UpdateLightning(aimDirection);
		} else {
			ClearLaser();
			ClearLightning();
			if (wantsToFire && gunCoolDown <= 0) {
				Shoot(aimDirection);
			}
		}
		if (gunCoolDown > 0) {
			gunCoolDown -= (float)delta;
		}
		if (HasLaserSight && aimDirection != Vector2.Zero) {
			UpdateLaserSight(aimDirection);
		} else {
			ClearLaserSight();
		}
		#endregion
		#region Sword
		if (Input.IsActionJustPressed("sword") && meleeCoolDown <= 0 && aimDirection.Normalized() != Vector2.Zero) {
			MeleeAttack(aimDirection);
		}
		if (meleeCoolDown > 0) {
			meleeCoolDown -= (float)delta;
		}
		#endregion
		if (Input.IsActionJustPressed("bomb")) {
			DropBomb();
		}
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, ScreenSize.Y * 0.1f, ScreenSize.Y * 0.9f)
		);
		int dotDamage = StatusEffects.Tick((float)delta);
		// DoT shouldn't flinch each tick, and it bypasses i-frames so it keeps ticking
		// on its own schedule even right after a bullet hit granted invincibility.
		if (dotDamage > 0) TakeDamage(dotDamage, playHitAnimation: false, triggersInvincibility: false);
		animatedSprite2D.SelfModulate = StatusEffects.GetTint();
		if (orbitalShields.Count > 0) {
			orbitalAngle += OrbitalRotationSpeed * (float)delta;
			UpdateOrbitalShieldPositions();
		}
		if (orbitalMinions.Count > 0) {
			orbitalMinionAngle += OrbitalMinionRotationSpeed * (float)delta;
			UpdateOrbitalMinionPositions();
		}
		if (HealthRegenPerSecond > 0f && CurrentHealth > 0 && CurrentHealth < MaxHealth) {
			healthRegenAccumulator += HealthRegenPerSecond * (float)delta;
			if (healthRegenAccumulator >= 1f) {
				int heal = (int)healthRegenAccumulator;
				healthRegenAccumulator -= heal;
				Heal(heal);
			}
		}
		if (MaxShield > 0 && CurrentShield < MaxShield && CurrentHealth > 0) {
			if (shieldRegenTimer > 0f) {
				shieldRegenTimer -= (float)delta;
			} else {
				CurrentShield = Mathf.Min(MaxShield, CurrentShield + ShieldRegenPerSecond * (float)delta);
			}
		}
		if (invincibilityRemaining > 0f) invincibilityRemaining -= (float)delta;
		TimeSinceLastAfterImage += (float)delta;
		bool shouldTrail = (Input.IsActionPressed("move_down") || RemainingDashTime > 0) && !IsTouchingWall;
		if (shouldTrail && TimeSinceLastAfterImage >= AfterImageInterval) {
			SpawnAfterImage();
			TimeSinceLastAfterImage = 0f;
		}
		UpdateMovementAnimation();
	}

	// Loops the "Dash" animation while dashing, or the "Dive" animation while holding
	// down; otherwise returns to idle. Runs after the shoot/sword animation logic so
	// the movement state takes visual priority. Dash outranks dive.
	private void UpdateMovementAnimation()
	{
		if (animatedSprite2D == null || playingDamageAnim || isDead) return;
		if (RemainingDashTime > 0f) {
			// The Dash art faces left; flip it horizontally when dashing rightward.
			if (DashDirection.X > 0f) animatedSprite2D.FlipH = true;
			else if (DashDirection.X < 0f) animatedSprite2D.FlipH = false;
			PlayLoopingAnimation("Dash");
		} else if (Input.IsActionPressed("move_down")) {
			animatedSprite2D.FlipH = false;
			PlayLoopingAnimation("Dive");
		} else if (animatedSprite2D.Animation == "Dash" || animatedSprite2D.Animation == "Dive") {
			animatedSprite2D.FlipH = false;
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
		} else if (animatedSprite2D.Animation == "Idle") {
			// Idle faces neutral; clear any leftover flip from a rightward dash/fire.
			animatedSprite2D.FlipH = false;
		}
	}

	// Plays the named animation looping, if the sprite has it and it isn't already current.
	private void PlayLoopingAnimation(string anim)
	{
		var frames = animatedSprite2D.SpriteFrames;
		if (frames != null && frames.HasAnimation(anim) && animatedSprite2D.Animation != anim) {
			frames.SetAnimationLoop(anim, true);
			animatedSprite2D.Animation = anim;
			animatedSprite2D.Play();
		}
	}

	public void ApplyBodyUpgrade(BodyUpgrade upgrade)
	{
		if (upgrade == null) return;
		switch (upgrade.BodyUpgradeType) {
			case BodyUpgradeType.Health:
				int delta = Mathf.RoundToInt(upgrade.Value);
				MaxHealth += delta;
				CurrentHealth += delta;
				var hb = GetParent().GetNode<TextureProgressBar>("Health Bar");
				hb.MaxValue = MaxHealth;
				hb.Value = CurrentHealth;
				break;
			case BodyUpgradeType.DamageReduction:
				DamageReduction += Mathf.RoundToInt(upgrade.Value);
				break;
			case BodyUpgradeType.MovementSpeed:
				Speed += Mathf.RoundToInt(upgrade.Value);
				break;
			case BodyUpgradeType.SeeEnemyHealth:
				HasSeeEnemyHealth = true;
				break;
			case BodyUpgradeType.ItemMagnet:
				ItemMagnetMultiplier += (upgrade.Value - 1f);
				break;
			case BodyUpgradeType.HealthRegen:
				HealthRegenPerSecond += upgrade.Value;
				break;
			case BodyUpgradeType.DashCount:
				int extra = Mathf.Max(1, Mathf.RoundToInt(upgrade.Value));
				MaxDashCharges += extra;
				currentDashCharges += extra;
				break;
			case BodyUpgradeType.DashDistance:
				ShortDashDistance += upgrade.Value;
				break;
			case BodyUpgradeType.LaserSight:
				HasLaserSight = true;
				break;
			case BodyUpgradeType.CorrosiveDefense:
				CorrosiveDefenseStacks++;
				break;
			case BodyUpgradeType.IceDefense:
				IceDefenseStacks++;
				break;
			case BodyUpgradeType.ElectricDefense:
				ElectricDefenseStacks++;
				break;
			case BodyUpgradeType.OrbitalShield:
				OrbitalShieldCount++;
				RebuildOrbitalShields();
				break;
			case BodyUpgradeType.OrbitalMinion:
				OrbitalMinionCount++;
				RebuildOrbitalMinions();
				break;
			case BodyUpgradeType.MeleeDamage:
				MeleeDamageBonus += Mathf.RoundToInt(upgrade.Value);
				break;
			case BodyUpgradeType.MeleeRange:
				MeleeRangeBonus += upgrade.Value;
				break;
			case BodyUpgradeType.MeleeSwingArc:
				MeleeArcBonus += Mathf.DegToRad(upgrade.Value);
				break;
			case BodyUpgradeType.MeleeAttackSpeed:
				MeleeSwingSpeedMultiplier += upgrade.Value / 100f;
				break;
			case BodyUpgradeType.MeleeCooldownReduction:
				MeleeCooldownMultiplier = Mathf.Max(0.1f, MeleeCooldownMultiplier * (1f - upgrade.Value / 100f));
				break;
			case BodyUpgradeType.MeleeLifeSteal:
				MeleeLifeSteal += upgrade.Value;
				break;
			case BodyUpgradeType.BlindResistance:
				BlindResistance = Mathf.Min(0.95f, BlindResistance + upgrade.Value / 100f);
				SyncResistances();
				break;
			case BodyUpgradeType.CorrosionResistance:
				CorrosionResistance = Mathf.Min(0.95f, CorrosionResistance + upgrade.Value / 100f);
				SyncResistances();
				break;
			case BodyUpgradeType.SlowResistance:
				SlowResistance = Mathf.Min(0.95f, SlowResistance + upgrade.Value / 100f);
				SyncResistances();
				break;
			case BodyUpgradeType.ShockResistance:
				ShockResistance = Mathf.Min(0.95f, ShockResistance + upgrade.Value / 100f);
				SyncResistances();
				break;
			case BodyUpgradeType.Shield:
				MaxShield += Mathf.RoundToInt(upgrade.Value);
				CurrentShield += upgrade.Value;
				break;
			case BodyUpgradeType.MaxHealthOnKill:
				MaxHealthPerKill += Mathf.RoundToInt(upgrade.Value);
				break;
			case BodyUpgradeType.ExpGainUp:
				ExperienceMultiplier += upgrade.Value / 100f;
				break;
			case BodyUpgradeType.ItemDropChanceUp:
				ItemDropChanceMultiplier += upgrade.Value / 100f;
				break;
			case BodyUpgradeType.DashCooldownReduction:
				ShortDashCooldown = Mathf.Max(0.1f, ShortDashCooldown * (1f - upgrade.Value / 100f));
				break;
			case BodyUpgradeType.WeaknessInsight:
				HasWeaknessInsight = true;
				break;
			case BodyUpgradeType.PickCount:
				PickCountBonus += LevelUpMath.PickCountBonusIncrement(upgrade.Value);
				break;
		}
	}

	// Pushes the current status-resistance fractions into the (possibly freshly-built)
	// StatusEffectController. Called after a resistance upgrade and on state restore.
	private void SyncResistances()
	{
		StatusEffects.SetResistance(StatusEffectType.Blind, BlindResistance);
		StatusEffects.SetResistance(StatusEffectType.DamageOverTime, CorrosionResistance);
		StatusEffects.SetResistance(StatusEffectType.Slow, SlowResistance);
		StatusEffects.SetResistance(StatusEffectType.ReducedFireRate, ShockResistance);
	}

	// Called by Enemy when it dies, so on-kill upgrades can react.
	public void OnEnemyKilled()
	{
		if (MaxHealthPerKill > 0 && CurrentHealth > 0) {
			MaxHealth += MaxHealthPerKill;
			CurrentHealth += MaxHealthPerKill;
			var hb = GetParent()?.GetNodeOrNull<TextureProgressBar>("Health Bar");
			if (hb != null) { hb.MaxValue = MaxHealth; hb.Value = CurrentHealth; }
		}
	}

	// Carried across the stage-advance scene reload (set by Main before reloading,
	// consumed by Main after the new player spawns).
	public static PlayerState CarriedState;

	public PlayerState CaptureState() => new PlayerState {
		Guns = Guns,
		BodyMods = BodyMods,
		MaxHealth = MaxHealth,
		CurrentHealth = CurrentHealth,
		CurrentExperience = CurrentExperience,
		DamageReduction = DamageReduction,
		Speed = Speed,
		HasSeeEnemyHealth = HasSeeEnemyHealth,
		HasWeaknessInsight = HasWeaknessInsight,
		HasLaserSight = HasLaserSight,
		CorrosiveDefenseStacks = CorrosiveDefenseStacks,
		IceDefenseStacks = IceDefenseStacks,
		ElectricDefenseStacks = ElectricDefenseStacks,
		OrbitalShieldCount = OrbitalShieldCount,
		OrbitalMinionCount = OrbitalMinionCount,
		ItemMagnetMultiplier = ItemMagnetMultiplier,
		HealthRegenPerSecond = HealthRegenPerSecond,
		MaxDashCharges = MaxDashCharges,
		CurrentDashCharges = currentDashCharges,
		ShortDashDistance = ShortDashDistance,
		ShortDashCooldown = ShortDashCooldown,
		MeleeDamageBonus = MeleeDamageBonus,
		MeleeRangeBonus = MeleeRangeBonus,
		MeleeArcBonus = MeleeArcBonus,
		MeleeSwingSpeedMultiplier = MeleeSwingSpeedMultiplier,
		MeleeCooldownMultiplier = MeleeCooldownMultiplier,
		MeleeLifeSteal = MeleeLifeSteal,
		BlindResistance = BlindResistance,
		CorrosionResistance = CorrosionResistance,
		SlowResistance = SlowResistance,
		ShockResistance = ShockResistance,
		MaxShield = MaxShield,
		CurrentShield = CurrentShield,
		MaxHealthPerKill = MaxHealthPerKill,
		ExperienceMultiplier = ExperienceMultiplier,
		ItemDropChanceMultiplier = ItemDropChanceMultiplier,
		PickCountBonus = PickCountBonus,
	};

	// Re-applies a captured snapshot (loadout + upgrade effects) onto a freshly spawned
	// player. Call after the player is in the tree (orbitals/UI need its parent).
	public void RestoreState(PlayerState s)
	{
		if (s == null) return;
		Guns = s.Guns;
		BodyMods = s.BodyMods;
		NormalizeLoadout();
		MaxHealth = s.MaxHealth;
		CurrentHealth = s.CurrentHealth;
		CurrentExperience = s.CurrentExperience;
		DamageReduction = s.DamageReduction;
		Speed = s.Speed;
		HasSeeEnemyHealth = s.HasSeeEnemyHealth;
		HasWeaknessInsight = s.HasWeaknessInsight;
		HasLaserSight = s.HasLaserSight;
		CorrosiveDefenseStacks = s.CorrosiveDefenseStacks;
		IceDefenseStacks = s.IceDefenseStacks;
		ElectricDefenseStacks = s.ElectricDefenseStacks;
		OrbitalShieldCount = s.OrbitalShieldCount;
		OrbitalMinionCount = s.OrbitalMinionCount;
		ItemMagnetMultiplier = s.ItemMagnetMultiplier;
		HealthRegenPerSecond = s.HealthRegenPerSecond;
		MaxDashCharges = s.MaxDashCharges;
		currentDashCharges = s.CurrentDashCharges;
		ShortDashDistance = s.ShortDashDistance;
		if (s.ShortDashCooldown > 0f) ShortDashCooldown = s.ShortDashCooldown;
		MeleeDamageBonus = s.MeleeDamageBonus;
		MeleeRangeBonus = s.MeleeRangeBonus;
		MeleeArcBonus = s.MeleeArcBonus;
		MeleeSwingSpeedMultiplier = s.MeleeSwingSpeedMultiplier > 0f ? s.MeleeSwingSpeedMultiplier : 1f;
		MeleeCooldownMultiplier = s.MeleeCooldownMultiplier > 0f ? s.MeleeCooldownMultiplier : 1f;
		MeleeLifeSteal = s.MeleeLifeSteal;
		BlindResistance = s.BlindResistance;
		CorrosionResistance = s.CorrosionResistance;
		SlowResistance = s.SlowResistance;
		ShockResistance = s.ShockResistance;
		SyncResistances();
		MaxShield = s.MaxShield;
		CurrentShield = s.CurrentShield;
		MaxHealthPerKill = s.MaxHealthPerKill;
		ExperienceMultiplier = s.ExperienceMultiplier > 0f ? s.ExperienceMultiplier : 1f;
		ItemDropChanceMultiplier = s.ItemDropChanceMultiplier > 0f ? s.ItemDropChanceMultiplier : 1f;
		PickCountBonus = s.PickCountBonus;
		var hb = GetParent()?.GetNodeOrNull<TextureProgressBar>("Health Bar");
		if (hb != null) { hb.MaxValue = MaxHealth; hb.Value = CurrentHealth; }
		UpdateGunLabel();
		RebuildOrbitalShields();
		RebuildOrbitalMinions();
	}

	public void UpdateGunLabel()
	{
		var gunImage = GetParent().GetNodeOrNull<TextureRect>("Gun Image");
		if (Gun == null) {
			GetParent().GetNode<Label>("Gun Label").Text = "Gun: -";
			if (gunImage != null) gunImage.Texture = null;
		} else {
			string gunName = !string.IsNullOrEmpty(Gun.SourceName)
				? Gun.SourceName
				: (!string.IsNullOrEmpty(Gun.ResourcePath)
					? System.IO.Path.GetFileNameWithoutExtension(Gun.ResourcePath)
					: "-");
			GetParent().GetNode<Label>("Gun Label").Text =
				$"Gun: {gunName}  Lv{Gun.CurrentLevel}  XP {Gun.CurrentExperience}/{Gun.ExperiencePerLevel}";
			if (gunImage != null) gunImage.Texture = Gun.GunImage;
		}
		SetGunSlot("Gun Slot Top", 0);
		SetGunSlot("Gun Slot Right", 1);
		SetGunSlot("Gun Slot Bottom", 2);
		SetGunSlot("Gun Slot Left", 3);
	}

	private void SetGunSlot(string nodeName, int idx)
	{
		var slot = GetParent().GetNodeOrNull<TextureRect>(nodeName);
		if (slot == null) return;
		Gun g = (Guns != null && idx < Guns.Length) ? Guns[idx] : null;
		if (g == null || g.GunImage == null) {
			slot.Texture = null;
			slot.Modulate = new Color(1f, 1f, 1f, 0.3f);
		} else {
			slot.Texture = g.GunImage;
			slot.Modulate = (idx == currentGunIndex)
				? Colors.White
				: new Color(1f, 1f, 1f, 0.5f);
		}
	}

	private void SpawnAfterImage()
	{
		var frames = animatedSprite2D.SpriteFrames;
		if (frames == null) return;
		var ghost = new Sprite2D();
		ghost.Texture = frames.GetFrameTexture(animatedSprite2D.Animation, animatedSprite2D.Frame);
		ghost.GlobalPosition = animatedSprite2D.GlobalPosition;
		ghost.Rotation = animatedSprite2D.GlobalRotation;
		ghost.Scale = animatedSprite2D.GlobalScale;
		ghost.FlipH = animatedSprite2D.FlipH;
		ghost.FlipV = animatedSprite2D.FlipV;
		ghost.TextureFilter = animatedSprite2D.TextureFilter;
		ghost.Modulate = new Color(1, 1, 1, AfterImageStartAlpha);
		var mat = new ShaderMaterial();
		mat.Shader = afterImageShader;
		ghost.Material = mat;
		GetParent().AddChild(ghost);
		ghost.ZIndex = ZIndex - 1;
		var tween = ghost.CreateTween();
		tween.TweenProperty(ghost, "modulate:a", 0.0f, AfterImageLifetime);
		tween.TweenCallback(Callable.From(ghost.QueueFree));
	}
 	public void GetInput()
	{
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		float effectiveSpeed = Speed * CurrentSpeedMultiplier * StatusEffects.GetSpeedMultiplier();
		Vector2 velocity = inputDirection * effectiveSpeed;
		if (!IsTouchingWall) {
			if (velocity.Y > 0) velocity.Y *= 1.5f;
			else if (velocity.Y < 0) velocity.Y *= 0.75f;
		}
		if (CanWallKick) velocity.X = 0;
		Velocity = velocity;
	}

	private void _on_area_2d_area_entered(Node2D node2D){
		// Match on type, not name: Godot auto-renames colliding siblings ("Bullet" ->
		// "@Bullet@2", ...), so only one bullet at a time is literally named "Bullet".
		// A name check would silently ignore every other bullet's hit.
		if (node2D is Attack attack) {
			var elem = (node2D is Bullet b) ? b.Element : ElementType.NonElemental;
			TakeDamage(attack.Damage, elem);
		}
	}

	public void TakeDamage(int amount, ElementType element = ElementType.NonElemental, bool playHitAnimation = true, bool triggersInvincibility = true)
	{
		if (CurrentHealth <= 0) return;
		if (triggersInvincibility) {
			if (invincibilityRemaining > 0f) return;
			invincibilityRemaining = InvincibilityTimer;
		}
		int stacks = element switch {
			ElementType.Corrosive => CorrosiveDefenseStacks,
			ElementType.Ice => IceDefenseStacks,
			ElementType.Electric => ElectricDefenseStacks,
			_ => 0,
		};
		shieldRegenTimer = ShieldRegenDelay;
		var hit = Combat.ResolvePlayerHit(amount, stacks, DamageReduction, CurrentShield);
		CurrentShield = hit.RemainingShield;
		if (hit.ShieldAbsorbed > 0) FloatingDamageText.Spawn(this, GlobalPosition, hit.ShieldAbsorbed, new Color(0.4f, 0.8f, 1f));
		int finalDamage = hit.HealthLoss;
		CurrentHealth -= finalDamage;
		GetParent().GetNode<TextureProgressBar>("Health Bar").Value = CurrentHealth;
		if (finalDamage > 0) {
			FloatingDamageText.Spawn(this, GlobalPosition, finalDamage, FloatingDamageText.ElementColor(element, new Color(1f, 0.4f, 0.4f)));
			Sfx.PlayHit(this);
		}
		if (CurrentHealth <= 0) {
			Die();
			return;
		}
		if (finalDamage > 0 && playHitAnimation) PlayDamageAnimation();
		FlashRed();
	}

	// Begins the death sequence: freezes the player, clears active weapon visuals,
	// wipes the roguelite save (permadeath), and plays the Explode animation. The
	// game-over menu opens when that animation finishes (see the animation_finished
	// handler), or immediately if the sprite has no Explode animation.
	private void Die()
	{
		if (isDead) return;
		isDead = true;
		CurrentHealth = 0;
		Velocity = Vector2.Zero;
		ClearLaser();
		ClearLightning();
		ClearLaserSight();
		CancelCharge();
		// Roguelite runs are permadeath — end the run by deleting its save.
		if (GameSession.Mode == GameMode.Roguelite) {
			SaveSystem.Delete(GameMode.Roguelite);
		}
		PlayExplodeAnimation();
	}

	// Plays the one-shot "Explode" death animation. If it's missing, jumps straight
	// to the game-over menu so death never softlocks.
	private void PlayExplodeAnimation()
	{
		playingDamageAnim = false;
		var frames = animatedSprite2D?.SpriteFrames;
		if (frames == null || !frames.HasAnimation("Explode")) {
			ShowGameOver();
			return;
		}
		flashTween?.Kill();
		animatedSprite2D.Modulate = Colors.White;
		frames.SetAnimationLoop("Explode", false);
		animatedSprite2D.Animation = "Explode";
		animatedSprite2D.Play();
	}

	// Opens the GAME OVER menu and pauses the game behind it.
	private void ShowGameOver()
	{
		if (gameOverShown) return;
		gameOverShown = true;
		GetParent().AddChild(new GameOverMenu());
		GetTree().Paused = true;
	}

	// Plays the "Damage" hit-reaction once; while it runs it owns the sprite (shooting,
	// dash and dive defer to it), then it returns to idle on finish.
	private void PlayDamageAnimation()
	{
		if (animatedSprite2D == null) return;
		var frames = animatedSprite2D.SpriteFrames;
		if (frames == null || !frames.HasAnimation("Damage")) return;
		frames.SetAnimationLoop("Damage", false);
		playingDamageAnim = true;
		animatedSprite2D.Animation = "Damage";
		animatedSprite2D.Play();
	}

	// Sets a one-shot action animation (shooting/sword) unless the hit reaction owns the sprite.
	// When an aim direction is given, the art (which faces left) is flipped to point rightward.
	private void PlayActionAnimation(string anim, Vector2 aimDirection = default)
	{
		if (playingDamageAnim || animatedSprite2D == null) return;
		if (aimDirection.X > 0f) animatedSprite2D.FlipH = true;
		else if (aimDirection.X < 0f) animatedSprite2D.FlipH = false;
		animatedSprite2D.Animation = anim;
		animatedSprite2D.Play();
	}

	private void RebuildOrbitalShields()
	{
		foreach (var s in orbitalShields) if (IsInstanceValid(s)) s.QueueFree();
		orbitalShields.Clear();
		for (int i = 0; i < OrbitalShieldCount; i++) {
			var shield = new OrbitalShield();
			AddChild(shield);
			orbitalShields.Add(shield);
		}
		UpdateOrbitalShieldPositions();
	}

	private void UpdateOrbitalShieldPositions()
	{
		int n = orbitalShields.Count;
		if (n == 0) return;
		float step = Mathf.Tau / n;
		for (int i = 0; i < n; i++) {
			float a = orbitalAngle + i * step;
			orbitalShields[i].Position = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * OrbitalRadius;
		}
	}

	private void RebuildOrbitalMinions()
	{
		foreach (var m in orbitalMinions) if (IsInstanceValid(m)) m.QueueFree();
		orbitalMinions.Clear();
		for (int i = 0; i < OrbitalMinionCount; i++) {
			var minion = new OrbitalMinion();
			minion.BulletScene = MinionBullet;
			minion.Damage = MinionDamage;
			minion.FireRate = MinionFireRate;
			minion.Range = MinionRange;
			minion.BulletSpeed = MinionBulletSpeed;
			minion.BulletLifetime = MinionBulletLifetime;
			AddChild(minion);
			orbitalMinions.Add(minion);
		}
		UpdateOrbitalMinionPositions();
	}

	private void UpdateOrbitalMinionPositions()
	{
		int n = orbitalMinions.Count;
		if (n == 0) return;
		float step = Mathf.Tau / n;
		for (int i = 0; i < n; i++) {
			float a = orbitalMinionAngle + i * step;
			orbitalMinions[i].Position = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * OrbitalMinionRadius;
		}
	}

	public void Heal(int amount)
	{
		if (amount <= 0 || CurrentHealth <= 0) return;
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
		var hb = GetParent()?.GetNodeOrNull<TextureProgressBar>("Health Bar");
		if (hb != null) hb.Value = CurrentHealth;
	}

	public void ApplyKnockback(Vector2 direction, float speed)
	{
		knockbackVelocity = direction.Normalized() * speed;
		knockbackTimer = KnockbackDuration;
	}

	private void ApplyWallStatusTick(Wall wall, float delta)
	{
		wallTouchTickTimer -= delta;
		if (wallTouchTickTimer > 0f) return;
		if (wall.DotStacksPerTick > 0) StatusEffects.AddStacks(StatusEffectType.DamageOverTime, wall.DotStacksPerTick);
		if (wall.SlowStacksPerTick > 0) StatusEffects.AddStacks(StatusEffectType.Slow, wall.SlowStacksPerTick);
		if (wall.FireRateStacksPerTick > 0) StatusEffects.AddStacks(StatusEffectType.ReducedFireRate, wall.FireRateStacksPerTick);
		if (wall.BlindStacksPerTick > 0) StatusEffects.AddStacks(StatusEffectType.Blind, wall.BlindStacksPerTick);
		wallTouchTickTimer = wall.StatusTickInterval > 0f ? wall.StatusTickInterval : 1f;
	}

	private static PackedScene bombScene;

	private void DropBomb()
	{
		if (bombScene == null) {
			bombScene = GD.Load<PackedScene>("res://assets/objects/Bomb.tscn");
		}
		if (bombScene == null) return;
		var bomb = bombScene.Instantiate<Bomb>();
		bomb.GlobalPosition = GlobalPosition;
		GetParent().AddChild(bomb);
	}

	private Tween flashTween;

	private void FlashRed()
	{
		if (animatedSprite2D == null) return;
		flashTween?.Kill();
		animatedSprite2D.Modulate = new Color(1f, 0.3f, 0.3f);
		flashTween = CreateTween();
		flashTween.TweenProperty(animatedSprite2D, "modulate", Colors.White, 0.2f);
	}

	public override void _PhysicsProcess(double delta) //TODO Wall kicking variables can probably be removed in favor of checking float time remaining
	{
		if (isDead) { Velocity = Vector2.Zero; return; } // dead: stop moving while the death sequence plays
		if (knockbackTimer > 0f) {
			Velocity = knockbackVelocity;
			MoveAndSlide();
			knockbackTimer -= (float)delta;
			return;
		}
		if (!IsDashing) {
			GetInput();
			MoveAndSlide();
		}
		if (Input.IsActionJustReleased("Dash")) {
			IsDashing = false;
			CanWallKick = false;
			RemainingDashTime = 0;
			RemainingWallKickPriorityTime = 0;
		}
		CurrentSpeedMultiplier = 1.0f;
		IsTouchingWall = false;
		if (GetSlideCollisionCount() > 0) {
			if (((Node2D)GetSlideCollision(0).GetCollider()).IsInGroup("Wall")) {
				IsTouchingWall = true;
				if (GetSlideCollision(0).GetCollider() is WallBody wb && wb.WallData != null) {
					CurrentSpeedMultiplier = wb.WallData.SpeedReduction;
					ApplyWallStatusTick(wb.WallData, (float)delta);
				}
				if (Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized().X >= 0.5f 
				|| Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized().X <= -0.5f){
					RemainingWallKickPriorityTime -= (float)delta;
					if (RemainingWallKickPriorityTime <= 0) {
						GetInput();
						MoveAndSlide();
						CanWallKick = false;
					}
				} else {
					RemainingWallKickPriorityTime = WallKickPriorityTime;
					CanWallKick = true;
				}
				if (Input.IsActionJustPressed("Dash")) {
					RemainingDashTime = DashDuration;
					DashDirection= Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized();
					isShortDash = false;
				}
			}

		}
		if (!IsTouchingWall) wallTouchTickTimer = 0f;
		if (currentDashCharges < MaxDashCharges) {
			dashRegenTimer -= (float)delta;
			if (dashRegenTimer <= 0f) {
				currentDashCharges++;
				dashRegenTimer = currentDashCharges < MaxDashCharges ? ShortDashCooldown : 0f;
			}
		}
		if (Input.IsActionJustPressed("Dash") && !IsTouchingWall
			&& currentDashCharges > 0 && RemainingDashTime <= 0) {
			Vector2 dashDir = Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized();
			if (dashDir != Vector2.Zero) {
				RemainingDashTime = ShortDashDuration;
				DashDirection = dashDir;
				currentDashCharges--;
				if (dashRegenTimer <= 0f) dashRegenTimer = ShortDashCooldown;
				isShortDash = true;
			}
		}
		if (RemainingDashTime > 0) {
			float dashVelocity = isShortDash
				? ShortDashDistance / Mathf.Max(ShortDashDuration, 0.001f)
				: Speed * 400f * (float)delta;
			Velocity = DashDirection.Normalized() * dashVelocity;
			MoveAndSlide();
			RemainingDashTime -= (float)delta;
			if (RemainingDashTime <= 0) isShortDash = false;
		}
	}


	private void UpdateChargeState(Vector2 aimDirection, float delta) {
		if (gunCoolDown > 0f) {
			ApplyChargeGlow(0f);
			return;
		}
		bool holding = Input.IsActionPressed("gun") && aimDirection != Vector2.Zero;
		if (holding) {
			chargeAmount = Mathf.Min(chargeAmount + delta, Gun.ChargeTime);
			chargePulseTime += delta;
		} else {
			chargePulseTime = 0f;
		}
		if (Input.IsActionJustReleased("gun") && chargeAmount > 0f && aimDirection != Vector2.Zero) {
			FireCharged(aimDirection);
			gunCoolDown = Gun.FireRate * StatusEffects.GetFireRateMultiplier();
			chargeAmount = 0f;
		}
		if (Input.IsActionJustReleased("gun") && aimDirection == Vector2.Zero) {
			chargeAmount = 0f;
		}
		float ratio = Gun.ChargeTime > 0f ? chargeAmount / Gun.ChargeTime : 0f;
		ApplyChargeGlow(ratio);
	}

	private void CancelCharge() {
		chargeAmount = 0f;
		chargePulseTime = 0f;
		ApplyChargeGlow(0f);
	}

	private void ApplyChargeGlow(float ratio) {
		ratio = Mathf.Clamp(ratio, 0f, 1f);
		QueueRedraw();
		if (ratio <= 0f) {
			animatedSprite2D.Modulate = Colors.White;
			return;
		}
		bool maxed = ratio >= 1f;
		float pulse = 0.85f + 0.15f * Mathf.Sin(chargePulseTime * (maxed ? 24f : 14f));
		Color target = maxed ? new Color(2.0f, 1.6f, 0.5f, 1f) : new Color(1.3f, 1.5f, 2.0f, 1f);
		animatedSprite2D.Modulate = Colors.White.Lerp(target, ratio * pulse);
	}

	public override void _Draw()
	{
		if (Gun == null || !Gun.IsChargeWeapon || chargeAmount <= 0f) return;
		float ratio = Gun.ChargeTime > 0f ? Mathf.Clamp(chargeAmount / Gun.ChargeTime, 0f, 1f) : 1f;
		bool maxed = ratio >= 1f;
		Color c = maxed ? new Color(1f, 0.85f, 0.2f) : new Color(0.3f, 0.6f, 1f);
		float pulse = 0.85f + 0.15f * Mathf.Sin(chargePulseTime * (maxed ? 24f : 14f));
		float r = Mathf.Lerp(35f, 95f, ratio) * pulse;
		float baseAlpha = maxed ? 1f : Mathf.Lerp(0.3f, 1f, ratio);
		DrawCircle(Vector2.Zero, r * 1.6f, new Color(c.R, c.G, c.B, 0.12f * baseAlpha));
		DrawCircle(Vector2.Zero, r * 1.1f, new Color(c.R, c.G, c.B, 0.22f * baseAlpha));
		DrawCircle(Vector2.Zero, r * 0.7f, new Color(c.R, c.G, c.B, 0.32f * baseAlpha));
	}

	private void FireCharged(Vector2 aimDirection) {
		PlayActionAnimation("Fire", aimDirection);
		float ratio = Gun.ChargeTime > 0f ? Mathf.Clamp(chargeAmount / Gun.ChargeTime, 0f, 1f) : 1f;
		int baseDamage = Combat.ChargedBaseDamage(Gun.MinDamage, Gun.MaxDamage, ratio);
		float sizeMult = Mathf.Lerp(Gun.MinSize, Gun.MaxSize, ratio);
		int dirCount = Mathf.Max(1, Gun.DirectionalCount);
		float dirStep = Mathf.DegToRad(Gun.DirectionalAngle);
		for (int d = 0; d < dirCount; d++) {
			Vector2 dir = aimDirection.Rotated(dirStep * d).Normalized();
			if (Gun.BulletSpread > 0f) {
				dir = dir.Rotated(Mathf.DegToRad(rng.RandfRange(0f, Gun.BulletSpread)));
			}
			dir = ApplyBlindSpread(dir);
			bool crit = RollCrit(Gun, out int damage, baseDamage);
			Bullet b = Gun.BulletType.Instantiate<Bullet>();
			b.Set("Direction", dir);
			b.Set("Damage", damage);
			b.Set("BulletLifetime", Gun.BulletLifetime);
			b.Gun = Gun;
			if (Gun.BulletSpeed > 0) b.BulletSpeed = Gun.BulletSpeed;
			if (Gun.Element != ElementType.NonElemental) b.Element = Gun.Element;
			b.AuraColor = crit ? CritAuraColor : new Color(0.3f, 0.6f, 1f, 0.8f);
			b.Position = Position;
			b.Rotation = dir.Angle();
			b.SetCollisionLayerValue(4, true);
			b.SetCollisionMaskValue(3, true);
			b.SetCollisionMaskValue(1, true);
			GetParent().AddChild(b);
			b.Scale *= sizeMult;
		}
	}

	private static readonly Color CritAuraColor = new Color(1f, 0.84f, 0.1f, 0.95f);

	// Deviates a fired direction by a random angle within the blind cone (0 when the
	// player isn't blinded), so a blinded shooter can't aim straight.
	private Vector2 ApplyBlindSpread(Vector2 dir) {
		float cone = StatusEffects.GetBlindSpreadDegrees();
		if (cone <= 0f) return dir;
		float half = cone * 0.5f;
		return dir.Rotated(Mathf.DegToRad(rng.RandfRange(-half, half)));
	}

	private bool RollCrit(Gun gun, out int damage, int baseDamage) {
		bool crit = Combat.RollsCrit(gun.CriticalChance, rng.Randf());
		damage = crit ? Combat.CritDamage(baseDamage, gun.CriticalMultiplier) : baseDamage;
		return crit;
	}

	private void ApplyGunHit(Enemy e, int baseDamage, Gun gun) {
		if (e == null || gun == null) return;
		RollCrit(gun, out int damage, baseDamage);
		e.TakeDamage(damage, gun.Element, gun.AcidRoundsCount, gun.ArmorPierce);
		if (gun.DotStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.DamageOverTime, gun.DotStacksPerHit);
		if (gun.SlowStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.Slow, gun.SlowStacksPerHit);
		if (gun.FireRateStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.ReducedFireRate, gun.FireRateStacksPerHit);
		if (gun.BlindStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.Blind, gun.BlindStacksPerHit);
		if (gun.LifeSteal > 0f) {
			int heal = Mathf.RoundToInt(gun.LifeSteal);
			if (heal > 0) Heal(heal);
		}
	}

	// Player-attack bullet collision, matching SpawnBulletSpread: layer bit 4, mask bits 1 and 3.
	private const uint PlayerBulletLayer = 1u << 3;
	private const uint PlayerBulletMask = (1u << 0) | (1u << 2);

	// Beam weapons (laser) have no travelling projectile, so the on-impact bullet mods
	// (Explode/Gas/Split) never fire on their own. This reuses the Bullet's own spawn logic
	// at each beam impact point so lasers inherit those mods just like bullets do. A detached
	// bullet instance (never added to the tree) supplies the Explosion/GasCloud scenes wired
	// on the gun's BulletType; Split is enemy-hit only, matching Bullet.HandleHit.
	private void SpawnBeamImpact(Vector2 point, bool hitEnemy) {
		if (Gun?.BulletType == null) return;
		if (!Gun.Explode && !Gun.Gas && !(hitEnemy && Gun.Split > 0)) return;
		var template = Gun.BulletType.Instantiate<Bullet>();
		template.Gun = Gun;
		var parent = GetParent();
		if (Gun.Explode) template.SpawnExplosionAt(parent, point, Gun.Damage);
		if (Gun.Gas) template.SpawnGasCloudAt(parent, point, Gun.Damage, Gun.Element, damagesEnemies: true);
		if (hitEnemy && Gun.Split > 0)
			template.SpawnSplitBulletsAt(parent, point, Gun.Damage, Gun.Element, PlayerBulletLayer, PlayerBulletMask);
		template.Free();
	}

	private float BeamThickness(Gun gun) {
		float t = 1f + Mathf.Max(0f, gun.SizeMultiplier);
		if (gun.BulletSize > 0f) t *= gun.BulletSize;
		return t;
	}

	private void Shoot(Vector2 aimDirection) {
		PlayActionAnimation("Fire", aimDirection);
		Sfx.Play(this, Gun.FireSound);
		int dirCount = Mathf.Max(1, Gun.DirectionalCount);
		float dirStep = Mathf.DegToRad(Gun.DirectionalAngle);
		for (int d = 0; d < dirCount; d++) {
			SpawnBulletSpread(aimDirection.Rotated(dirStep * d));
		}
		gunCoolDown = Gun.FireRate * StatusEffects.GetFireRateMultiplier();
	}

	private void SpawnBulletSpread(Vector2 aimDirection) {
		float effectiveSpread = Gun.MultiBulletAngle;
		float halfSpread = effectiveSpread / 2f;
		int n = Mathf.Max(1, Gun.BulletCount);
		bool spiral = Gun.Spiral != 0f;
		float baseAngle = aimDirection.Angle();
		for (int i = 0; i < n; i++) {
			Bullet b = Gun.BulletType.Instantiate<Bullet>();
			Vector2 direction;
			if (spiral) {
				float a = baseAngle + i * (Mathf.Tau / n);
				direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
			} else {
				float angle = n == 1 ? 0f : -halfSpread + i * (effectiveSpread / (n - 1));
				direction = aimDirection.Rotated(angle);
			}
			if (Gun.BulletSpread > 0f) {
				direction = direction.Rotated(Mathf.DegToRad(rng.RandfRange(0f, Gun.BulletSpread)));
			}
			direction = ApplyBlindSpread(direction);
			bool crit = RollCrit(Gun, out int dmg, Gun.Damage);
			b.Set("Direction", direction);
			b.Set("Damage", dmg);
			b.Set("BulletLifetime", Gun.BulletLifetime);
			b.Gun = Gun;
			if (Gun.BulletSpeed > 0) b.BulletSpeed = Gun.BulletSpeed;
			if (Gun.Element != ElementType.NonElemental) b.Element = Gun.Element;
			b.AuraColor = crit ? CritAuraColor : new Color(0.3f, 0.6f, 1f, 0.8f);
			if (spiral) {
				b.SpawnOrigin = Position;
				b.SpiralRate = Gun.Spiral;
			}
			if (Gun.IsBoomerang) {
				b.IsBoomerang = true;
				b.BoomerangOrigin = this;
				b.BoomerangMaxRadius = Gun.BoomerangMaxRadius;
				b.BoomerangDuration = Gun.BoomerangDuration;
				b.BoomerangOutwardTime = Gun.BoomerangOutwardTime;
				b.BoomerangArcDirection = Gun.BoomerangArcDirection;
				b.SpawnOrigin = Position;
			}
			b.Position = Position;
			b.Rotation = direction.Angle();
			b.SetCollisionLayerValue(4, true);
			b.SetCollisionMaskValue(3, true);
			b.SetCollisionMaskValue(1, true);
			GetParent().AddChild(b);
		}
	}

	private void UpdateLaserBeam(Vector2 aimDirection)
	{
		var spaceState = GetWorld2D().DirectSpaceState;
		Vector2 from = GlobalPosition;
		int beamCount = Mathf.Max(1, Gun.BulletCount);
		aimDirection = ApplyBlindSpread(aimDirection);
		float effectiveSpread = Gun.MultiBulletAngle;
		float halfSpread = effectiveSpread / 2f;
		var allPaths = new List<List<Vector2>>();
		var hitEnemies = new List<(Enemy enemy, Vector2 point)>();
		var hitObstacles = new List<(DestructibleObstacle obstacle, Vector2 point)>();
		for (int beam = 0; beam < beamCount; beam++) {
			float beamAngle = beamCount == 1 ? 0f : -halfSpread + beam * (effectiveSpread / (beamCount - 1));
			Vector2 beamDir = aimDirection.Normalized().Rotated(beamAngle);
			List<Vector2> path;
			if (Gun.HeatSeeking > 0f) {
				// Heat-seeking: march a curving beam toward the nearest enemy.
				path = BuildHeatSeekBeam(from, beamDir, spaceState, hitEnemies, hitObstacles);
			} else {
				Vector2 currentFrom = from;
				Vector2 currentDir = beamDir;
				path = new List<Vector2> { from };
				int bouncesLeft = Mathf.Max(0, Gun.Ricochet);
				while (true) {
					Vector2 to = currentFrom + currentDir * ScreenSize.Length();
					var query = PhysicsRayQueryParameters2D.Create(currentFrom, to);
					query.CollideWithAreas = true;
					query.CollideWithBodies = true;
					query.CollisionMask = 5;
					var excluded = new Godot.Collections.Array<Rid>();
					Vector2 endPoint = to;
					Vector2 hitNormal = Vector2.Zero;
					Enemy hitEnemy = null;
					DestructibleObstacle hitObstacle = null;
					int safety = 32;
					while (safety-- > 0) {
						query.Exclude = excluded;
						var result = spaceState.IntersectRay(query);
						if (result.Count == 0) break;
						var collider = result["collider"].As<Node>();
						if (collider is Pickup pickup) {
							excluded.Add(pickup.GetRid());
							continue;
						}
						endPoint = (Vector2)result["position"];
						hitNormal = (Vector2)result["normal"];
						if (collider is Enemy e) hitEnemy = e;
						else if (collider is DestructibleObstacle d) hitObstacle = d;
						break;
					}
					path.Add(endPoint);
					if (hitEnemy != null) hitEnemies.Add((hitEnemy, endPoint));
					if (hitObstacle != null) hitObstacles.Add((hitObstacle, endPoint));
					if (bouncesLeft <= 0 || hitNormal == Vector2.Zero || endPoint.DistanceSquaredTo(currentFrom) < 1f) break;
					currentDir = (currentDir - 2f * currentDir.Dot(hitNormal) * hitNormal).Normalized();
					currentFrom = endPoint + currentDir * 1f;
					bouncesLeft--;
				}
			}
			allPaths.Add(path);
		}
		// Wave upgrade: undulate the rendered beam. Hit detection above stays along
		// the straight ray, so the beam still damages along its aimed axis.
		if (Gun.Wave > 0f) {
			laserWaveTime += GetProcessDeltaTime();
			for (int i = 0; i < allPaths.Count; i++) allPaths[i] = MakeWavyPath(allPaths[i]);
		}
		int totalSegments = 0;
		foreach (var p in allPaths) totalSegments += p.Count - 1;
		if (Gun.BulletSpriteFrames != null) {
			if (activeLaser != null) { activeLaser.QueueFree(); activeLaser = null; }
			float texWidth = GetSpriteFrameWidth(Gun.BulletSpriteFrames);
			while (laserSegments.Count > totalSegments) {
				var last = laserSegments[laserSegments.Count - 1];
				if (IsInstanceValid(last)) last.QueueFree();
				laserSegments.RemoveAt(laserSegments.Count - 1);
			}
			while (laserSegments.Count < totalSegments) {
				var seg = new AnimatedSprite2D();
				seg.SpriteFrames = Gun.BulletSpriteFrames;
				var names = Gun.BulletSpriteFrames.GetAnimationNames();
				if (names.Length > 0) seg.Animation = names[0];
				seg.Play();
				seg.Offset = new Vector2(-texWidth / 2f, 0f);
				seg.ZIndex = 10;
				GetParent().AddChild(seg);
				laserSegments.Add(seg);
			}
			float thickness = BeamThickness(Gun);
			int segIdx = 0;
			foreach (var path in allPaths) {
				for (int i = 0; i < path.Count - 1; i++) {
					if (laserSegments[segIdx] is AnimatedSprite2D s) {
						Vector2 a = path[i];
						Vector2 b = path[i + 1];
						Vector2 d = b - a;
						s.Offset = new Vector2(-texWidth / 2f, 0f);
						s.GlobalPosition = a;
						s.Rotation = (a - b).Angle();
						s.Scale = new Vector2(d.Length() / texWidth, thickness);
					}
					segIdx++;
				}
			}
		} else {
			foreach (var s in laserSegments) if (IsInstanceValid(s)) s.QueueFree();
			laserSegments.Clear();
			if (activeLaser == null) {
				activeLaser = new Line2D();
				activeLaser.Width = 4f * BeamThickness(Gun);
				activeLaser.DefaultColor = new Color(1f, 0.3f, 0.3f);
				activeLaser.ZIndex = 10;
				GetParent().AddChild(activeLaser);
			}
			activeLaser.ClearPoints();
			foreach (var path in allPaths) {
				for (int i = 0; i < path.Count; i++) {
					activeLaser.AddPoint(path[i]);
				}
			}
		}
		if (gunCoolDown <= 0 && (hitEnemies.Count > 0 || hitObstacles.Count > 0)) {
			foreach (var (e, point) in hitEnemies) {
				ApplyGunHit(e, Gun.Damage, Gun);
				SpawnBeamImpact(point, hitEnemy: true);
			}
			foreach (var (o, point) in hitObstacles) {
				o.TakeDamage(Gun.Damage);
				SpawnBeamImpact(point, hitEnemy: false);
			}
			gunCoolDown = Gun.FireRate * StatusEffects.GetFireRateMultiplier();
		}
	}

	// Heat-seeking laser: marches the beam outward in short steps, steering each step
	// toward the nearest enemy so it curves to home in. Stops at the first thing hit;
	// records the hit enemy/obstacle. Frequency of the curve scales with Gun.HeatSeeking.
	private System.Collections.Generic.List<Vector2> BuildHeatSeekBeam(
		Vector2 from, Vector2 dir, PhysicsDirectSpaceState2D spaceState,
		List<(Enemy enemy, Vector2 point)> hitEnemies, List<(DestructibleObstacle obstacle, Vector2 point)> hitObstacles)
	{
		var path = new System.Collections.Generic.List<Vector2> { from };
		var noVisited = new HashSet<Enemy>();
		Vector2 pos = from;
		dir = dir.Normalized();
		float stepLen = 22f;
		float maxLen = ScreenSize.Length();
		float traveled = 0f;
		float turn = Mathf.Clamp(Gun.HeatSeeking * 0.05f, 0f, 0.5f);
		int safety = 256;
		while (traveled < maxLen && safety-- > 0) {
			Enemy target = FindNearestEnemy(pos, noVisited, maxLen);
			if (target != null) {
				Vector2 toTarget = (target.GlobalPosition - pos).Normalized();
				if (toTarget != Vector2.Zero) dir = dir.Slerp(toTarget, turn).Normalized();
			}
			Vector2 next = pos + dir * stepLen;
			var query = PhysicsRayQueryParameters2D.Create(pos, next);
			query.CollideWithAreas = true;
			query.CollideWithBodies = true;
			query.CollisionMask = 5;
			var excluded = new Godot.Collections.Array<Rid>();
			Vector2 endPoint = next;
			bool hit = false;
			int safety2 = 8;
			while (safety2-- > 0) {
				query.Exclude = excluded;
				var result = spaceState.IntersectRay(query);
				if (result.Count == 0) break;
				var collider = result["collider"].As<Node>();
				if (collider is Pickup pickup) { excluded.Add(pickup.GetRid()); continue; }
				endPoint = (Vector2)result["position"];
				if (collider is Enemy e) hitEnemies.Add((e, endPoint));
				else if (collider is DestructibleObstacle d) hitObstacles.Add((d, endPoint));
				hit = true;
				break;
			}
			if (hit) { path.Add(endPoint); break; }
			path.Add(next);
			pos = next;
			traveled += stepLen;
		}
		return path;
	}

	// Rebuilds a straight beam polyline as a sine wave: each point is displaced
	// perpendicular by amplitude that peaks mid-beam (so the muzzle and the hit point
	// stay anchored) and scrolls outward over time. Frequency scales with Gun.Wave.
	private System.Collections.Generic.List<Vector2> MakeWavyPath(System.Collections.Generic.List<Vector2> straight)
	{
		if (straight.Count < 2) return straight;
		float totalLen = 0f;
		for (int i = 0; i < straight.Count - 1; i++) totalLen += straight[i].DistanceTo(straight[i + 1]);
		if (totalLen < 1f) return straight;
		float amp = 24f * BeamThickness(Gun);
		float k = Gun.Wave * 0.03f;
		float phase = (float)(laserWaveTime * Gun.Wave * 3f);
		var result = new System.Collections.Generic.List<Vector2> { straight[0] };
		float distAccum = 0f;
		for (int i = 0; i < straight.Count - 1; i++) {
			Vector2 a = straight[i];
			Vector2 b = straight[i + 1];
			Vector2 seg = b - a;
			float segLen = seg.Length();
			if (segLen < 0.001f) { result.Add(b); continue; }
			Vector2 dir = seg / segLen;
			Vector2 perp = new Vector2(-dir.Y, dir.X);
			int steps = Mathf.Max(1, Mathf.RoundToInt(segLen / 14f));
			for (int s = 1; s <= steps; s++) {
				float along = segLen * ((float)s / steps);
				float d = distAccum + along;
				float envelope = Mathf.Sin(Mathf.Pi * d / totalLen);
				float disp = amp * envelope * Mathf.Sin(d * k - phase);
				result.Add(a + dir * along + perp * disp);
			}
			distAccum += segLen;
		}
		return result;
	}

	private void ClearLaser()
	{
		if (activeLaser != null) {
			activeLaser.QueueFree();
			activeLaser = null;
		}
		foreach (var s in laserSegments) if (IsInstanceValid(s)) s.QueueFree();
		laserSegments.Clear();
	}

	private void UpdateLaserSight(Vector2 aimDirection)
	{
		var spaceState = GetWorld2D().DirectSpaceState;
		Vector2 from = GlobalPosition;
		Vector2 to = from + aimDirection.Normalized() * ScreenSize.Length();
		var query = PhysicsRayQueryParameters2D.Create(from, to);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.CollisionMask = 5;
		var excluded = new Godot.Collections.Array<Rid>();
		Vector2 endPoint = to;
		int safety = 32;
		while (safety-- > 0) {
			query.Exclude = excluded;
			var result = spaceState.IntersectRay(query);
			if (result.Count == 0) break;
			var collider = result["collider"].As<Node>();
			if (collider is Pickup pickup) {
				excluded.Add(pickup.GetRid());
				continue;
			}
			endPoint = (Vector2)result["position"];
			break;
		}
		if (laserSightLine == null) {
			laserSightLine = new Line2D();
			laserSightLine.Width = 1.5f;
			laserSightLine.DefaultColor = new Color(1f, 0.2f, 0.2f, 0.7f);
			laserSightLine.ZIndex = 9;
			laserSightLine.AddPoint(from);
			laserSightLine.AddPoint(endPoint);
			GetParent().AddChild(laserSightLine);
		} else {
			laserSightLine.SetPointPosition(0, from);
			laserSightLine.SetPointPosition(1, endPoint);
		}
	}

	private void ClearLaserSight()
	{
		if (laserSightLine != null) {
			laserSightLine.QueueFree();
			laserSightLine = null;
		}
	}

	private void UpdateLightning(Vector2 aimDirection)
	{
		if (gunCoolDown > 0f) return;
		Enemy primary = FindLightningTarget(aimDirection.Normalized(), Mathf.DegToRad(Gun.LightningAimConeDeg), Gun.LightningRange);
		if (primary == null) return;
		var chain = new List<Enemy> { primary };
		var visited = new HashSet<Enemy> { primary };
		Enemy current = primary;
		for (int i = 0; i < Gun.LightningMaxJumps; i++) {
			Enemy next = FindNearestEnemy(current.GlobalPosition, visited, Gun.LightningChainRadius);
			if (next == null) break;
			chain.Add(next);
			visited.Add(next);
			current = next;
		}
		var points = new List<Vector2> { GlobalPosition };
		foreach (var e in chain) points.Add(e.GlobalPosition);
		ClearLightning();
		if (Gun.BulletSpriteFrames != null) {
			BuildAnimatedBeamSegments(points, Gun.BulletSpriteFrames, Gun.BeamSegmentBaseLength, lightningSegments, BeamThickness(Gun));
		} else {
			activeLightning = new Line2D();
			activeLightning.Width = 3f * BeamThickness(Gun);
			activeLightning.DefaultColor = new Color(0.6f, 0.8f, 1f, 0.95f);
			activeLightning.ZIndex = 10;
			foreach (var p in points) activeLightning.AddPoint(p);
			GetParent().AddChild(activeLightning);
		}
		int dmg = Gun.Damage;
		foreach (var e in chain) {
			if (dmg <= 0) continue;
			ApplyGunHit(e, dmg, Gun);
			dmg = dmg / 2;
		}
		lightningFlashTimer = 0.12f;
		gunCoolDown = Gun.FireRate * StatusEffects.GetFireRateMultiplier();
	}

	private void BuildAnimatedBeamSegments(List<Vector2> points, SpriteFrames frames, float baseLength, List<Node2D> track, float thickness)
	{
		var animNames = frames.GetAnimationNames();
		string animName = animNames.Length > 0 ? animNames[0] : "default";
		float texWidth = GetSpriteFrameWidth(frames);
		for (int i = 0; i < points.Count - 1; i++) {
			var seg = new AnimatedSprite2D();
			seg.SpriteFrames = frames;
			if (frames.HasAnimation(animName)) seg.Animation = animName;
			seg.Play();
			seg.Offset = new Vector2(-texWidth / 2f, 0f);
			float len = points[i].DistanceTo(points[i + 1]);
			seg.GlobalPosition = points[i];
			seg.Rotation = (points[i] - points[i + 1]).Angle();
			seg.Scale = new Vector2(len / texWidth, thickness);
			seg.ZIndex = 10;
			GetParent().AddChild(seg);
			track.Add(seg);
		}
	}

	private static float GetSpriteFrameWidth(SpriteFrames frames)
	{
		if (frames == null) return 64f;
		var names = frames.GetAnimationNames();
		if (names.Length == 0) return 64f;
		var animName = names[0];
		if (frames.GetFrameCount(animName) == 0) return 64f;
		var tex = frames.GetFrameTexture(animName, 0);
		return tex != null && tex.GetWidth() > 0 ? tex.GetWidth() : 64f;
	}

	private void ClearLightning()
	{
		if (activeLightning != null) {
			activeLightning.QueueFree();
			activeLightning = null;
		}
		foreach (var s in lightningSegments) if (IsInstanceValid(s)) s.QueueFree();
		lightningSegments.Clear();
	}

	private Enemy FindLightningTarget(Vector2 aimDir, float maxAngleRad, float maxDist)
	{
		Enemy best = null;
		float bestScore = float.MaxValue;
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e.CurrentHealth > 0) {
				Vector2 toE = e.GlobalPosition - GlobalPosition;
				float d = toE.Length();
				if (d > maxDist || d < 1f) continue;
				float ang = Mathf.Abs(toE.AngleTo(aimDir));
				if (ang > maxAngleRad) continue;
				float score = d + ang * 300f;
				if (score < bestScore) { bestScore = score; best = e; }
			}
		}
		return best;
	}

	private Enemy FindNearestEnemy(Vector2 from, HashSet<Enemy> visited, float maxDist)
	{
		Enemy best = null;
		float bestDist = float.MaxValue;
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e.CurrentHealth > 0 && !visited.Contains(e)) {
				float d = e.GlobalPosition.DistanceTo(from);
				if (d > maxDist) continue;
				if (d < bestDist) { bestDist = d; best = e; }
			}
		}
		return best;
	}

	private void MeleeAttack(Vector2 aimDirection) {
		if (Melee?.Attack == null) return;
		PlayActionAnimation("Swording");
		float swingDuration = Melee.SwingDuration / Mathf.Max(0.1f, MeleeSwingSpeedMultiplier);
		meleeCoolDown = Melee.SwingCooldown * MeleeCooldownMultiplier / Mathf.Max(0.1f, MeleeSwingSpeedMultiplier);
		MeleeAttack a = Melee.Attack.Instantiate<MeleeAttack>();
		a.Direction = aimDirection.Normalized();
		a.Damage = Melee.Damage + MeleeDamageBonus;
		a.SwingDuration = swingDuration;
		a.SwingArc = Melee.SwingArc + MeleeArcBonus;
		a.OffsetDistance = Melee.OffsetDistance + MeleeRangeBonus;
		a.LifeSteal = MeleeLifeSteal;
		a.SetCollisionLayerValue(4, true);
		a.SetCollisionMaskValue(5, true);
		AddChild(a);
	}

	private void _on_animated_sprite_2d_animation_finished() {
		switch (animatedSprite2D.Animation) {
			case "Explode":
			ShowGameOver();
			break;
			case "Damage":
			playingDamageAnim = false;
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
			break;
			case "Fire":
			// Only drop back to idle once the player has released fire; while it's held
			// the next shot replays "Fire", so returning to idle here would flicker.
			if (Input.IsActionPressed("gun")) break;
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
			break;
			case "Shooting":
			case "Swording":
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
			break;
		}
	}
}
