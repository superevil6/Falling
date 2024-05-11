using Godot;

public partial class MeleeAttack : Attack
{
	public float SwingDuration {get;set;}
	public Vector2 Direction {get;set;}
	public float MeleeSpeed {get;set;} = 100;
	public AnimatedSprite2D animatedSprite2D;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animatedSprite2D.Animation = "Slash";
		animatedSprite2D.Play();
		GD.Print(SwingDuration);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// GlobalPosition = GlobalPosition + MeleeSpeed * Direction.Normalized() * (float)delta;
		if (SwingDuration > 0) {
			SwingDuration -= (float)delta;
		} else {
			QueueFree();
		}

	}
}

