using System;
using System.Linq;
using Godot;

public partial class Enemy : Area2D
{
	[Export]
	public EnemyStatBlock Stats {get;set;}
	[Export]
	public float InputDriftSpeed {get;set;} = 50f;
	[Export]
	public float WallContactDriftSpeed {get;set;} = 300f;
	[Export]
	public ElementType ElementalDefense { get; set; }
	[Export]
	public ElementType ElementalWeakness { get; set; }
	[Export]
	public int CurrentHealth {get;set;}
	private float GunCoolDown;
	public AnimatedSprite2D animatedSprite2D;
	public Vector2 PostSpawnDestination {get;set;}
	private bool ReachedPostSpawnDestination = false;
	private Player player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		CurrentHealth = Stats.MaxHealth;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GunCoolDown <= 0 && CurrentHealth > 0) {
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
		if (CurrentHealth > 0) {
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
				if (player == null) player = GetParent()?.GetNodeOrNull<Player>("Player");
				if (player != null) {
					float driftY;
					if (player.IsTouchingWall) {
						driftY = WallContactDriftSpeed;
					} else {
						float inputY = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
						driftY = -inputY * InputDriftSpeed;
					}
					Position += new Vector2(0, driftY * (float)delta);
				}
			}
			else {
				if (Stats.InstantSpawn)
				{
					Position = PostSpawnDestination;
					ReachedPostSpawnDestination = true;
				}
				var motion = (PostSpawnDestination - Position) * Stats.MovementSpeed * (float)delta;
				Position += motion.Normalized() * Stats.SpawnMovementSpeed;
				if (Math.Abs(GlobalPosition.X - PostSpawnDestination.X) > 0.000005 && Math.Abs(GlobalPosition.Y - PostSpawnDestination.Y) > 0.000005) {
					GD.Print("arrived");
					ReachedPostSpawnDestination = true;
				}
			}
		}
    }

	private void _on_area_entered(Node2D node2D){
		var attack = node2D as Attack;
		if (attack == null) return;
		float damage = attack.Damage;
		if (node2D is Bullet bullet && bullet.Element != ElementType.NonElemental) {
			if (bullet.Element == ElementalWeakness) damage *= 2f;
			else if (bullet.Element == ElementalDefense) damage *= 0.5f;
		}
		CurrentHealth -= Mathf.RoundToInt(damage);
	}

	private void _on_animated_sprite_2d_animation_finished() {
		switch (animatedSprite2D.Animation) {
			case "Death":
			DropItems();
			var children = GetParent().GetChildren().Where(child => child.IsInGroup("Enemy")).Count();
			GD.Print(GetParent().GetChildren().Where(child => child.IsInGroup("Enemy")).Count());
			if (children <= 1) {
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
	private void DropItems() {
		if (Stats?.ItemDrops == null) return;
		foreach (var drop in Stats.ItemDrops) {
			if (drop?.Item == null) continue;
			if (GD.Randf() < drop.Chance) {
				var item = drop.Item.Instantiate<Node2D>();
				item.GlobalPosition = GlobalPosition;
				GetParent().AddChild(item);
			}
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
			b.Gun = Stats.Gun;
			b.Position = Position;
			b.Rotation = Position.Angle();
			b.SetCollisionLayerValue(5, true);
			b.SetCollisionMaskValue(2, true);
			GetParent().AddChild(b);
		}
		GunCoolDown = Stats.Gun.FireRate;
	}
}
