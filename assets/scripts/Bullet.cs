using Godot;

public partial class Bullet : Attack
{
	[Export]
	public float BulletSpeed {get;set;}= 100;
	public float BulletLifetime {get;set;}
	public Vector2 Direction {get;set;}
	public Gun Gun {get;set;}
	[Export]
	public PackedScene Explosion {get;set;}
	[Export]
	public ElementType Element { get; set; }
	public Color AuraColor {get;set;} = new Color(0, 0, 0, 0);
	public float AuraRadius {get;set;} = 3f;
	private System.Collections.Generic.HashSet<ulong> auraDamagedIds = new System.Collections.Generic.HashSet<ulong>();
	public Vector2 SpawnOrigin {get;set;}
	public float SpiralRate {get;set;} = 0f;
	private double spiralTime = 0;
	private float spiralInitialAngle = 0f;
	private float spiralInitialRadius = 0f;
	private Vector2 growthBaseScale;
	private float growthInitialDamage;
	private float growthTraveled = 0f;
	private Vector2 growthLastPos;
	private int ExplosionDamage;
	private int RemainingRicochets;

	//for wave movement
	private double time;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		string shaderPath = Element switch {
			ElementType.Fire => "res://assets/shaders/Fire.gdshader",
			ElementType.Ice => "res://assets/shaders/Ice.gdshader",
			ElementType.Electric => "res://assets/shaders/Lightning.gdshader",
			ElementType.Poison => "res://assets/shaders/Poison.gdshader",
			_ => null
		};
		if (shaderPath != null) {
			var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			if (sprite != null) {
				var mat = new ShaderMaterial();
				mat.Shader = GD.Load<Shader>(shaderPath);
				sprite.Material = mat;
			}
		}
		if (Gun == null) return;
		if (Gun.BulletSpriteFrames != null) {
			var bulletSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			if (bulletSprite != null) bulletSprite.SpriteFrames = Gun.BulletSpriteFrames;
		}
		if (Gun.SizeMultiplier > 0) {
			Scale = new Vector2(Scale.X + Gun.SizeMultiplier, Scale.Y + Gun.SizeMultiplier);
		}
		if (Gun.BulletSize > 0) {
			Scale = new Vector2(Scale.X * Gun.BulletSize, Scale.Y * Gun.BulletSize);
		}
		RemainingRicochets = Gun.Ricochet;
		if (SpiralRate != 0f) {
			Vector2 offset = GlobalPosition - SpawnOrigin;
			spiralInitialAngle = offset.Angle();
			spiralInitialRadius = offset.Length();
		}
		if (Gun.Growth) {
			growthBaseScale = Scale;
			growthInitialDamage = Damage;
			growthLastPos = GlobalPosition;
			Scale = growthBaseScale * Gun.GrowthStartSize;
		}
	}

	public override void _Draw()
	{
		if (AuraColor.A <= 0f) return;
		var outer = new Color(AuraColor.R, AuraColor.G, AuraColor.B, AuraColor.A * 0.15f);
		var mid = new Color(AuraColor.R, AuraColor.G, AuraColor.B, AuraColor.A * 0.30f);
		var inner = new Color(AuraColor.R, AuraColor.G, AuraColor.B, AuraColor.A * 0.45f);
		DrawCircle(Vector2.Zero, AuraRadius * 1.6f, outer);
		DrawCircle(Vector2.Zero, AuraRadius * 1.1f, mid);
		DrawCircle(Vector2.Zero, AuraRadius * 0.6f, inner);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (SpiralRate != 0f) {
			spiralTime += delta;
			float t = (float)spiralTime;
			float ang = spiralInitialAngle + SpiralRate * t;
			float r = spiralInitialRadius + BulletSpeed * t;
			GlobalPosition = SpawnOrigin + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
			Vector2 motion = new Vector2(
				BulletSpeed * Mathf.Cos(ang) - r * SpiralRate * Mathf.Sin(ang),
				BulletSpeed * Mathf.Sin(ang) + r * SpiralRate * Mathf.Cos(ang));
			Rotation = motion.Angle();
			if (BulletLifetime > 0) BulletLifetime -= (float)delta;
			else QueueFree();
			return;
		}
		if (Gun != null) {
			if (Gun.Wave > 0) {
				time += delta;
				const float waveAmp = 40f;
				float omega = Gun.Wave;
				Vector2 perp = Direction.Normalized().Rotated(Mathf.Pi / 2f);
				float lateralVel = waveAmp * omega * Mathf.Cos((float)(time * omega));
				Position += perp * lateralVel * (float)delta;
			}
			if (Gun.HeatSeeking > 0) {
				var targetDir = ClosestEnemyDirection();
				if (targetDir.HasValue) {
					float weight = Mathf.Clamp(Gun.HeatSeeking * (float)delta, 0, 1);
					Direction = Direction.Normalized().Slerp(targetDir.Value, weight);
				}
			}
		}
		GlobalPosition = GlobalPosition + BulletSpeed * Direction.Normalized() * (float)delta;
		if (Gun != null && Gun.Growth) {
			growthTraveled += GlobalPosition.DistanceTo(growthLastPos);
			growthLastPos = GlobalPosition;
			float t = Gun.GrowthDistance > 0f ? Mathf.Clamp(growthTraveled / Gun.GrowthDistance, 0f, 1f) : 1f;
			float sizeMult = Mathf.Lerp(Gun.GrowthStartSize, Gun.GrowthMaxSize, t);
			Scale = growthBaseScale * sizeMult;
			float dmgFactor = Mathf.Lerp(1f, Gun.GrowthMinDamageRatio, t);
			Damage = Mathf.RoundToInt(growthInitialDamage * dmgFactor);
		}
		if (Gun != null && Gun.AuraDamages && AuraRadius > 0f && AuraColor.A > 0f) {
			float worldRadius = AuraRadius * 1.6f * Scale.X;
			int auraDmg = Mathf.CeilToInt(Damage / 10f);
			if (auraDmg > 0) {
				foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
					if (n is Enemy e && e.CurrentHealth > 0) {
						ulong id = e.GetInstanceId();
						if (auraDamagedIds.Contains(id)) continue;
						if (GlobalPosition.DistanceTo(e.GlobalPosition) <= worldRadius) {
							e.TakeDamage(auraDmg, Element);
							auraDamagedIds.Add(id);
						}
					}
				}
			}
		}
		if (BulletLifetime > 0) {
			BulletLifetime -= (float)delta;
		} else {
			QueueFree();
		}
	}

	private void _on_area_entered(Node2D node) {
		if (node is Pickup) return;
		if (Gun != null && node is Enemy e) {
			if (Gun.DotStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.DamageOverTime, Gun.DotStacksPerHit);
			if (Gun.SlowStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.Slow, Gun.SlowStacksPerHit);
			if (Gun.FireRateStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.ReducedFireRate, Gun.FireRateStacksPerHit);
			if (Gun.BlindStacksPerHit > 0) e.StatusEffects.AddStacks(StatusEffectType.Blind, Gun.BlindStacksPerHit);
			switch (Element) {
				case ElementType.Fire: e.StatusEffects.AddStack(StatusEffectType.DamageOverTime); break;
				case ElementType.Ice: e.StatusEffects.AddStack(StatusEffectType.Slow); break;
				case ElementType.Electric: e.StatusEffects.AddStack(StatusEffectType.ReducedFireRate); break;
				case ElementType.Poison: e.StatusEffects.AddStack(StatusEffectType.DamageOverTime); break;
			}
		}
		HandleHit(true);
	}
	private void _on_body_entered(Node2D node) => HandleHit(false);

	private void HandleHit(bool hitEnemy) {
		if (Gun == null) {
			QueueFree();
			return;
		}
		if (Gun.Explode) {
			CallDeferred("GenerateExplosion");
		}
		if (hitEnemy && Gun.Split > 0) {
			SpawnSplitBullets();
			if (Gun.Pierce) return;
			QueueFree();
			return;
		}
		if (Gun.Pierce) return;
		if (RemainingRicochets > 0) {
			RemainingRicochets--;
			Direction = -Direction;
			Rotation = Direction.Angle();
			return;
		}
		QueueFree();
	}

	private void SpawnSplitBullets()
	{
		if (Gun?.BulletType == null) return;
		var parent = GetParent();
		if (parent == null) return;
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		int count = Gun.Split + 1;
		for (int i = 0; i < count; i++) {
			var b = Gun.BulletType.Instantiate<Bullet>();
			float angle = rng.RandfRange(0, Mathf.Pi * 2f);
			b.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			b.Damage = Damage;
			b.BulletLifetime = Gun.BulletLifetime > 0 ? Gun.BulletLifetime : 1f;
			b.Element = Element;
			b.Position = Position;
			b.Rotation = b.Direction.Angle();
			b.CollisionLayer = CollisionLayer;
			b.CollisionMask = CollisionMask;
			parent.CallDeferred("add_child", b);
		}
	}

	private Vector2? ClosestEnemyDirection() {
		Enemy closest = null;
		float closestDistSq = float.MaxValue;
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e.CurrentHealth > 0) {
				float d = GlobalPosition.DistanceSquaredTo(e.GlobalPosition);
				if (d < closestDistSq) {
					closestDistSq = d;
					closest = e;
				}
			}
		}
		if (closest == null) return null;
		return (closest.GlobalPosition - GlobalPosition).Normalized();
	}

	private void GenerateExplosion() {
		Explosion e = Explosion.Instantiate<Explosion>();
		e.Position = Position;
		e.Damage = Damage;
		if (Gun != null && Gun.ExplosionRadius > 0f) {
			float s = 1f + Gun.ExplosionRadius;
			e.Scale = new Vector2(s, s);
		}
		GetParent().AddChild(e);
	}
}
