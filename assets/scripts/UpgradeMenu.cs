using Godot;

public partial class UpgradeMenu : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("upgrademenu"))
		{
			GetTree().Paused = !GetTree().Paused;
			if (GetTree().Paused)
			{
				Engine.TimeScale = 0;
				Show();
			}
			else
			{
				Engine.TimeScale = 1;
				Hide();
			}
		}
	}
}
