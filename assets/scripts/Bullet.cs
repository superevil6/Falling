using System;
using System.Linq;
using Godot;

public partial class Bullet : Attack
{
	[Export]
	public float BulletSpeed {get;set;}= 100;
	public float BulletLifetime {get;set;}
	public Vector2 Direction {get;set;}
    [Export]
	public BulletMod[] BulletMods {get;set;}

	//for wave movement
	private double time;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (BulletMods !=null) {
			if (BulletMods.Any(mod => mod.Wave > 0)) {
				float wave = BulletMods.Sum(mod => mod.Wave);
				time += delta;
				Position = 
				Position + new Vector2(
				((float)Mathf.Cos(time * (wave * 1.5))) * Direction.Y, 
				((float)Mathf.Cos(time * (wave * 1))) * Direction.X).Normalized() * BulletSpeed * (float)delta;
				// var movement = Math.Cos(time * wave)
				// GlobalPosition = GlobalPosition + BulletSpeed * Direction.Normalized() * (float)delta;
				// var perpendicular = new Vector2(Direction.Y, -Direction.X);
				// GlobalPosition += (0.5f * Direction + BulletMods.Sum(mod => mod.Wave) * perpendicular * Math.Sin(0)) * BulletSpeed * delta;
				// float movement = (float)Math.Cos(time*1)*BulletMods.Sum(mod => mod.Wave);
				// GlobalPosition = GlobalPosition + BulletSpeed * new Vector2(Direction.X * movement, Direction.Y * movement) * (float)delta;
				// Vector2 pos = GlobalPosition;
				// pos.Y = Mathf.Sin((float) time * 5) * wave + Direction.Y;
				// pos.X = Mathf.Sin((float) time * 5) * wave + Direction.X;
				// GlobalPosition += pos;
			}
		}
		GlobalPosition = GlobalPosition + BulletSpeed * Direction.Normalized() * (float)delta;
		if (BulletLifetime > 0) {
			BulletLifetime -= (float)delta;
		} else {
			QueueFree();
		}

	}

	private void _on_area_entered(Node2D node){
		GD.Print("Bullet entered collider");
		if (BulletMods != null) {
			if (!BulletMods.Any(mod => mod.Pierce == true)) {
				QueueFree();
			}
		}
		 else {
			QueueFree();
		}
	}
}
