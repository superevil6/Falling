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
	}

	public override void _Process(double delta)
	{
		if (player == null) {
			player = GetParent()?.GetNodeOrNull<Player>("Player");
			if (player == null) return;
		}
		float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
		if (dist < MagnetRadius && dist > 0.01f) {
			Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += toPlayer * MagnetSpeed * (float)delta;
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
