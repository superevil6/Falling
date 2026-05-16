using System;
using System.Collections.Generic;
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
	[Export]
	public float WallMargin {get;set;} = 30f;
	[Export]
	public float DropSpreadRadius {get;set;} = 25f;
	private float leftWallX = 0f;
	private float rightWallX = 0f;
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
		float scale = ComputeStageScale();
		scaledMaxHealth = Mathf.RoundToInt(Stats.MaxHealth * scale);
		scaledDamage = Stats.Gun != null ? Mathf.RoundToInt(Stats.Gun.Damage * scale) : 0;
		scaledDamageReduction = Mathf.RoundToInt(Stats.DamageReduction * scale);
		CurrentHealth = scaledMaxHealth;
		rng.Randomize();
		if (Stats.TeleportMovement) {
			teleportTimer = Stats.TeleportHesitationTime;
		}
		var leftQ = GetParent()?.GetNodeOrNull<Node2D>("Left Wall Queue");
		var rightQ = GetParent()?.GetNodeOrNull<Node2D>("Right Wall Queue");
		leftWallX = leftQ != null ? leftQ.Position.X : 0f;
		rightWallX = rightQ != null ? rightQ.Position.X : GetViewportRect().Size.X;
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
		if (Stats != null && (Stats.DropsBombs || Stats.UsesMines) && CurrentHealth > 0) {
			activeDeployables.RemoveAll(d => !IsInstanceValid(d));
			bombCooldownRemaining -= (float)delta;
			if (bombCooldownRemaining <= 0f && activeDeployables.Count < Stats.BombMaxCount) {
				if (Stats.UsesMines) PlaceMine();
				else PlaceBomb();
				bombCooldownRemaining = Stats.BombCooldown;
			}
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
				if (Stats.TeleportMovement) {
					teleportTimer -= (float)delta;
					if (teleportTimer <= 1f && teleportTimer > 0f && !isPhasing) {
						ApplyPhasingShader();
						isPhasing = true;
					}
					if (teleportTimer > 0f) return;
					teleportTimer = Stats.TeleportHesitationTime;
					delta = Stats.TeleportHesitationTime;
					if (isPhasing) {
						RemovePhasingShader();
						isPhasing = false;
					}
				}
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
					var screen = GetViewportRect().Size;
					Position = new Vector2(
						Mathf.Clamp(Position.X, leftWallX + WallMargin, rightWallX - WallMargin),
						Mathf.Clamp(Position.Y, 0f, screen.Y)
					);
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
					case MovementType.WallOnly:
					{
						var queue = GetParent()?.GetNodeOrNull<WallQueue>("Left Wall Queue");
						if (queue != null) {
							Position += queue.GetCurrentScrollMovement(delta);
						}
						if (GlobalPosition.Y < -50f) {
							QueueFree();
							return;
						}
					}
					break;
				}
				if (Stats.MovementType != MovementType.WallOnly) {
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
					Position = new Vector2(
						Mathf.Clamp(Position.X, leftWallX + WallMargin, rightWallX - WallMargin),
						Position.Y
					);
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

	private bool hasBeenHit = false;
	private int scaledMaxHealth;
	private int scaledDamage;
	private int scaledDamageReduction;
	private float teleportTimer = 0f;
	private bool isPhasing = false;
	private static Shader phasingShader;
	private float bombCooldownRemaining = 0f;
	private List<Node2D> activeDeployables = new List<Node2D>();
	private static PackedScene bombScene;
	private static PackedScene mineScene;

	public void TakeDamage(int damage, ElementType element)
	{
		float dmg = damage;
		if (element != ElementType.NonElemental) {
			if (element == ElementalWeakness) dmg *= 2f;
			else if (element == ElementalDefense) dmg *= 0.5f;
		}
		int finalDamage = Mathf.Max(0, Mathf.RoundToInt(dmg) - scaledDamageReduction);
		CurrentHealth -= finalDamage;
		if (CurrentHealth > 0) FlashRed();
		hasBeenHit = true;
		QueueRedraw();
	}

	private float ComputeStageScale()
	{
		var main = GetParent() as Main;
		int stage = main?.CurrentStage ?? 0;
		return 1f + stage * 0.1f;
	}

	private void PlaceBomb()
	{
		if (bombScene == null) {
			bombScene = GD.Load<PackedScene>("res://assets/objects/Bomb.tscn");
		}
		if (bombScene == null) return;
		var bomb = bombScene.Instantiate<Bomb>();
		bomb.GlobalPosition = GlobalPosition;
		bomb.Damage = Stats.BombDamage;
		bomb.Radius = Stats.BombRadius;
		bomb.FuseTime = Stats.BombFuseTime;
		bomb.TargetsEnemy = false;
		bomb.TargetsPlayer = true;
		GetParent().AddChild(bomb);
		activeDeployables.Add(bomb);
	}

	private void PlaceMine()
	{
		if (mineScene == null) {
			mineScene = GD.Load<PackedScene>("res://assets/objects/Mine.tscn");
		}
		if (mineScene == null) return;
		var mine = mineScene.Instantiate<Mine>();
		mine.GlobalPosition = GlobalPosition;
		mine.Damage = Stats.BombDamage;
		mine.Radius = Stats.BombRadius;
		mine.TargetsEnemy = false;
		mine.TargetsPlayer = true;
		GetParent().AddChild(mine);
		activeDeployables.Add(mine);
	}

	private void ApplyPhasingShader()
	{
		if (animatedSprite2D == null) return;
		if (phasingShader == null) {
			phasingShader = GD.Load<Shader>("res://assets/shaders/Phasing.gdshader");
		}
		var mat = new ShaderMaterial();
		mat.Shader = phasingShader;
		animatedSprite2D.Material = mat;
	}

	private void RemovePhasingShader()
	{
		if (animatedSprite2D != null) animatedSprite2D.Material = null;
	}

	public override void _Draw()
	{
		if (!hasBeenHit || CurrentHealth <= 0) return;
		var p = GetParent()?.GetNodeOrNull<Player>("Player");
		if (p == null || !p.HasSeeEnemyHealth) return;
		float barWidth = 40f;
		float barHeight = 6f;
		float yOffset = -30f;
		float maxHealth = Mathf.Max(1, scaledMaxHealth);
		float healthRatio = Mathf.Clamp((float)CurrentHealth / maxHealth, 0f, 1f);
		Vector2 barPos = new Vector2(-barWidth / 2f, yOffset);
		DrawRect(new Rect2(barPos, new Vector2(barWidth, barHeight)), new Color(0.1f, 0.1f, 0.1f, 0.85f));
		DrawRect(new Rect2(barPos, new Vector2(barWidth * healthRatio, barHeight)), new Color(0.9f, 0.2f, 0.2f));
		DrawRect(new Rect2(barPos, new Vector2(barWidth, barHeight)), Colors.Black, false, 1.0f);
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
				float angle = rng.RandfRange(0f, Mathf.Pi * 2f);
				float radius = rng.RandfRange(0f, DropSpreadRadius);
				Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
				item.GlobalPosition = GlobalPosition + offset;
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
			b.Set("Damage", scaledDamage);
			b.Set("BulletLifetime", Stats.Gun.BulletLifetime);
			b.Gun = Stats.Gun;
			if (Stats.Gun.BulletSpeed > 0) b.BulletSpeed = Stats.Gun.BulletSpeed;
			if (Stats.Gun.BulletSpriteFrames != null) {
				var bSprite = b.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
				if (bSprite != null) bSprite.SpriteFrames = Stats.Gun.BulletSpriteFrames;
			}
			b.Position = Position;
			b.Rotation = playerLocation.Angle();
			b.SetCollisionLayerValue(5, true);
			b.SetCollisionMaskValue(2, true);
			GetParent().AddChild(b);
		}
		GunCoolDown = Stats.Gun.FireRate;
	}
}
