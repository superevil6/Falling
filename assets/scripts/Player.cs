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
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		CurrentHealth = MaxHealth;
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		if (Guns != null) {
			for (int i = 0; i < Guns.Length; i++) {
				if (Guns[i] != null) Guns[i] = (Gun)Guns[i].Duplicate();
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
		afterImageShader = GD.Load<Shader>("res://assets/objects/AfterImage.gdshader");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		bool gunSwitched = false;
		if (Input.IsActionJustPressed("gun_1")) { currentGunIndex = 0; gunSwitched = true; }
		else if (Input.IsActionJustPressed("gun_2")) { currentGunIndex = 1; gunSwitched = true; }
		else if (Input.IsActionJustPressed("gun_3")) { currentGunIndex = 2; gunSwitched = true; }
		else if (Input.IsActionJustPressed("gun_4")) { currentGunIndex = 3; gunSwitched = true; }
		if (gunSwitched) UpdateGunLabel();
		#region Shooting
		Vector2 aimDirection = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
		bool wantsToFire = Input.IsActionPressed("gun") && aimDirection != Vector2.Zero && Gun != null;
		if (Gun != null && Gun.IsLaser && wantsToFire) {
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
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);
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
		}
	}

	public void UpdateGunLabel()
	{
		if (Gun == null) {
			GetParent().GetNode<Label>("Gun Label").Text = "Gun: -";
			return;
		}
		string gunName = !string.IsNullOrEmpty(Gun.ResourcePath)
			? System.IO.Path.GetFileNameWithoutExtension(Gun.ResourcePath)
			: "-";
		GetParent().GetNode<Label>("Gun Label").Text =
			$"Gun: {gunName}  Lv{Gun.CurrentLevel}  XP {Gun.CurrentExperience}/{Gun.ExperiencePerLevel}  SP {Gun.SkillPoints}";
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
		float effectiveSpeed = Speed * CurrentSpeedMultiplier;
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
			CurrentHealth -= (node2D as Attack).Damage;
			GetParent().GetNode<TextureProgressBar>("Health Bar").Value = CurrentHealth;
			FlashRed();
		}
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
				}
			}

		}
		if (RemainingDashTime > 0) {
			Velocity = DashDirection.Normalized() * (Speed * 400) * (float)delta;
			MoveAndSlide();
			RemainingDashTime -= (float)delta;
		}
	}


	private void Shoot(Vector2 aimDirection) {
		animatedSprite2D.Animation = "Shooting";
		float angleStep = Gun.BulletCount > 1 ? Gun.BulletSpread / (Gun.BulletCount - 1) : 0f;
		float startAngle = -Gun.BulletSpread / 2f;
		Vector2 perp = aimDirection.Normalized().Rotated(Mathf.Pi / 2f);
		float center = (Gun.BulletCount - 1) / 2f;
		float perBulletSeparation = Gun.BulletSpread * 60f;
		for (int i = 0; i < Gun.BulletCount; i++) {
			Bullet b = Gun.BulletType.Instantiate<Bullet>();
			Vector2 direction = aimDirection.Rotated(startAngle + i * angleStep);
			b.Set("Direction", direction);
			b.Set("Damage", Gun.Damage);
			b.Set("BulletLifetime", Gun.BulletLifetime);
			b.Gun = Gun;
			if (Gun.Element != ElementType.NonElemental) b.Element = Gun.Element;
			b.Position = Position + perp * (i - center) * perBulletSeparation;
			b.Rotation = direction.Angle();
			b.SetCollisionLayerValue(4, true);
			b.SetCollisionMaskValue(3, true);
			b.SetCollisionMaskValue(1, true);
			GetParent().AddChild(b);
		}
		animatedSprite2D.Play();
		gunCoolDown = Gun.FireRate;
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
		if (hitEnemy != null && gunCoolDown <= 0) {
			hitEnemy.TakeDamage(Gun.Damage, ElementType.NonElemental);
			gunCoolDown = Gun.FireRate;
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
		animatedSprite2D.Animation = "Swording";
		animatedSprite2D.Play();
		MeleeAttack a = Melee.Attack.Instantiate<MeleeAttack>();
		a.Set("Direction", aimDirection.Normalized());
		a.Set("Damage", Melee.Damage);
		a.Set("SwingDuration", Melee.SwingDuration);
		a.Position = new Vector2(aimDirection.X * 150, aimDirection.Y * 150);
		a.Rotation = aimDirection.Angle();
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
