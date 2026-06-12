using Godot;

public partial class Bomb : Node2D
{
	[Export]
	public float FuseTime {get;set;} = 2f;
	[Export]
	public int Damage {get;set;} = 5;
	[Export]
	public float Radius {get;set;} = 100f;
	[Export]
	public bool TargetsEnemy {get;set;} = true;
	[Export]
	public bool TargetsPlayer {get;set;} = false;

	private float remainingFuse;
	private static PackedScene explosionScene;

	public override void _Ready()
	{
		remainingFuse = FuseTime;
		if (explosionScene == null) {
			explosionScene = GD.Load<PackedScene>("res://assets/objects/Explosion.tscn");
		}
	}

	public override void _Process(double delta)
	{
		remainingFuse -= (float)delta;
		QueueRedraw();
		if (remainingFuse <= 0f) Detonate();
	}

	private void Detonate()
	{
		if (TargetsEnemy) {
			foreach (var node in GetTree().GetNodesInGroup("Enemy")) {
				if (node is Enemy e && e.CurrentHealth > 0
					&& e.GlobalPosition.DistanceTo(GlobalPosition) <= Radius) {
					e.TakeDamage(Damage, ElementType.NonElemental);
				}
			}
		}
		if (TargetsPlayer) {
			var p = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
			if (p != null && p.CurrentHealth > 0
				&& p.GlobalPosition.DistanceTo(GlobalPosition) <= Radius) {
				p.TakeDamage(Damage);
			}
		}
		if (explosionScene != null && Explosion.CanSpawn()) {
			var ex = explosionScene.Instantiate<Explosion>();
			ex.GlobalPosition = GlobalPosition;
			ex.Damage = Damage;
			GetParent().AddChild(ex);
		}
		QueueFree();
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 12f, new Color(0.1f, 0.1f, 0.1f));
		DrawCircle(Vector2.Zero, 12f, new Color(0.4f, 0.4f, 0.4f), false, 1.5f);
		DrawLine(new Vector2(-3, -8), new Vector2(-3, -18), new Color(0.6f, 0.4f, 0.2f), 2f);
		float t = Mathf.Clamp(remainingFuse / Mathf.Max(FuseTime, 0.001f), 0f, 1f);
		float sparkRadius = 2.5f + Mathf.Abs(Mathf.Sin((FuseTime - remainingFuse) * 20f)) * 1.5f;
		DrawCircle(new Vector2(-3, -18), sparkRadius, new Color(1f, 0.7f - (1f - t) * 0.4f, 0.1f));
	}
}
