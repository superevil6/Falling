using Godot;

// A lingering cloud of gas spawned where a gas bullet hits. It repeatedly damages
// everything inside its radius on a fixed interval, then fades away over its
// lifetime. Direction of harm is decided at spawn: a cloud from a player bullet
// damages enemies; a cloud from an enemy bullet damages the player.
public partial class GasCloud : Area2D
{
	[Export]
	public float Radius {get;set;} = 60f;
	[Export]
	public float Duration {get;set;} = 4f;
	[Export]
	public float DamageInterval {get;set;} = 0.5f;
	[Export]
	public ElementType Element {get;set;} = ElementType.Grease;
	// Damage applied to each valid target every DamageInterval seconds.
	public int Damage {get;set;} = 5;
	// true  → player-fired cloud, damages enemies.
	// false → enemy-fired cloud, damages the player.
	public bool DamagesEnemies {get;set;} = true;

	private float elapsedTime = 0f;
	private float tickTimer = 0f;

	// Mirror Explosion's global cap so a wall of gas bullets can't tank the frame rate.
	public static int MaxActive = 20;
	private static int activeCount = 0;
	public static bool CanSpawn() => activeCount < MaxActive;

	public override void _ExitTree()
	{
		activeCount--;
	}

	public override void _Ready()
	{
		activeCount++;
		// The cloud damages by scanning groups, not by physics contact, so it never
		// needs to monitor or be monitored.
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);
		var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null) {
			// Scale the animated art so its frame spans the damage diameter.
			var firstFrame = sprite.SpriteFrames?.GetFrameTexture(sprite.Animation, 0);
			if (firstFrame != null && firstFrame.GetWidth() > 0) {
				float spriteScale = (2f * Radius) / firstFrame.GetWidth();
				sprite.Scale = new Vector2(spriteScale, spriteScale);
			}
			sprite.Play();
		}
		// First tick lands immediately so contact damage feels instant on impact.
		ApplyDamageInRadius();
	}

	public override void _Process(double delta)
	{
		elapsedTime += (float)delta;
		tickTimer += (float)delta;
		if (tickTimer >= DamageInterval) {
			tickTimer -= DamageInterval;
			ApplyDamageInRadius();
		}
		// Fade out over the back half of the cloud's life.
		float life = Duration > 0f ? Mathf.Clamp(elapsedTime / Duration, 0f, 1f) : 1f;
		float alpha = life < 0.5f ? 1f : Mathf.Clamp(1f - (life - 0.5f) / 0.5f, 0f, 1f);
		Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, alpha);
		if (elapsedTime >= Duration) QueueFree();
	}

	private void ApplyDamageInRadius()
	{
		float r = Radius * Mathf.Abs(Scale.X);
		float rSq = r * r;
		if (DamagesEnemies) {
			foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
				if (n is Enemy e && e.CurrentHealth > 0
					&& e.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= rSq) {
					e.TakeDamage(Damage, Element);
				}
			}
		} else {
			var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
			if (player != null && player.CurrentHealth > 0
				&& player.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= rSq) {
				player.TakeDamage(Damage, Element);
			}
		}
	}
}
