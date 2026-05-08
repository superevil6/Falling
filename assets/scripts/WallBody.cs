using Godot;

public partial class WallBody : StaticBody2D
{
	[Export]
	public Wall WallData {get;set;}

	public override void _Ready()
	{
		if (WallData == null) return;
		if (WallData.Graphic != null) {
			GetNode<Sprite2D>("Sprite2D").Texture = WallData.Graphic;
		}
		if (WallData.CollisionShape != null) {
			GetNode<CollisionShape2D>("CollisionShape2D").Shape = WallData.CollisionShape;
		}
	}
}
