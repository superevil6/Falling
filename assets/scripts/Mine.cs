using Godot;

public partial class Mine : Area2D
{
	[Export]
	public int Damage {get;set;} = 5;
	[Export]
	public float Radius {get;set;} = 100f;
	[Export]
	public bool TargetsEnemy {get;set;} = false;
	[Export]
	public bool TargetsPlayer {get;set;} = true;

	private static PackedScene explosionScene;
	private bool detonated = false;

	// When launched, the mine slides toward TargetPosition at LaunchSpeed and only
	// arms (becomes able to detonate) once it lands and comes to rest.
	private bool launching = false;
	private Vector2 targetPosition;
	private float launchSpeed = 500f;

	// Launches the mine toward a target position; it travels there before stopping
	// and staying stationary. It will not detonate while in flight.
	public void LaunchTo(Vector2 target, float speed = 500f)
	{
		targetPosition = target;
		launchSpeed = speed;
		launching = true;
	}

	public override void _Ready()
	{
		if (explosionScene == null) {
			explosionScene = GD.Load<PackedScene>("res://assets/objects/Explosion.tscn");
		}
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!launching) return;
		Vector2 toTarget = targetPosition - GlobalPosition;
		float step = launchSpeed * (float)delta;
		if (toTarget.Length() <= step) {
			GlobalPosition = targetPosition;
			launching = false;
		} else {
			GlobalPosition += toTarget.Normalized() * step;
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (detonated || launching) return;
		if (area.GetParent() is Player) Detonate();
	}

	private void OnBodyEntered(Node body)
	{
		if (detonated || launching) return;
		if (body is Player) Detonate();
	}

	private void Detonate()
	{
		detonated = true;
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
		DrawCircle(Vector2.Zero, 10f, new Color(0.3f, 0.1f, 0.05f));
		DrawCircle(Vector2.Zero, 10f, new Color(0.7f, 0.2f, 0.1f), false, 1.5f);
		DrawCircle(Vector2.Zero, 4f, new Color(1f, 0.4f, 0.1f));
		for (int i = 0; i < 4; i++) {
			float a = i * Mathf.Pi / 2f;
			Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
			DrawLine(dir * 8f, dir * 14f, new Color(0.5f, 0.5f, 0.5f), 2f);
		}
	}
}
