using Godot;
using System;

public partial class Attack : Area2D
{
	public float SwingDurration {get;set;}
	public int Damage {get;set;}
	public Vector2 Direction {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (SwingDurration > 0) {
			SwingDurration -= (float)delta;
		} else {
			Hide();
		}

	}
}
