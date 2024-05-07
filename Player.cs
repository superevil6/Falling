using Godot;
using System;
using System.Collections.Generic;

public partial class Player : Area2D
{
	[Export]
	public int Speed { get;set;} = 400;
	[Export]
	public int BulletCount {get;set;} = 20;
	[Export]
	public PackedScene BulletObject {get;set;}
	// public List<Area2D> Bullets = new List<Area2D>();
	public Vector2 ScreenSize;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		#region Movement
		Vector2 velocity = Vector2.Zero;
		animatedSprite2D.Animation = "Falling";
		animatedSprite2D.Play();
		if (Input.IsActionPressed("move_up")) {
			velocity.Y -= 1.0f;
		}
		if (Input.IsActionPressed("move_down")) {
			velocity.Y += 1.0f;
		}
		if (Input.IsActionPressed("move_left")) {
			velocity.X -= 1.0f;
		}
		if (Input.IsActionPressed("move_right")) {
			velocity.X += 1.0f;
		}
	
		if (velocity.Length() > 0) {
			velocity = velocity.Normalized() * Speed;
		}

		Position += velocity * (float)delta;
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);
		#endregion
		#region Attacks
		Vector2 aimDirection = Vector2.Zero;
		if (Input.IsActionPressed("aim_up")) {
			aimDirection.Y -= Input.GetActionStrength("aim_up");
			GD.Print(aimDirection);
		}
		if (Input.IsActionPressed("aim_down")) {
			aimDirection.Y += Input.GetActionStrength("aim_down");
			GD.Print(aimDirection);
		}
		if (Input.IsActionPressed("aim_left")) {
			aimDirection.X -= Input.GetActionStrength("aim_left");
			GD.Print(aimDirection);
		}
		if (Input.IsActionPressed("aim_right")) {
			aimDirection.X += Input.GetActionStrength("aim_right");
			GD.Print(aimDirection);
		}
		if (Input.IsActionPressed("gun")) {
			animatedSprite2D.Animation = "Shooting";
			var b = BulletObject.Instantiate();
			AddChild(b);
			animatedSprite2D.Play();
			GD.Print("Gun Button Pressed");
		}
		if (Input.IsActionJustPressed("sword")) {
			animatedSprite2D.Animation = "Swording";
			animatedSprite2D.Play();
			GD.Print("Sword Pressed");
		}
		#endregion
	}
}
