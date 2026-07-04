using Godot;

// Self-contained boss attack: a small beam is launched straight up from the spawn
// point; when it reaches the top of the screen, warning indicators appear across
// the top and, after a wind-up, large vertical lasers rain down at those spots.
// Created in code by Enemy.LaunchLaserRain and freed when the sequence finishes.
public partial class LaserRainAttack : Node2D
{
	// Number of large lasers that rain down.
	public int LaserCount {get;set;} = 4;
	// Wind-up between the warning indicators appearing and the lasers falling.
	public float WarningDuration {get;set;} = 1f;
	public int LaserDamage {get;set;} = 3;
	public float LaserThickness {get;set;} = 60f;
	// How long each falling laser stays active.
	public float LaserDuration {get;set;} = 1.2f;
	// Travel time for the small beam to reach the top of the screen.
	public float FlareTime {get;set;} = 0.4f;
	public Color LaserColor {get;set;} = new Color(1f, 0.3f, 0.3f);
	// Horizontal margin kept clear of the screen edges when spreading the lasers.
	public float EdgeMargin {get;set;} = 120f;

	private enum RainState { Flare, Warning, Done }
	private RainState state = RainState.Flare;
	private float timer;
	private float topLocalY;
	private readonly System.Collections.Generic.List<float> targetXs = new System.Collections.Generic.List<float>();
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	private static PackedScene indicatorScene;
	private static PackedScene laserScene;

	public override void _Ready()
	{
		rng.Randomize();
		ZIndex = 5;
		// Local Y of the top of the screen (world y = 0) relative to our origin.
		topLocalY = -GlobalPosition.Y;
		if (indicatorScene == null)
			indicatorScene = GD.Load<PackedScene>("res://assets/objects/SpawnIndicator.tscn");
		if (laserScene == null)
			laserScene = GD.Load<PackedScene>("res://assets/objects/LaserObstacle.tscn");
		PickTargets();
		timer = Mathf.Max(0.01f, FlareTime);
	}

	// Chooses evenly spread X positions across the play area with a little jitter.
	private void PickTargets()
	{
		float width = GetViewportRect().Size.X;
		float minX = EdgeMargin;
		float maxX = Mathf.Max(EdgeMargin, width - EdgeMargin);
		int count = Mathf.Max(1, LaserCount);
		if (count == 1) {
			targetXs.Add((minX + maxX) * 0.5f);
			return;
		}
		float span = maxX - minX;
		float step = span / (count - 1);
		float jitter = step * 0.25f;
		for (int i = 0; i < count; i++) {
			float x = minX + step * i + rng.RandfRange(-jitter, jitter);
			targetXs.Add(Mathf.Clamp(x, minX, maxX));
		}
	}

	public override void _Process(double delta)
	{
		timer -= (float)delta;
		QueueRedraw();
		switch (state) {
			case RainState.Flare:
				if (timer <= 0f) {
					SpawnWarnings();
					timer = Mathf.Max(0.01f, WarningDuration);
					state = RainState.Warning;
				}
				break;
			case RainState.Warning:
				if (timer <= 0f) {
					SpawnLasers();
					state = RainState.Done;
					QueueFree();
				}
				break;
		}
	}

	public override void _Draw()
	{
		if (state != RainState.Flare) return;
		// Beam growing from the origin up to the advancing head.
		float t = Mathf.Clamp(1f - timer / Mathf.Max(0.01f, FlareTime), 0f, 1f);
		float headY = topLocalY * t;
		float thickness = 8f;
		Color core = new Color(LaserColor.R, LaserColor.G, LaserColor.B, 1f);
		Color glow = new Color(LaserColor.R, LaserColor.G, LaserColor.B, 0.4f);
		DrawLine(new Vector2(0, 0), new Vector2(0, headY), glow, thickness);
		DrawLine(new Vector2(0, 0), new Vector2(0, headY), core, thickness * 0.4f);
		DrawCircle(new Vector2(0, headY), thickness, core);
	}

	private void SpawnWarnings()
	{
		if (indicatorScene == null) return;
		float warnY = 80f;
		foreach (float x in targetXs) {
			var ind = indicatorScene.Instantiate<SpawnIndicator>();
			ind.TriangleColor = LaserColor;
			ind.Duration = WarningDuration;
			ind.GlobalPosition = new Vector2(x, warnY);
			GetParent().AddChild(ind);
		}
	}

	private void SpawnLasers()
	{
		if (laserScene == null) return;
		foreach (float x in targetXs) {
			var laser = laserScene.Instantiate<LaserObstacle>();
			laser.IsHorizontal = false;
			laser.Damage = LaserDamage;
			laser.Thickness = LaserThickness;
			laser.Duration = LaserDuration;
			laser.LaserColor = LaserColor;
			laser.GlobalPosition = new Vector2(x, 0f);
			GetParent().AddChild(laser);
		}
	}
}
