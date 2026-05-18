using Godot;

public partial class ObstacleSpawner : Node2D
{
	[Export]
	public PackedScene[] Obstacles {get;set;}
	[Export]
	public float MinInterval {get;set;} = 2f;
	[Export]
	public float MaxInterval {get;set;} = 5f;
	[Export]
	public float MinX {get;set;} = 100f;
	[Export]
	public float MaxX {get;set;} = 1400f;
	[Export]
	public float SpawnYBelow {get;set;} = 100f;
	[Export]
	public float WarningDuration {get;set;} = 1f;
	[Export]
	public float WarningYOffset {get;set;} = 80f;
	[Export]
	public Color WarningColor {get;set;} = new Color(0.3f, 0.6f, 1f);
	[Export]
	public float StaticSpawnMargin {get;set;} = 120f;

	private enum SpawnState { Waiting, Warning }
	private SpawnState state = SpawnState.Waiting;
	private float spawnTimer = 0f;
	private float warningTimer = 0f;
	private float pendingX;
	private float pendingY;
	private PackedScene pendingScene;
	private RandomNumberGenerator rng = new RandomNumberGenerator();
	private float viewportHeight;
	private static PackedScene indicatorScene;

	public override void _Ready()
	{
		rng.Randomize();
		viewportHeight = GetViewportRect().Size.Y;
		spawnTimer = rng.RandfRange(MinInterval, MaxInterval);
		if (indicatorScene == null) {
			indicatorScene = GD.Load<PackedScene>("res://assets/objects/SpawnIndicator.tscn");
		}
	}

	public override void _Process(double delta)
	{
		if (Obstacles == null || Obstacles.Length == 0) return;
		switch (state) {
			case SpawnState.Waiting:
				spawnTimer -= (float)delta;
				if (spawnTimer <= 0f) {
					pendingScene = Obstacles[rng.RandiRange(0, Obstacles.Length - 1)];
					pendingX = rng.RandfRange(MinX, MaxX);
					bool isStatic = IsStaticObstacle(pendingScene);
					if (isStatic) {
						pendingY = rng.RandfRange(StaticSpawnMargin, viewportHeight - StaticSpawnMargin);
					} else {
						pendingY = viewportHeight + SpawnYBelow;
					}
					float warnY = isStatic ? pendingY : viewportHeight - WarningYOffset;
					SpawnWarning(pendingX, warnY);
					warningTimer = WarningDuration;
					state = SpawnState.Warning;
				}
				break;
			case SpawnState.Warning:
				warningTimer -= (float)delta;
				if (warningTimer <= 0f) {
					SpawnObstacle();
					spawnTimer = rng.RandfRange(MinInterval, MaxInterval);
					state = SpawnState.Waiting;
				}
				break;
		}
	}

	private bool IsStaticObstacle(PackedScene scene)
	{
		if (scene == null) return false;
		var preview = scene.Instantiate<Node>();
		bool isStatic = preview is LaserObstacle;
		preview.Free();
		return isStatic;
	}

	private void SpawnWarning(float x, float y)
	{
		if (indicatorScene == null) return;
		var ind = indicatorScene.Instantiate<SpawnIndicator>();
		ind.TriangleColor = WarningColor;
		ind.Duration = WarningDuration;
		ind.GlobalPosition = new Vector2(x, y);
		GetParent().AddChild(ind);
	}

	private void SpawnObstacle()
	{
		if (pendingScene == null) return;
		var obs = pendingScene.Instantiate<Node2D>();
		obs.GlobalPosition = new Vector2(pendingX, pendingY);
		GetParent().AddChild(obs);
	}
}
