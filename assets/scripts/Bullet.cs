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
			ElementType.Fire => "res://assets/objects/Fire.gdshader",
			ElementType.Ice => "res://assets/objects/Ice.gdshader",
			ElementType.Electric => "res://assets/objects/Lightning.gdshader",
			ElementType.Poison => "res://assets/objects/Poison.gdshader",
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
		RemainingRicochets = Gun.Ricochet;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Gun != null) {
			if (Gun.Wave > 0) {
				time += delta;
				Position += new Vector2(
				((float)Mathf.Cos(time * (Gun.Wave * 1.5))) * Direction.Y,
				((float)Mathf.Cos(time * (Gun.Wave * 1))) * Direction.X).Normalized() * BulletSpeed * (float)delta;
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
		HandleHit();
	}
	private void _on_body_entered(Node2D node) => HandleHit();

	private void HandleHit() {
		if (Gun == null) {
			QueueFree();
			return;
		}
		if (Gun.Explode) {
			CallDeferred("GenerateExplosion");
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
