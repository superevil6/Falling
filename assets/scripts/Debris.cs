using Godot;

// A small sprite that shoots outward from a dying enemy, spins, slows to a stop
// and fades out before freeing itself. Spawned by Enemy on death; see
// Enemy.SpawnDebris. Created entirely in code (no scene) like FloatingDamageText.
public partial class Debris : Node2D
{
	public Texture2D Texture;
	public Vector2 Velocity = Vector2.Zero;
	public float SpinSpeed = 0f;
	public float Duration = 0.8f;

	private float lifetime;
	private Sprite2D sprite;

	public override void _Ready()
	{
		lifetime = Duration;
		ZIndex = 19; // just under the death explosion (ZIndex 20)
		sprite = new Sprite2D();
		sprite.Texture = Texture;
		AddChild(sprite);
	}

	public override void _Process(double delta)
	{
		lifetime -= (float)delta;
		Position += Velocity * (float)delta;
		// Friction so the pieces decelerate as they fly out.
		Velocity *= Mathf.Pow(0.15f, (float)delta);
		Rotation += SpinSpeed * (float)delta;
		float t = Duration > 0f ? Mathf.Clamp(lifetime / Duration, 0f, 1f) : 0f;
		Modulate = new Color(1f, 1f, 1f, t);
		if (lifetime <= 0f) QueueFree();
	}

	// Spawns 'count' debris pieces bursting outward from globalPos. No-op when
	// count <= 0 or texture is null, so enemies without debris configured do nothing.
	public static void Burst(Node host, Vector2 globalPos, int count, Texture2D texture)
	{
		if (host == null || count <= 0 || texture == null) return;
		var parent = host.GetTree()?.CurrentScene;
		if (parent == null) return;
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		for (int i = 0; i < count; i++) {
			var debris = new Debris();
			debris.Texture = texture;
			float angle = rng.RandfRange(0f, Mathf.Pi * 2f);
			float speed = rng.RandfRange(120f, 260f);
			debris.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
			debris.SpinSpeed = rng.RandfRange(-8f, 8f);
			debris.Duration = rng.RandfRange(0.5f, 0.9f);
			parent.AddChild(debris);
			debris.GlobalPosition = globalPos;
			debris.Rotation = rng.RandfRange(0f, Mathf.Pi * 2f);
		}
	}
}
