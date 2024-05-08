using Godot;

public partial class Bullet : Attack
{
	[Export]
	public float BulletSpeed {get;set;}= 100;
	public float BulletLifetime {get;set;}
	public Vector2 Direction {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		GlobalPosition = GlobalPosition + BulletSpeed * Direction.Normalized() * (float)delta;
		if (BulletLifetime > 0) {
			BulletLifetime -= (float)delta;
		} else {
			QueueFree();
		}

	}

	private void _on_area_entered(Node2D node){
		QueueFree();
	}
}
