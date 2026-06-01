using Godot;

public partial class AttackIndicator : Node2D
{
	public float Duration {get;set;} = 0.5f;
	public float Length {get;set;} = 36f;
	public Color NeedleColor {get;set;} = new Color(1f, 0.3f, 0.3f);
	public Node2D Anchor {get;set;}

	private float lifetime;

	public override void _Ready()
	{
		lifetime = Duration;
		ZIndex = 4;
	}

	public override void _Process(double delta)
	{
		lifetime -= (float)delta;
		if (Anchor != null && IsInstanceValid(Anchor)) {
			GlobalPosition = Anchor.GlobalPosition;
		}
		QueueRedraw();
		if (lifetime <= 0f) QueueFree();
	}

	public override void _Draw()
	{
		float t = Mathf.Clamp(lifetime / Duration, 0f, 1f);
		float pulse = 0.75f + 0.25f * Mathf.Sin((Duration - lifetime) * 30f);
		Color core = new Color(NeedleColor.R, NeedleColor.G, NeedleColor.B, NeedleColor.A * t);
		Color glow = new Color(NeedleColor.R, NeedleColor.G, NeedleColor.B, NeedleColor.A * t * 0.3f);
		DrawLine(Vector2.Zero, new Vector2(Length, 0f), glow, 5f);
		DrawLine(Vector2.Zero, new Vector2(Length, 0f), core, 1.5f);
		DrawCircle(new Vector2(Length, 0f), 2.5f * pulse, core);
	}
}
