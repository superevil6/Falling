using Godot;

public partial class Enemy : Area2D
{
	[Export]
	public EnemyStatBlock Stats {get;set;}
	public int CurrentHealth {get;set;}
	private float GunCoolDown;
	public AnimatedSprite2D animatedSprite2D;

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
			if (!animatedSprite2D.IsPlaying()) {
				QueueFree();
			}
		}
	}

	private void _on_area_entered(Node2D node2D){
		CurrentHealth -= (node2D as Attack).Damage;
		GD.Print(CurrentHealth);
	}

	private void Shoot() {
		for (int i = 0; i < Stats.Gun.BulletCount; i++) {
			animatedSprite2D.Animation = "Shoot";
			animatedSprite2D.Play();
			Bullet b = Stats.Gun.BulletType.Instantiate<Bullet>();
			b.Set("Direction", ((GetParent().GetNode("Player") as Node2D).GlobalPosition - GlobalPosition).Normalized());
			b.Set("Damage", Stats.Gun.Damage);
			b.Set("BulletLifetime", Stats.Gun.BulletLifetime);
			b.Position = Position;
			b.Rotation = Position.Angle();
			b.SetCollisionLayerValue(5, true);
			GetParent().AddChild(b);
		}
		GunCoolDown = Stats.Gun.FireRate;
	}
}
