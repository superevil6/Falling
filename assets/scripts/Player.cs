using Godot;
using System;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	[Export]
	public int MaxHealth {get;set;}
	public int CurrentHealth;
	private float gunCoolDown;
	[Export]
	public int Speed { get;set;} = 400;
	[Export]
	public Gun Gun {get;set;}
	[Export]
	public MeleeWeapon Melee {get;set;}
	// public List<Area2D> Bullets = new List<Area2D>();
	public Vector2 ScreenSize;
	public AnimatedSprite2D animatedSprite2D;
	private RandomNumberGenerator rng = new RandomNumberGenerator();
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
		#region Shooting
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
		if (Input.IsActionPressed("gun") && aimDirection.Normalized() != Vector2.Zero && gunCoolDown <= 0) {
			Shoot(aimDirection);
		}
		if (gunCoolDown > 0) {
			gunCoolDown -= (float)delta;
		}
		#endregion
		#region Sword
		if (Input.IsActionJustPressed("sword") && aimDirection.Normalized() != Vector2.Zero) {
			MeleeAttack(aimDirection);
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

	private void Shoot(Vector2 aimDirection) {
		animatedSprite2D.Animation = "Shooting";
		for (int i = 0; i < Gun.BulletCount; i++) {
			Bullet b = Gun.BulletType.Instantiate<Bullet>();
			var randomX = rng.RandfRange(-Gun.BulletSpread, Gun.BulletSpread);
			var randomY = rng.RandfRange(-Gun.BulletSpread, Gun.BulletSpread);
			b.Set("Direction", new Vector2(aimDirection.X + randomX, aimDirection.Y + randomY));
			b.Set("Damage", Gun.Damage);
			b.Set("BulletLifetime", Gun.BulletLifetime);
			b.Position = Position;
			b.Rotation = aimDirection.Angle();
			b.SetCollisionLayerValue(4, true);
			GetParent().AddChild(b);
		}
		animatedSprite2D.Play();
		gunCoolDown = Gun.FireRate;
	}

	private void MeleeAttack(Vector2 aimDirection) {
		animatedSprite2D.Animation = "Swording";
		animatedSprite2D.Play();
		MeleeAttack a = Melee.Attack.Instantiate<MeleeAttack>();
		a.Set("Direction", aimDirection);
		a.Set("SwingDuration", Melee.SwingDuration);
		a.Position = new Vector2(aimDirection.X + 100, aimDirection.Y + 100);
		a.Rotation = aimDirection.Angle();
		AddChild(a);
	}
}
