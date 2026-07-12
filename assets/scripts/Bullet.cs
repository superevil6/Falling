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
	public PackedScene GasCloud {get;set;}
	[Export]
	public ElementType Element { get; set; }
	public Color AuraColor {get;set;} = new Color(0, 0, 0, 0);
	public float AuraRadius {get;set;} = 3f;
	// The outermost aura ring (and the aura's damage reach) is AuraRadius * this factor.
	private const float AuraOuterFactor = 1.6f;
	private System.Collections.Generic.HashSet<ulong> auraDamagedIds = new System.Collections.Generic.HashSet<ulong>();
	public Vector2 SpawnOrigin {get;set;}
	public float SpiralRate {get;set;} = 0f;
	public bool IsBoomerang {get;set;} = false;
	public Node2D BoomerangOrigin {get;set;}
	public float BoomerangMaxRadius {get;set;} = 200f;
	public float BoomerangDuration {get;set;} = 1.0f;
	public float BoomerangOutwardTime {get;set;} = 0.3f;
	public int BoomerangArcDirection {get;set;} = 1;
	private float boomerangTime = 0f;
	private float boomerangBaseAngle = 0f;
	private double spiralTime = 0;
	private float spiralInitialAngle = 0f;
	private float spiralInitialRadius = 0f;
	private Vector2 growthBaseScale;
	private float growthInitialDamage;
	private float growthTraveled = 0f;
	private Vector2 growthLastPos;
	private int ExplosionDamage;
	private int RemainingRicochets;
	private ulong spawnPhysicsFrame;
	// Bodies (walls) the bullet was already overlapping when it spawned — e.g. the player
	// firing while a contracting wall overlaps them. Collisions with these are ignored
	// until the bullet exits them, so it can't vanish/detonate on the wall it was born in.
	private readonly System.Collections.Generic.HashSet<ulong> spawnOverlapBodies = new System.Collections.Generic.HashSet<ulong>();
	private double subBulletTime;
	private float subBulletSpinAccum;

	//for wave movement
	private double time;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		spawnPhysicsFrame = Engine.GetPhysicsFrames();
		RecordSpawnOverlaps();
		string shaderPath = Element switch {
			ElementType.Corrosive => "res://assets/shaders/Fire.gdshader",
			ElementType.Ice => "res://assets/shaders/Ice.gdshader",
			ElementType.Electric => "res://assets/shaders/Lightning.gdshader",
			ElementType.Grease => "res://assets/shaders/Grease.gdshader",
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
		// Start the sprite's default animation. Bullets without a Gun (sub-bullets,
		// split bullets) return below, so play here too, not only in the Gun path.
		PlayDefaultAnimation();
		if (Gun == null) return;
		if (Gun.BulletSpriteFrames != null) {
			var bulletSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			if (bulletSprite != null) {
				bulletSprite.SpriteFrames = Gun.BulletSpriteFrames;
				PlayDefaultAnimation();
			}
		}
		if (Gun.SizeMultiplier > 0) {
			Scale = new Vector2(Scale.X + Gun.SizeMultiplier, Scale.Y + Gun.SizeMultiplier);
		}
		if (Gun.BulletSize > 0) {
			Scale = new Vector2(Scale.X * Gun.BulletSize, Scale.Y * Gun.BulletSize);
		}
		if (Gun.AuraDamages) {
			// Size the aura so its outer edge (AuraRadius * AuraOuterFactor) spans twice the
			// bullet's radius. Scale.X is uniform for both, so this ratio is scale-independent.
			AuraRadius = MeasureBulletRadius() * 2f / AuraOuterFactor;
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
		if (IsBoomerang) {
			boomerangBaseAngle = Direction.Angle();
		}
	}

	public override void _Draw()
	{
		if (AuraColor.A <= 0f) return;
		var outer = new Color(AuraColor.R, AuraColor.G, AuraColor.B, AuraColor.A * 0.15f);
		var mid = new Color(AuraColor.R, AuraColor.G, AuraColor.B, AuraColor.A * 0.30f);
		var inner = new Color(AuraColor.R, AuraColor.G, AuraColor.B, AuraColor.A * 0.45f);
		DrawCircle(Vector2.Zero, AuraRadius * AuraOuterFactor, outer);
		DrawCircle(Vector2.Zero, AuraRadius * 1.1f, mid);
		DrawCircle(Vector2.Zero, AuraRadius * 0.6f, inner);
	}

	// Plays the sprite's default animation ("default" if present, otherwise the first
	// animation defined in its SpriteFrames). No-op if there's no sprite or no animations.
	private void PlayDefaultAnimation()
	{
		var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite?.SpriteFrames == null) return;
		StringName anim = "default";
		if (!sprite.SpriteFrames.HasAnimation(anim)) {
			var names = sprite.SpriteFrames.GetAnimationNames();
			if (names.Length == 0) return;
			anim = names[0];
		}
		sprite.Play(anim);
	}

	// The bullet sprite's radius in this node's local space (texture half-size scaled by
	// the sprite child's own scale). Falls back to the current AuraRadius if unmeasurable.
	private float MeasureBulletRadius()
	{
		var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite?.SpriteFrames == null) return AuraRadius;
		StringName anim = sprite.Animation;
		if (anim.IsEmpty || !sprite.SpriteFrames.HasAnimation(anim)) {
			var names = sprite.SpriteFrames.GetAnimationNames();
			if (names.Length == 0) return AuraRadius;
			anim = names[0];
		}
		if (sprite.SpriteFrames.GetFrameCount(anim) == 0) return AuraRadius;
		var tex = sprite.SpriteFrames.GetFrameTexture(anim, 0);
		if (tex == null) return AuraRadius;
		Vector2 sz = tex.GetSize() * sprite.Scale.Abs();
		return Mathf.Max(sz.X, sz.Y) * 0.5f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsBoomerang) {
			boomerangTime += (float)delta;
			if (boomerangTime >= BoomerangDuration) {
				QueueFree();
				return;
			}
			Vector2 originPos = (BoomerangOrigin != null && IsInstanceValid(BoomerangOrigin))
				? BoomerangOrigin.GlobalPosition
				: SpawnOrigin;
			Vector2 forward = new Vector2(Mathf.Cos(boomerangBaseAngle), Mathf.Sin(boomerangBaseAngle));
			Vector2 side = new Vector2(-forward.Y, forward.X) * BoomerangArcDirection;
			float t = boomerangTime / Mathf.Max(0.0001f, BoomerangDuration);
			float theta = Mathf.Pi * t;
			float sinTheta = Mathf.Sin(theta);
			float forwardOffset = BoomerangMaxRadius * sinTheta * sinTheta;
			float sideOffset = BoomerangMaxRadius * 0.5f * Mathf.Sin(2f * theta);
			GlobalPosition = originPos + forward * forwardOffset + side * sideOffset;
			float fwdVel = BoomerangMaxRadius * Mathf.Sin(2f * theta);
			float sideVel = BoomerangMaxRadius * Mathf.Cos(2f * theta);
			Vector2 vel = forward * fwdVel + side * sideVel;
			if (vel != Vector2.Zero) Rotation = vel.Angle();
			return;
		}
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
			float worldRadius = AuraRadius * AuraOuterFactor * Scale.X;
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
		if (Gun != null && Gun.BulletShootsBullets) {
			subBulletTime += delta;
			if (subBulletTime >= Mathf.Max(0.02f, Gun.SubBulletInterval)) {
				subBulletTime = 0;
				EmitSubBullets();
			}
		}
		if (BulletLifetime > 0) {
			BulletLifetime -= (float)delta;
		} else {
			QueueFree();
		}
	}

	// Fires sub-bullets radially (with a per-burst spin) that deal half this bullet's
	// damage. Sub-bullets carry no Gun, so they don't sub-shoot or explode themselves.
	private void EmitSubBullets() {
		PackedScene scene = Gun.SubBulletType ?? Gun.BulletType;
		var parent = GetParent();
		if (scene == null || parent == null) return;
		int subDamage = Mathf.Max(1, Mathf.RoundToInt(Damage * 0.5f));
		int count = Mathf.Max(1, Gun.SubBulletCount);
		float speed = Gun.SubBulletSpeed > 0f ? Gun.SubBulletSpeed : Mathf.Max(200f, BulletSpeed * 0.6f);
		for (int i = 0; i < count; i++) {
			float angle = subBulletSpinAccum + i * (Mathf.Tau / count);
			Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			var b = scene.Instantiate<Bullet>();
			b.Direction = dir;
			b.Damage = subDamage;
			b.BulletLifetime = Gun.BulletLifetime > 0f ? Gun.BulletLifetime : 1.5f;
			b.BulletSpeed = speed;
			b.Element = Element;
			b.Position = Position;
			b.Rotation = dir.Angle();
			b.CollisionLayer = CollisionLayer;
			b.CollisionMask = CollisionMask;
			parent.CallDeferred("add_child", b);
		}
		subBulletSpinAccum += Mathf.DegToRad(Gun.SubBulletSpinDegrees);
	}

	private void _on_area_entered(Node2D node) {
		if (node is Pickup) return;
		if (Gun != null && node is Enemy e) {
			if (Gun.DotStacksPerHit > 0) {
				e.StatusEffects.AddStacks(StatusEffectType.DamageOverTime, Gun.DotStacksPerHit);
				GD.Print($"Bullet → {e.Name}: +{Gun.DotStacksPerHit} DoT (total {e.StatusEffects.GetStackCount(StatusEffectType.DamageOverTime)})");
			}
			if (Gun.SlowStacksPerHit > 0) {
				e.StatusEffects.AddStacks(StatusEffectType.Slow, Gun.SlowStacksPerHit);
				GD.Print($"Bullet → {e.Name}: +{Gun.SlowStacksPerHit} Slow (total {e.StatusEffects.GetStackCount(StatusEffectType.Slow)})");
			}
			if (Gun.FireRateStacksPerHit > 0) {
				e.StatusEffects.AddStacks(StatusEffectType.ReducedFireRate, Gun.FireRateStacksPerHit);
				GD.Print($"Bullet → {e.Name}: +{Gun.FireRateStacksPerHit} FireRate (total {e.StatusEffects.GetStackCount(StatusEffectType.ReducedFireRate)})");
			}
			if (Gun.BlindStacksPerHit > 0) {
				e.StatusEffects.AddStacks(StatusEffectType.Blind, Gun.BlindStacksPerHit);
				GD.Print($"Bullet → {e.Name}: +{Gun.BlindStacksPerHit} Blind (total {e.StatusEffects.GetStackCount(StatusEffectType.Blind)})");
			}
		}
		if (IsBoomerang) return;
		HandleHit(true);
	}
	// Records the walls the bullet spawns already overlapping so it isn't destroyed by
	// them (see spawnOverlapBodies). A point query is used instead of relying on
	// physics-frame timing: the spawnPhysicsFrame grace window can miss a body_entered
	// that arrives two or more physics frames after spawn (frame hitches, or render
	// running ahead of physics), which made bullets fired against a wall vanish.
	private void RecordSpawnOverlaps() {
		var space = GetWorld2D()?.DirectSpaceState;
		if (space == null) return;
		var query = new PhysicsPointQueryParameters2D();
		query.Position = GlobalPosition;
		query.CollisionMask = CollisionMask;
		query.CollideWithBodies = true;
		query.CollideWithAreas = false;
		foreach (var hit in space.IntersectPoint(query)) {
			if (hit["collider"].As<GodotObject>() is GodotObject collider) {
				spawnOverlapBodies.Add(collider.GetInstanceId());
			}
		}
	}

	private void _on_body_entered(Node2D node) {
		if (IsBoomerang) return;
		// Player hits are dealt (and the bullet despawned) via the player's hurtbox
		// Area2D in _on_area_entered. The physics body is a slightly different size, so
		// letting it destroy the bullet here would eat hits in the sliver between the two
		// shapes — the bullet would vanish on the player without ever dealing damage.
		if (node is Player) return;
		// Ignore any wall the bullet spawned inside (e.g. the player shooting while a
		// contracting wall overlaps them) so it doesn't vanish or detonate on spawn.
		// spawnOverlapBodies is seeded at spawn; the spawnPhysicsFrame window is a
		// fallback that also catches (and remembers) a spawn overlap the query missed.
		ulong id = node.GetInstanceId();
		if (spawnOverlapBodies.Contains(id)) return;
		if (Engine.GetPhysicsFrames() <= spawnPhysicsFrame + 1) {
			spawnOverlapBodies.Add(id);
			return;
		}
		HandleHit(false);
	}

	// Once the bullet clears a wall it was born in, stop ignoring that wall — so it dies
	// normally if it flies back into it (e.g. after a ricochet).
	private void _on_body_exited(Node2D node) {
		spawnOverlapBodies.Remove(node.GetInstanceId());
	}

	private void HandleHit(bool hitEnemy) {
		if (Gun == null) {
			QueueFree();
			return;
		}
		if (Gun.Explode) {
			CallDeferred("GenerateExplosion");
		}
		if (Gun.Gas) {
			CallDeferred("GenerateGasCloud");
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

	private void SpawnSplitBullets() =>
		SpawnSplitBulletsAt(GetParent(), Position, Damage, Element, CollisionLayer, CollisionMask);

	// Spawns Gun.Split+1 fan-out bullets at a point. Public so beam weapons (laser/lightning),
	// which have no travelling projectile, can trigger the same Split mod at their impact points.
	// The split bullets carry no Gun, so they never recurse.
	public void SpawnSplitBulletsAt(Node parent, Vector2 position, int damage, ElementType element, uint collisionLayer, uint collisionMask)
	{
		if (Gun?.BulletType == null || parent == null) return;
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		int count = Gun.Split + 1;
		for (int i = 0; i < count; i++) {
			var b = Gun.BulletType.Instantiate<Bullet>();
			float angle = rng.RandfRange(0, Mathf.Pi * 2f);
			b.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			b.Damage = damage;
			b.BulletLifetime = Gun.BulletLifetime > 0 ? Gun.BulletLifetime : 1f;
			b.Element = element;
			b.Position = position;
			b.Rotation = b.Direction.Angle();
			b.CollisionLayer = collisionLayer;
			b.CollisionMask = collisionMask;
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

	private void GenerateExplosion() => SpawnExplosionAt(GetParent(), Position, Damage);

	// Spawns this bullet's explosion scene at a point, dealing `damage`. Public so beam
	// weapons (laser/lightning), which have no travelling projectile, can trigger the same
	// Explode mod at their impact points.
	public void SpawnExplosionAt(Node parent, Vector2 position, int damage) {
		if (Explosion == null || parent == null) return;
		if (!global::Explosion.CanSpawn()) return;
		Explosion e = Explosion.Instantiate<Explosion>();
		e.Position = position;
		e.Damage = damage;
		if (Gun != null && Gun.ExplosionRadius > 0f) {
			float s = 1f + Gun.ExplosionRadius;
			e.Scale = new Vector2(s, s);
		}
		parent.AddChild(e);
	}

	private void GenerateGasCloud() =>
		SpawnGasCloudAt(GetParent(), Position, Damage, Element, GetCollisionLayerValue(4));

	// Spawns this bullet's gas cloud at a point, sized from the Gun's gas settings. Public so
	// beam weapons can trigger the same Gas mod at their impact points. `damagesEnemies`
	// mirrors the layer check below: player beams pass true, enemy beams false.
	public void SpawnGasCloudAt(Node parent, Vector2 position, int damage, ElementType element, bool damagesEnemies) {
		if (GasCloud == null || Gun == null || parent == null) return;
		if (!global::GasCloud.CanSpawn()) return;
		GasCloud cloud = GasCloud.Instantiate<GasCloud>();
		cloud.Position = position;
		cloud.Radius = Gun.GasRadius;
		cloud.Duration = Gun.GasDuration;
		cloud.DamageInterval = Gun.GasDamageInterval;
		cloud.Damage = Gun.GasDamage > 0 ? Gun.GasDamage : Mathf.Max(1, damage / 4);
		if (element != ElementType.NonElemental) cloud.Element = element;
		// Layer 4 = player attack, layer 5 = enemy attack. The bullet's own layer
		// decides who the cloud is hostile to, so player and enemy gas mirror correctly.
		cloud.DamagesEnemies = damagesEnemies;
		parent.AddChild(cloud);
	}
}
