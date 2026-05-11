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
	public float RandomDirectionInterval {get;set;} = 1f;
	private Vector2 randomDirection = Vector2.Zero;
	private float randomDirectionTimer = 0f;
	private RandomNumberGenerator rng = new RandomNumberGenerator();
	[Export]
	public float SmallSquareSize {get;set;} = 50f;
	[Export]
	public float LargeSquareSize {get;set;} = 150f;
	[Export]
	public float SmallCircleRadius {get;set;} = 50f;
	[Export]
	public float LargeCircleRadius {get;set;} = 150f;
	[Export]
	public float VerticalAmplitude {get;set;} = 100f;
	[Export]
	public float HorizontalAmplitude {get;set;} = 100f;
	private int currentSquareCorner = 0;
	private float currentCircleAngle = 0f;
	private int verticalDirection = 1;
	private int horizontalDirection = 1;
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
		rng.Randomize();
		SpawnDestinationIndicator();
	}

	private void SpawnDestinationIndicator()
	{
		var scene = GD.Load<PackedScene>("res://assets/objects/SpawnIndicator.tscn");
		if (scene == null) return;
		var indicator = scene.Instantiate<Node2D>();
		GetParent().AddChild(indicator);
		indicator.GlobalPosition = PostSpawnDestination;
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
					var away = -playerLocation * Stats.MovementSpeed * (float)delta;
					Position += away;
					break;
					case MovementType.Random:
					randomDirectionTimer -= (float)delta;
					if (randomDirectionTimer <= 0 || randomDirection == Vector2.Zero) {
						float angle = rng.RandfRange(0f, Mathf.Pi * 2f);
						randomDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
						randomDirectionTimer = RandomDirectionInterval;
					}
					Position += randomDirection * Stats.MovementSpeed * (float)delta;
					break;
					case MovementType.SmallSquare:
					{
						Vector2 cornerOffset = currentSquareCorner switch {
							0 => new Vector2(SmallSquareSize, SmallSquareSize),
							1 => new Vector2(-SmallSquareSize, SmallSquareSize),
							2 => new Vector2(-SmallSquareSize, -SmallSquareSize),
							3 => new Vector2(SmallSquareSize, -SmallSquareSize),
							_ => Vector2.Zero,
						};
						Vector2 squareTarget = PostSpawnDestination + cornerOffset;
						Vector2 toSquareTarget = squareTarget - Position;
						float squareStep = Stats.MovementSpeed * (float)delta;
						if (toSquareTarget.Length() <= squareStep) {
							Position = squareTarget;
							currentSquareCorner = (currentSquareCorner + 1) % 4;
						} else {
							Position += toSquareTarget.Normalized() * squareStep;
						}
					}
					break;
					case MovementType.LargeSquare:
					{
						Vector2 cornerOffset = currentSquareCorner switch {
							0 => new Vector2(LargeSquareSize, LargeSquareSize),
							1 => new Vector2(-LargeSquareSize, LargeSquareSize),
							2 => new Vector2(-LargeSquareSize, -LargeSquareSize),
							3 => new Vector2(LargeSquareSize, -LargeSquareSize),
							_ => Vector2.Zero,
						};
						Vector2 squareTarget = PostSpawnDestination + cornerOffset;
						Vector2 toSquareTarget = squareTarget - Position;
						float squareStep = Stats.MovementSpeed * (float)delta;
						if (toSquareTarget.Length() <= squareStep) {
							Position = squareTarget;
							currentSquareCorner = (currentSquareCorner + 1) % 4;
						} else {
							Position += toSquareTarget.Normalized() * squareStep;
						}
					}
					break;
					case MovementType.SmallCircle:
					{
						float omega = SmallCircleRadius > 0f ? Stats.MovementSpeed / SmallCircleRadius : 0f;
						currentCircleAngle += omega * (float)delta;
						Position = PostSpawnDestination + new Vector2(Mathf.Cos(currentCircleAngle), Mathf.Sin(currentCircleAngle)) * SmallCircleRadius;
					}
					break;
					case MovementType.LargeCircle:
					{
						float omega = LargeCircleRadius > 0f ? Stats.MovementSpeed / LargeCircleRadius : 0f;
						currentCircleAngle += omega * (float)delta;
						Position = PostSpawnDestination + new Vector2(Mathf.Cos(currentCircleAngle), Mathf.Sin(currentCircleAngle)) * LargeCircleRadius;
					}
					break;
					case MovementType.VerticalBackAndForth:
					{
						float targetY = PostSpawnDestination.Y + verticalDirection * VerticalAmplitude;
						float dy = targetY - Position.Y;
						float step = Stats.MovementSpeed * (float)delta;
						if (Mathf.Abs(dy) <= step) {
							Position = new Vector2(Position.X, targetY);
							verticalDirection = -verticalDirection;
						} else {
							Position += new Vector2(0, Mathf.Sign(dy) * step);
						}
					}
					break;
					case MovementType.HorizontalBackAndForth:
					{
						float targetX = PostSpawnDestination.X + horizontalDirection * HorizontalAmplitude;
						float dx = targetX - Position.X;
						float step = Stats.MovementSpeed * (float)delta;
						if (Mathf.Abs(dx) <= step) {
							Position = new Vector2(targetX, Position.Y);
							horizontalDirection = -horizontalDirection;
						} else {
							Position += new Vector2(Mathf.Sign(dx) * step, 0);
						}
					}
					break;
					case MovementType.DiagonalBackAndForth:
					{
						float step = Stats.MovementSpeed * (float)delta;
						float targetX = PostSpawnDestination.X + horizontalDirection * HorizontalAmplitude;
						float dx = targetX - Position.X;
						float newX;
						if (Mathf.Abs(dx) <= step) {
							newX = targetX;
							horizontalDirection = -horizontalDirection;
						} else {
							newX = Position.X + Mathf.Sign(dx) * step;
						}
						float targetY = PostSpawnDestination.Y + verticalDirection * VerticalAmplitude;
						float dy = targetY - Position.Y;
						float newY;
						if (Mathf.Abs(dy) <= step) {
							newY = targetY;
							verticalDirection = -verticalDirection;
						} else {
							newY = Position.Y + Mathf.Sign(dy) * step;
						}
						Position = new Vector2(newX, newY);
					}
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
				if (Stats.InstantSpawn) {
					Position = PostSpawnDestination;
					ReachedPostSpawnDestination = true;
				} else {
					Vector2 toDestination = PostSpawnDestination - Position;
					if (toDestination.Length() <= Stats.SpawnMovementSpeed) {
						Position = PostSpawnDestination;
						ReachedPostSpawnDestination = true;
					} else {
						Position += toDestination.Normalized() * Stats.SpawnMovementSpeed;
					}
				}
			}
		}
    }

	private void _on_area_entered(Node2D node2D){
		var attack = node2D as Attack;
		if (attack == null) return;
		var element = (node2D is Bullet bullet) ? bullet.Element : ElementType.NonElemental;
		TakeDamage(attack.Damage, element);
	}

	public void TakeDamage(int damage, ElementType element)
	{
		float dmg = damage;
		if (element != ElementType.NonElemental) {
			if (element == ElementalWeakness) dmg *= 2f;
			else if (element == ElementalDefense) dmg *= 0.5f;
		}
		CurrentHealth -= Mathf.RoundToInt(dmg);
		if (CurrentHealth > 0) FlashRed();
	}

	private Tween flashTween;

	private void FlashRed()
	{
		if (animatedSprite2D == null) return;
		flashTween?.Kill();
		animatedSprite2D.Modulate = new Color(1f, 0.3f, 0.3f);
		flashTween = CreateTween();
		flashTween.TweenProperty(animatedSprite2D, "modulate", Colors.White, 0.2f);
	}

	private void _on_animated_sprite_2d_animation_finished() {
		switch (animatedSprite2D.Animation) {
			case "Death":
			DropItems();
			if (Stats != null && Stats.IsBoss) {
				var menu = GetParent()?.GetNodeOrNull<SelectionMenu>("Selection Menu");
				menu?.Open();
			}
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
			b.Rotation = playerLocation.Angle();
			b.SetCollisionLayerValue(5, true);
			b.SetCollisionMaskValue(2, true);
			GetParent().AddChild(b);
		}
		GunCoolDown = Stats.Gun.FireRate;
	}
}
