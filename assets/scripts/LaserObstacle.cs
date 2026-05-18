using Godot;

public partial class LaserObstacle : Area2D
{
	[Export]
	public int Damage {get;set;} = 2;
	[Export]
	public float Thickness {get;set;} = 20f;
	[Export]
	public float Duration {get;set;} = 2f;
	[Export]
	public bool IsHorizontal {get;set;} = false;
	[Export]
	public Color LaserColor {get;set;} = new Color(1f, 0.3f, 0.3f);

	private float remainingTime;
	private float length;

	public override void _Ready()
	{
		remainingTime = Duration;
		var viewport = GetViewportRect().Size;
		length = IsHorizontal ? viewport.X : viewport.Y;
		var coll = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (coll != null) {
			var shape = new RectangleShape2D();
			shape.Size = IsHorizontal
				? new Vector2(length, Thickness)
				: new Vector2(Thickness, length);
			coll.Shape = shape;
		}
		GlobalPosition = IsHorizontal
			? new Vector2(viewport.X / 2f, GlobalPosition.Y)
			: new Vector2(GlobalPosition.X, viewport.Y / 2f);
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		remainingTime -= (float)delta;
		QueueRedraw();
		if (remainingTime <= 0f) QueueFree();
	}

	public override void _Draw()
	{
		float pulse = 0.7f + 0.3f * Mathf.Sin((Duration - remainingTime) * 20f);
		Color body = new Color(LaserColor.R * pulse, LaserColor.G * pulse, LaserColor.B * pulse, LaserColor.A);
		Color core = new Color(1f, 1f, 1f, LaserColor.A * 0.8f);
		float coreThickness = Thickness * 0.3f;
		Rect2 bodyRect;
		Rect2 coreRect;
		if (IsHorizontal) {
			bodyRect = new Rect2(-length / 2f, -Thickness / 2f, length, Thickness);
			coreRect = new Rect2(-length / 2f, -coreThickness / 2f, length, coreThickness);
		} else {
			bodyRect = new Rect2(-Thickness / 2f, -length / 2f, Thickness, length);
			coreRect = new Rect2(-coreThickness / 2f, -length / 2f, coreThickness, length);
		}
		DrawRect(bodyRect, body);
		DrawRect(coreRect, core);
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player p) p.TakeDamage(Damage);
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player p) p.TakeDamage(Damage);
	}
}
