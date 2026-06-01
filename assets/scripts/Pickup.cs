using Godot;

public partial class Pickup : Area2D
{
	[Export]
	public float MagnetRadius {get;set;} = 100f;
	[Export]
	public float MagnetSpeed {get;set;} = 300f;

	private Player player;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null && sprite.SpriteFrames != null && sprite.SpriteFrames.HasAnimation("Idle")) {
			sprite.Animation = "Idle";
			sprite.Play();
		}
	}

	public override void _Process(double delta)
	{
		if (player == null) {
			player = GetParent()?.GetNodeOrNull<Player>("Player");
			if (player == null) return;
		}
		float mult = player.ItemMagnetMultiplier;
		float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
		if (dist < MagnetRadius * mult && dist > 0.01f) {
			Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += toPlayer * MagnetSpeed * mult * (float)delta;
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player p) {
			OnCollected(p);
			QueueFree();
		}
	}

	protected virtual void OnCollected(Player player) { }
}
