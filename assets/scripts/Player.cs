using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export]
	public int MaxHealth {get;set;}
	public int CurrentHealth;
	[Export]
	public int Speed { get;set;} = 400;
	[Export]
	public PackedScene BulletObject {get;set;}
	[Export]
	public Weapon Weapon {get;set;}
	// public List<Area2D> Bullets = new List<Area2D>();
	public Vector2 ScreenSize;
	public AnimatedSprite2D animatedSprite2D;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		CurrentHealth = MaxHealth;
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		#region Attacks
		Vector2 aimDirection = Vector2.Zero;
		if (Input.IsActionPressed("aim_up")) {
			aimDirection.Y -= Input.GetActionStrength("aim_up");
		}
		if (Input.IsActionPressed("aim_down")) {
			aimDirection.Y += Input.GetActionStrength("aim_down");
		}
		if (Input.IsActionPressed("aim_left")) {
			aimDirection.X -= Input.GetActionStrength("aim_left");
		}
		if (Input.IsActionPressed("aim_right")) {
			aimDirection.X += Input.GetActionStrength("aim_right");
		}
		if (Input.IsActionJustPressed("gun") && aimDirection.Normalized() != Vector2.Zero) {
			animatedSprite2D.Animation = "Shooting";
			var b = BulletObject.Instantiate();
			b.Set("direction", aimDirection);
			AddChild(b);
			animatedSprite2D.Play();
		}
		if (Input.IsActionJustPressed("sword")) {
			animatedSprite2D.Animation = "Swording";
			animatedSprite2D.Play();
		}
		#endregion
	}
 	public void GetInput()
    {

        Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = inputDirection * Speed;
    }

    public override void _PhysicsProcess(double delta)
    {
        GetInput();
        MoveAndSlide();
    }
}
