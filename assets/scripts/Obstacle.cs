using Godot;

public partial class Obstacle : Area2D
{
	[Export]
	public float Speed {get;set;} = 200f;
	[Export]
	public float Height {get;set;} = 100f;
	[Export]
	public bool DealsDamage {get;set;} = false;
	[Export]
	public int Damage {get;set;} = 1;
	[Export(PropertyHint.Range, "-1.0,1.0,0.05")]
	public float InputSpeedFactor {get;set;} = 0.5f;
	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	public float WallContactSpeedFactor {get;set;} = 0.1f;

	private Player player;

	public override void _Ready()
	{
		if (DealsDamage) {
			AreaEntered += OnAreaEntered;
			BodyEntered += OnBodyEntered;
		}
		var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null && sprite.Texture != null && Height > 0f) {
			float texHeight = sprite.Texture.GetHeight();
			if (texHeight > 0f) {
				sprite.Scale = new Vector2(sprite.Scale.X, Height / texHeight);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (player == null) player = GetParent()?.GetNodeOrNull<Player>("Player");
		float inputY = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
		float wallFactor = (player != null && player.IsTouchingWall) ? WallContactSpeedFactor : 1f;
		float effectiveSpeed = Speed * (1 + inputY * InputSpeedFactor) * wallFactor;
		Position += new Vector2(0, -effectiveSpeed * (float)delta);
		if (GlobalPosition.Y + Height / 2f < 0f) QueueFree();
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
