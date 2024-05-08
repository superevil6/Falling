using Godot;
using System;

public partial class bullet : Area2D
{
	[Export]
	public float BulletSpeed {get;set;}= 100;
	public Vector2 direction {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		GlobalPosition = GlobalPosition + BulletSpeed * direction.Normalized() * (float)delta;

	}

	private void _on_area_entered(Node2D node){
		Hide();
	}
}
