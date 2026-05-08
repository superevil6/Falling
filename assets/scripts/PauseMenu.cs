using Godot;

public partial class PauseMenu : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print(GetNode<Button>("VBoxContainer/Unpause"));
		GetNode<Button>("VBoxContainer/Unpause").GrabFocus();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause"))
		{
			GD.Print("pause pushed");
			GetTree().Paused = !GetTree().Paused;
			if (GetTree().Paused)
			{
				GetNode<Button>("VBoxContainer/Unpause").GrabFocus();
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
