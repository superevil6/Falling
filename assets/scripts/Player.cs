using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

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
	private bool CanWallKick = false;
	[Export]
	public float WallKickPriorityTime {get;set;} //This is the amount of time before the player will leave the while if pointing away from it.
	private float RemainingWallKickPriorityTime;
	private bool IsDashing = false;
	[Export]
	public float DashDuration {get;set;} = 1;
	private float RemainingDashTime;
	private Vector2 DashDirection;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScreenSize = GetViewportRect().Size;
		CurrentHealth = MaxHealth;
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		GetParent().GetNode<TextureProgressBar>("Health Bar").MaxValue = MaxHealth;
		GetParent().GetNode<TextureProgressBar>("Health Bar").Value = CurrentHealth;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		#region Shooting
		Vector2 aimDirection = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
		if (Input.IsActionPressed("gun") && aimDirection != Vector2.Zero && gunCoolDown <= 0) {
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
		Position = new Vector2(
			x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
			y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
		);
	}
 	public void GetInput()
    {
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (!CanWallKick) {
			Velocity = inputDirection * Speed;
		}
		if (CanWallKick) {
			inputDirection.X = 0;
			Velocity = inputDirection * Speed;
		}
    }

	private void _on_area_2d_area_entered(Node2D node2D){
		GD.Print(node2D);
		if (node2D.Name == "Bullet") {
			CurrentHealth -= (node2D as Attack).Damage;
			GetParent().GetNode<TextureProgressBar>("Health Bar").Value = CurrentHealth;
		}
	}

    public override void _PhysicsProcess(double delta) //TODO Wall kicking variables can probably be removed in favor of checking float time remaining
    {
		if (!IsDashing) {
			GetInput();
			MoveAndSlide();
		}
		if (Input.IsActionJustReleased("Dash")) {
			IsDashing = false;
			CanWallKick = false;
			RemainingDashTime = 0;
			RemainingWallKickPriorityTime = 0;
		}
		if (GetSlideCollisionCount() > 0) {
			if (((Node2D)GetSlideCollision(0).GetCollider()).IsInGroup("Wall")) {
				if (Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized().X >= 0.5f 
				|| Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized().X <= -0.5f){
					RemainingWallKickPriorityTime -= (float)delta;
					if (RemainingWallKickPriorityTime <= 0) {
						GetInput();
						MoveAndSlide();
						CanWallKick = false;
					}
				} else {
					RemainingWallKickPriorityTime = WallKickPriorityTime;
					CanWallKick = true;
				}
				if (Input.IsActionJustPressed("Dash")) {
					RemainingDashTime = DashDuration;
					DashDirection= Input.GetVector("move_left", "move_right", "move_up", "move_down").Normalized();
				}
			}

		}
		if (RemainingDashTime > 0) {
			Velocity = DashDirection.Normalized() * (Speed * 400) * (float)delta;
			MoveAndSlide();
			RemainingDashTime -= (float)delta;
		}
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
			b.SetCollisionMaskValue(3, true);
			GetParent().AddChild(b);
		}
		animatedSprite2D.Play();
		gunCoolDown = Gun.FireRate;
	}

	private void MeleeAttack(Vector2 aimDirection) {
		animatedSprite2D.Animation = "Swording";
		animatedSprite2D.Play();
		MeleeAttack a = Melee.Attack.Instantiate<MeleeAttack>();
		a.Set("Direction", aimDirection.Normalized());
		a.Set("Damage", Melee.Damage);
		a.Set("SwingDuration", Melee.SwingDuration);
		a.Position = new Vector2(aimDirection.X * 150, aimDirection.Y * 150);
		a.Rotation = aimDirection.Angle();
		a.SetCollisionLayerValue(4, true);
		a.SetCollisionMaskValue(5, true);
		AddChild(a);
	}

	private void _on_animated_sprite_2d_animation_finished() {
		switch (animatedSprite2D.Animation) {
			case "Shooting":
			case "Swording":
			animatedSprite2D.Animation = "Falling";
			animatedSprite2D.Play();
			break;
		}
	}
}
