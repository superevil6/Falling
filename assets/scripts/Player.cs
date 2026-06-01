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
	public StatusEffectController StatusEffects = new StatusEffectController();
	private float wallTouchTickTimer = 0f;
	private float gunCoolDown;
	[Export]
	public int Speed { get;set;} = 400;
	[Export]
	public Gun[] Guns {get;set;} = new Gun[4];
	[Export]
	public BodyMod[] BodyMods {get;set;} = new BodyMod[4];
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
	public float ShortDashDistance {get;set;} = 80f;
	private bool isShortDash = false;
	private float RemainingDashTime;
	private Vector2 DashDirection;
	private float shortDashCooldownRemaining = 0f;
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
	private float chargeAmount = 0f;
	private float chargePulseTime = 0f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		CurrentHealth = MaxHealth;
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		rng.Randomize();
		if (Guns != null) {
			for (int i = 0; i < Guns.Length; i++) {
				if (Guns[i] != null) {
					string srcName = System.IO.Path.GetFileNameWithoutExtension(Guns[i].ResourcePath);
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
		GetParent().GetNode<TextureProgressBar>("Health Bar").MaxValue = MaxHealth;
		GetParent().GetNode<TextureProgressBar>("Health Bar").Value = CurrentHealth;
		GetParent().GetNode<Label>("Experience Counter").Text = $"XP: {CurrentExperience}";
		UpdateGunLabel();
		afterImageShader = GD.Load<Shader>("res://assets/shaders/AfterImage.gdshader");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
		if (Gun != null && Gun.IsChargeWeapon) {
			ClearLaser();
			UpdateChargeState(aimDirection, (float)delta);
		} else if (Gun != null && Gun.IsLaser && wantsToFire) {
			UpdateLaserBeam(aimDirection);
		} else {
			ClearLaser();
			if (wantsToFire && gunCoolDown <= 0) {
				Shoot(aimDirection);
			}
		}
		if (gunCoolDown > 0) {
			gunCoolDown -= (float)delta;
		}
		#endregion
		#region Sword
		if (Input.IsActionJustPressed("sword") && aimDirection.Normalized() != Vector2.Zero) {
			MeleeAttack(aimDirection);
		}
		#endregion
		if (Input.IsActionJustPressed("bomb")) {
			DropBomb();
		}
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);
		int dotDamage = StatusEffects.Tick((float)delta);
		if (dotDamage > 0) TakeDamage(dotDamage);
		TimeSinceLastAfterImage += (float)delta;
		bool shouldTrail = (Input.IsActionPressed("move_down") || RemainingDashTime > 0) && !IsTouchingWall;
		if (shouldTrail && TimeSinceLastAfterImage >= AfterImageInterval) {
			SpawnAfterImage();
			TimeSinceLastAfterImage = 0f;
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
		}
	}

	public void UpdateGunLabel()
	{
		var gunImage = GetParent().GetNodeOrNull<TextureRect>("Gun Image");
		if (Gun == null) {
			GetParent().GetNode<Label>("Gun Label").Text = "Gun: -";
			if (gunImage != null) gunImage.Texture = null;
			return;
		}
		string gunName = !string.IsNullOrEmpty(Gun.SourceName)
			? Gun.SourceName
			: (!string.IsNullOrEmpty(Gun.ResourcePath)
				? System.IO.Path.GetFileNameWithoutExtension(Gun.ResourcePath)
				: "-");
		GetParent().GetNode<Label>("Gun Label").Text =
			$"Gun: {gunName}  Lv{Gun.CurrentLevel}  XP {Gun.CurrentExperience}/{Gun.ExperiencePerLevel}  SP {Gun.SkillPoints}";
		if (gunImage != null) gunImage.Texture = Gun.GunImage;
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
		GD.Print(node2D);
		if (node2D.Name == "Bullet") {
			TakeDamage((node2D as Attack).Damage);
		}
	}

	public void TakeDamage(int amount)
	{
		if (CurrentHealth <= 0) return;
		int finalDamage = Mathf.Max(0, amount - DamageReduction);
		CurrentHealth -= finalDamage;
		GetParent().GetNode<TextureProgressBar>("Health Bar").Value = CurrentHealth;
		FlashRed();
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
		if (shortDashCooldownRemaining > 0) shortDashCooldownRemaining -= (float)delta;
		if (Input.IsActionJustPressed("Dash") && !IsTouchingWall
			&& shortDashCooldownRemaining <= 0 && RemainingDashTime <= 0) {
			Vector2 dashDir = Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized();
			if (dashDir != Vector2.Zero) {
				RemainingDashTime = ShortDashDuration;
				DashDirection = dashDir;
				shortDashCooldownRemaining = ShortDashCooldown;
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
		animatedSprite2D.Animation = "Shooting";
		animatedSprite2D.Play();
		float ratio = Gun.ChargeTime > 0f ? Mathf.Clamp(chargeAmount / Gun.ChargeTime, 0f, 1f) : 1f;
		int baseDamage = Mathf.RoundToInt(Mathf.Lerp(Gun.MinDamage, Gun.MaxDamage, ratio));
		float sizeMult = Mathf.Lerp(Gun.MinSize, Gun.MaxSize, ratio);
		int dirCount = Mathf.Max(1, Gun.DirectionalCount);
		float dirStep = Mathf.DegToRad(Gun.DirectionalAngle);
		for (int d = 0; d < dirCount; d++) {
			Vector2 dir = aimDirection.Rotated(dirStep * d).Normalized();
			bool crit = RollCrit(Gun, out int damage, baseDamage);
			Bullet b = Gun.BulletType.Instantiate<Bullet>();
			b.Set("Direction", dir);
			b.Set("Damage", damage);
			b.Set("BulletLifetime", Gun.BulletLifetime);
			b.Gun = Gun;
			if (Gun.BulletSpeed > 0) b.BulletSpeed = Gun.BulletSpeed;
			if (Gun.BulletSpriteFrames != null) {
				var bSprite = b.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
				if (bSprite != null) bSprite.SpriteFrames = Gun.BulletSpriteFrames;
			}
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

	private bool RollCrit(Gun gun, out int damage, int baseDamage) {
		bool crit = gun.CriticalChance > 0f && rng.Randf() < gun.CriticalChance;
		damage = crit ? Mathf.RoundToInt(baseDamage * gun.CriticalMultiplier) : baseDamage;
		return crit;
	}

	private void Shoot(Vector2 aimDirection) {
		animatedSprite2D.Animation = "Shooting";
		int dirCount = Mathf.Max(1, Gun.DirectionalCount);
		float dirStep = Mathf.DegToRad(Gun.DirectionalAngle);
		for (int d = 0; d < dirCount; d++) {
			SpawnBulletSpread(aimDirection.Rotated(dirStep * d));
		}
		animatedSprite2D.Play();
		gunCoolDown = Gun.FireRate * StatusEffects.GetFireRateMultiplier();
	}

	private void SpawnBulletSpread(Vector2 aimDirection) {
		Vector2 perp = aimDirection.Normalized().Rotated(Mathf.Pi / 2f);
		float center = (Gun.BulletCount - 1) / 2f;
		float effectiveSpread = Gun.BulletSpread + StatusEffects.GetSpreadIncrease();
		float perBulletSeparation = effectiveSpread * 60f;
		float halfSpread = effectiveSpread / 2f;
		for (int i = 0; i < Gun.BulletCount; i++) {
			Bullet b = Gun.BulletType.Instantiate<Bullet>();
			float angle = Gun.BulletCount > 1 || effectiveSpread > 0f
				? rng.RandfRange(-halfSpread, halfSpread)
				: 0f;
			Vector2 direction = aimDirection.Rotated(angle);
			bool crit = RollCrit(Gun, out int dmg, Gun.Damage);
			b.Set("Direction", direction);
			b.Set("Damage", dmg);
			b.Set("BulletLifetime", Gun.BulletLifetime);
			b.Gun = Gun;
			if (Gun.BulletSpeed > 0) b.BulletSpeed = Gun.BulletSpeed;
			if (Gun.BulletSpriteFrames != null) {
				var bSprite = b.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
				if (bSprite != null) bSprite.SpriteFrames = Gun.BulletSpriteFrames;
			}
			if (Gun.Element != ElementType.NonElemental) b.Element = Gun.Element;
			b.AuraColor = crit ? CritAuraColor : new Color(0.3f, 0.6f, 1f, 0.8f);
			b.Position = Position + perp * (i - center) * perBulletSeparation;
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
		Vector2 to = from + aimDirection.Normalized() * ScreenSize.Length();
		var query = PhysicsRayQueryParameters2D.Create(from, to);
		query.CollideWithAreas = true;
		query.CollideWithBodies = true;
		query.CollisionMask = 5;
		var excluded = new Godot.Collections.Array<Rid>();
		Vector2 endPoint = to;
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
			if (collider is Enemy e) hitEnemy = e;
			else if (collider is DestructibleObstacle d) hitObstacle = d;
			break;
		}
		if (activeLaser == null) {
			activeLaser = new Line2D();
			activeLaser.Width = 4f;
			activeLaser.DefaultColor = new Color(1f, 0.3f, 0.3f);
			activeLaser.ZIndex = 10;
			activeLaser.AddPoint(from);
			activeLaser.AddPoint(endPoint);
			GetParent().AddChild(activeLaser);
		} else {
			activeLaser.SetPointPosition(0, from);
			activeLaser.SetPointPosition(1, endPoint);
		}
		if (gunCoolDown <= 0 && (hitEnemy != null || hitObstacle != null)) {
			if (hitEnemy != null) hitEnemy.TakeDamage(Gun.Damage, ElementType.NonElemental);
			if (hitObstacle != null) hitObstacle.TakeDamage(Gun.Damage);
			gunCoolDown = Gun.FireRate * StatusEffects.GetFireRateMultiplier();
		}
	}

	private void ClearLaser()
	{
		if (activeLaser != null) {
			activeLaser.QueueFree();
			activeLaser = null;
		}
	}

	private void MeleeAttack(Vector2 aimDirection) {
		if (Melee?.Attack == null) return;
		animatedSprite2D.Animation = "Swording";
		animatedSprite2D.Play();
		MeleeAttack a = Melee.Attack.Instantiate<MeleeAttack>();
		a.Direction = aimDirection.Normalized();
		a.Damage = Melee.Damage;
		a.SwingDuration = Melee.SwingDuration;
		a.SwingArc = Melee.SwingArc;
		a.OffsetDistance = Melee.OffsetDistance;
		a.SetCollisionLayerValue(4, true);
		a.SetCollisionMaskValue(5, true);
		AddChild(a);
	}

	private void _on_animated_sprite_2d_animation_finished() {
		switch (animatedSprite2D.Animation) {
			case "Shooting":
			case "Swording":
			animatedSprite2D.Animation = "Falling";
			animatedSprite2D.Play();
			break;
		}
	}
}
