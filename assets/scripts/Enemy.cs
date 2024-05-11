using System.Linq;
using Godot;

public partial class Enemy : Area2D
{
	[Export]
	public EnemyStatBlock Stats {get;set;}
	public int CurrentHealth {get;set;}
	private float GunCoolDown;
	public AnimatedSprite2D animatedSprite2D;
	public Vector2 PostSpawnDestination {get;set;}
	private bool ReachedPostSpawnDestination = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		CurrentHealth = Stats.MaxHealth;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GunCoolDown <= 0) {
			Shoot();
		}
		if (GunCoolDown > 0) {
			GunCoolDown -= (float)delta;
		}
		if (CurrentHealth <= 0) {
			animatedSprite2D.Animation = "Death";
			animatedSprite2D.Play();
		}
	}

	public override void _PhysicsProcess(double delta)
    {
		if (ReachedPostSpawnDestination) {
			var playerLocation = ((GetParent().GetNode("Player") as Node2D).GlobalPosition - GlobalPosition).Normalized();
			switch (Stats.MovementType)
			{
				case MovementType.Stationary:
				break;
				case MovementType.TowardsPlayer:
				var towards = playerLocation * Stats.MovementSpeed * (float)delta;
				Position += towards;
				break;
				case MovementType.AwayFromPlayer:
				var away = -Position.DirectionTo(playerLocation) * Stats.MovementSpeed * (float)delta;
				Position += away;
				break;
				case MovementType.Random:
				
				break;
			}
		}
		else {
			var motion = (PostSpawnDestination - GlobalPosition) * Stats.MovementSpeed * (float)delta;
			Position += motion.Normalized();
			if (GlobalPosition == PostSpawnDestination) {
				GD.Print("destination");
				ReachedPostSpawnDestination = true;
			}
		}
    }

	private void _on_area_entered(Node2D node2D){
		CurrentHealth -= (node2D as Attack).Damage;
	}

	private void _on_animated_sprite_2d_animation_finished() {
		switch (animatedSprite2D.Animation) {
			case "Death": 
			var children = GetParent().GetChildren().Where(child => child.HasMeta("Enemy")).Count();
			GD.Print(children);
			if (children <= 1) {
				GD.Print("No more enemies");
				GetParent<Main>().SpawnEnemyGroup();
			}
			QueueFree();
			break;
			case "Shoot":
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
			break;
			
		}
	}
	private void Shoot() {
		for (int i = 0; i < Stats.Gun.BulletCount; i++) {
			var playerLocation = ((GetParent().GetNode("Player") as Node2D).GlobalPosition - GlobalPosition).Normalized();
			animatedSprite2D.Animation = "Shoot";
			animatedSprite2D.LookAt(playerLocation);
			animatedSprite2D.Play();
			Bullet b = Stats.Gun.BulletType.Instantiate<Bullet>();
			b.Set("Direction", playerLocation);
			b.Set("Damage", Stats.Gun.Damage);
			b.Set("BulletLifetime", Stats.Gun.BulletLifetime);
			b.Position = Position;
			b.Rotation = Position.Angle();
			b.SetCollisionLayerValue(5, true);
			b.SetCollisionMaskValue(2, true);
			GetParent().AddChild(b);
		}
		GunCoolDown = Stats.Gun.FireRate;
	}
}
