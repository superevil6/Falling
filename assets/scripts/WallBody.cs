using Godot;

public partial class WallBody : StaticBody2D
{
	[Export]
	public Wall WallData {get;set;}

	public override void _Ready()
	{
		if (WallData == null) return;
		if (WallData.Graphics != null && WallData.Graphics.Length > 0) {
			var rng = new RandomNumberGenerator();
			GetNode<Sprite2D>("Sprite2D").Texture = WallData.Graphics[rng.RandiRange(0, WallData.Graphics.Length - 1)];
		}
		if (WallData.CollisionShape != null) {
			GetNode<CollisionShape2D>("CollisionShape2D").Shape = WallData.CollisionShape;
		}
	}
}
