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
		if (Gun.SizeMultiplier > 0) {
			Scale = new Vector2(Scale.X + Gun.SizeMultiplier, Scale.Y + Gun.SizeMultiplier);
		}
		if (Gun.BulletSize > 0) {
			Scale = new Vector2(Scale.X * Gun.BulletSize, Scale.Y * Gun.BulletSize);
		}
		RemainingRicochets = Gun.Ricochet;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
		if (BulletLifetime > 0) {
			BulletLifetime -= (float)delta;
		} else {
			QueueFree();
		}
	}

	private void _on_area_entered(Node2D node) {
		if (node is Pickup) return;
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
		GetParent().AddChild(e);
	}
}
