using Godot;

public partial class SpawnIndicator : Node2D
{
	[Export]
	public float Duration {get;set;} = 1.0f;
	[Export]
	public float Radius {get;set;} = 32.0f;
	[Export]
	public Color TriangleColor {get;set;} = new Color(1f, 0.8f, 0f);

	private float lifetime;

	public override void _Ready()
	{
		lifetime = Duration;
	}

	public override void _Process(double delta)
	{
		lifetime -= (float)delta;
		float t = Mathf.Clamp(lifetime / Duration, 0f, 1f);
		float pulse = 1.0f + 0.15f * Mathf.Sin((Duration - lifetime) * 14.0f);
		Scale = new Vector2(pulse, pulse);
		Modulate = new Color(1, 1, 1, t);
		if (lifetime <= 0) QueueFree();
	}

	public override void _Draw()
	{
		float s = Radius;
		Vector2 top = new Vector2(0, -s);
		Vector2 left = new Vector2(-s * 0.866f, s * 0.5f);
		Vector2 right = new Vector2(s * 0.866f, s * 0.5f);
		var triangle = new Vector2[] { top, left, right };
		DrawColoredPolygon(triangle, TriangleColor);
		float outlineWidth = 3.0f;
		DrawLine(top, left, Colors.Black, outlineWidth);
		DrawLine(left, right, Colors.Black, outlineWidth);
		DrawLine(right, top, Colors.Black, outlineWidth);
		float barWidth = s * 0.15f;
		float barHeight = s * 0.5f;
		float barTop = -s * 0.3f;
		DrawRect(new Rect2(-barWidth / 2f, barTop, barWidth, barHeight), Colors.Black);
		float dotRadius = barWidth * 0.7f;
		DrawCircle(new Vector2(0, s * 0.32f), dotRadius, Colors.Black);
	}
}
