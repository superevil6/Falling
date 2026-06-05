using Godot;

public partial class FloatingDamageText : Node2D
{
	public float Duration = 1.0f;
	public Vector2 Velocity = Vector2.Zero;
	public string Text = "";
	public Color TextColor = Colors.White;

	private float lifetime;
	private Label label;

	public override void _Ready()
	{
		lifetime = Duration;
		ZIndex = 50;
		label = new Label();
		label.Text = Text;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AddThemeColorOverride("font_color", TextColor);
		label.AddThemeFontSizeOverride("font_size", 18);
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
		label.Size = new Vector2(80f, 28f);
		label.Position = new Vector2(-40f, -14f);
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(label);
	}

	public override void _Process(double delta)
	{
		lifetime -= (float)delta;
		Position += Velocity * (float)delta;
		Velocity *= Mathf.Pow(0.18f, (float)delta);
		float t = Duration > 0f ? Mathf.Clamp(lifetime / Duration, 0f, 1f) : 0f;
		Modulate = new Color(1f, 1f, 1f, t);
		if (lifetime <= 0f) QueueFree();
	}

	public static Color ElementColor(ElementType element, Color fallback)
	{
		return element switch {
			ElementType.Fire => new Color(1f, 0.5f, 0.1f),
			ElementType.Electric => new Color(1f, 0.95f, 0.2f),
			ElementType.Ice => new Color(0.4f, 0.7f, 1f),
			ElementType.Poison => new Color(0.7f, 0.3f, 0.9f),
			_ => fallback,
		};
	}

	public static void Spawn(Node host, Vector2 globalPos, int amount, Color color)
	{
		if (host == null || amount <= 0) return;
		var scene = host.GetTree()?.CurrentScene;
		if (scene == null) return;
		var text = new FloatingDamageText();
		text.Text = amount.ToString();
		text.TextColor = color;
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		float angle = rng.RandfRange(-Mathf.Pi * 0.7f, -Mathf.Pi * 0.3f);
		float speed = rng.RandfRange(80f, 140f);
		text.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
		scene.AddChild(text);
		text.GlobalPosition = globalPos + new Vector2(rng.RandfRange(-10f, 10f), rng.RandfRange(-10f, 0f));
	}
}
