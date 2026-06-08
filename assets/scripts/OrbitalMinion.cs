using Godot;

public partial class OrbitalMinion : Node2D
{
	public PackedScene BulletScene;
	public int Damage = 1;
	public float FireRate = 1.5f;
	public float Range = 500f;
	public float BulletSpeed = 800f;
	public float BulletLifetime = 1.5f;

	private float fireCooldown;

	public override void _Ready()
	{
		fireCooldown = FireRate;
		ZIndex = 4;
	}

	public override void _Process(double delta)
	{
		fireCooldown -= (float)delta;
		if (fireCooldown <= 0f) {
			TryShoot();
			fireCooldown = FireRate;
		}
	}

	private void TryShoot()
	{
		if (BulletScene == null) return;
		Enemy target = FindNearestEnemy();
		if (target == null) return;
		Vector2 dir = (target.GlobalPosition - GlobalPosition).Normalized();
		Bullet b = BulletScene.Instantiate<Bullet>();
		b.Set("Direction", dir);
		b.Set("Damage", Damage);
		b.Set("BulletLifetime", BulletLifetime);
		if (BulletSpeed > 0f) b.BulletSpeed = BulletSpeed;
		b.AuraColor = new Color(0.7f, 1f, 0.4f, 0.85f);
		b.Rotation = dir.Angle();
		b.SetCollisionLayerValue(4, true);
		b.SetCollisionMaskValue(3, true);
		b.SetCollisionMaskValue(1, true);
		var scene = GetTree()?.CurrentScene;
		if (scene == null) return;
		scene.AddChild(b);
		b.GlobalPosition = GlobalPosition;
	}

	private Enemy FindNearestEnemy()
	{
		Enemy best = null;
		float bestDist = float.MaxValue;
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e.CurrentHealth > 0) {
				float d = GlobalPosition.DistanceTo(e.GlobalPosition);
				if (d > Range) continue;
				if (d < bestDist) { bestDist = d; best = e; }
			}
		}
		return best;
	}

	public override void _Draw()
	{
		Color glow = new Color(0.7f, 1f, 0.4f, 0.25f);
		Color body = new Color(0.5f, 0.95f, 0.35f, 0.95f);
		DrawCircle(Vector2.Zero, 14f, glow);
		DrawCircle(Vector2.Zero, 9f, body);
		DrawArc(Vector2.Zero, 9f, 0f, Mathf.Tau, 24, new Color(0.95f, 1f, 0.85f, 0.9f), 1.5f);
	}
}
