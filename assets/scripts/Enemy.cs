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
	public float SeparationRadius {get;set;} = 60f;
	[Export]
	public float SeparationStrength {get;set;} = 2.0f;
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
	private float meleeCoolDown;
	private bool telegraphActive = false;
	private const float TelegraphLeadTime = 0.5f;
	public AnimatedSprite2D animatedSprite2D;
	public Vector2 PostSpawnDestination {get;set;}
	private bool ReachedPostSpawnDestination = false;
	private Player player;
	public StatusEffectController StatusEffects = new StatusEffectController();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		float scale = ComputeStageScale();
		scaledMaxHealth = Mathf.RoundToInt(Stats.MaxHealth * scale);
		currentArmor = Mathf.RoundToInt(Stats.Armor * scale);
		scaledDamage = Stats.Gun != null ? Mathf.RoundToInt(Stats.Gun.Damage * scale) : 0;
		scaledDamageReduction = Mathf.RoundToInt(Stats.DamageReduction * scale);
		CurrentHealth = scaledMaxHealth;
		rng.Randomize();
		if (Stats.Gun != null) GunCoolDown = TelegraphLeadTime;
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
		if (Stats?.IsLeader == true && CurrentHealth > 0) QueueRedraw();
		if (armorShellTimer > 0f) {
			armorShellTimer -= (float)delta;
			QueueRedraw();
		}
		if (Stats?.Gun != null && CurrentHealth > 0) {
			if (!telegraphActive && GunCoolDown > 0f && GunCoolDown <= TelegraphLeadTime) {
				SpawnAttackIndicator();
				telegraphActive = true;
			}
			if (GunCoolDown <= 0f) {
				Shoot();
				telegraphActive = false;
			}
		}
		if (GunCoolDown > 0) {
			GunCoolDown -= (float)delta;
		}
		if (meleeCoolDown > 0) {
			meleeCoolDown -= (float)delta;
		}
		if (Stats.CanMelee && Stats?.Melee != null && meleeCoolDown <= 0 && CurrentHealth > 0) {
			TrySwingMelee();
		}
		int dotDamage = StatusEffects.Tick((float)delta);
		if (dotDamage > 0) TakeDamage(dotDamage, ElementType.NonElemental);
		if (animatedSprite2D != null) animatedSprite2D.SelfModulate = StatusEffects.GetTint();
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
					Vector2 chaseDir = playerLocation;
					Vector2 separation = GetSeparationVector(SeparationRadius);
					Vector2 blended = (chaseDir + separation * SeparationStrength);
					if (blended != Vector2.Zero) blended = blended.Normalized();
					Position += blended * EffectiveMovementSpeed * (float)delta;
					break;
					case MovementType.AwayFromPlayer:
					var away = -playerLocation * EffectiveMovementSpeed * (float)delta;
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
					Position += randomDirection * EffectiveMovementSpeed * (float)delta;
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
						float squareStep = EffectiveMovementSpeed * (float)delta;
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
						float squareStep = EffectiveMovementSpeed * (float)delta;
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
						float omega = SmallCircleRadius > 0f ? EffectiveMovementSpeed / SmallCircleRadius : 0f;
						currentCircleAngle += omega * (float)delta;
						Position = PostSpawnDestination + new Vector2(Mathf.Cos(currentCircleAngle), Mathf.Sin(currentCircleAngle)) * SmallCircleRadius;
					}
					break;
					case MovementType.LargeCircle:
					{
						float omega = LargeCircleRadius > 0f ? EffectiveMovementSpeed / LargeCircleRadius : 0f;
						currentCircleAngle += omega * (float)delta;
						Position = PostSpawnDestination + new Vector2(Mathf.Cos(currentCircleAngle), Mathf.Sin(currentCircleAngle)) * LargeCircleRadius;
					}
					break;
					case MovementType.VerticalBackAndForth:
					{
						float targetY = PostSpawnDestination.Y + verticalDirection * VerticalAmplitude;
						float dy = targetY - Position.Y;
						float step = EffectiveMovementSpeed * (float)delta;
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
						float step = EffectiveMovementSpeed * (float)delta;
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
						float step = EffectiveMovementSpeed * (float)delta;
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
						var main = GetParent() as Main;
						float driftSpeed = main?.EnemyInputDriftSpeed ?? InputDriftSpeed;
						float driftY = 0f;
						if (player.IsTouchingWall) {
							driftY = WallContactDriftSpeed;
						} else {
							float inputY = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
							float screenH = player.ScreenSize.Y;
							bool pushingTopEdge = player.Position.Y <= screenH * 0.1f && inputY < 0f;
							bool pushingBottomEdge = player.Position.Y >= screenH * 0.9f && inputY > 0f;
							if (pushingTopEdge || pushingBottomEdge) {
								driftY = -inputY * driftSpeed;
							}
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
		int acidStacks = (node2D is Bullet ab) ? (ab.Gun?.AcidRoundsCount ?? 0) : 0;
		TakeDamage(attack.Damage, element, acidStacks);
		if (node2D is Bullet b && b.Gun != null && b.Gun.LifeSteal > 0f) {
			int heal = Mathf.RoundToInt(b.Gun.LifeSteal);
			if (heal > 0) {
				var p = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
				p?.Heal(heal);
			}
		}
	}

	private bool hasBeenHit = false;
	private bool coreDeathTriggered = false;
	private int scaledMaxHealth;
	private int currentArmor;
	private float armorShellTimer = 0f;
	private int scaledDamage;
	private int scaledDamageReduction;
	private float teleportTimer = 0f;
	private bool isPhasing = false;
	private static Shader phasingShader;
	private float bombCooldownRemaining = 0f;
	private List<Node2D> activeDeployables = new List<Node2D>();
	private static PackedScene bombScene;
	private static PackedScene mineScene;

	public void TakeDamage(int damage, ElementType element, int acidStacks = 0)
	{
		float dmg = damage;
		if (element != ElementType.NonElemental) {
			if (element == ElementalWeakness) dmg *= 2f;
			else if (element == ElementalDefense) dmg *= 0.5f;
		}
		int boostedReduction = Mathf.RoundToInt(scaledDamageReduction * (1f + GetLeaderBoost(LeaderType.Defense)));
		int finalDamage = Mathf.Max(0, Mathf.RoundToInt(dmg) - boostedReduction);
		if (currentArmor > 0) {
			finalDamage = Mathf.CeilToInt(finalDamage * 0.5f);
			int armorDmg = finalDamage * (1 + acidStacks);
			currentArmor = Mathf.Max(0, currentArmor - armorDmg);
			armorShellTimer = 0.5f;
		}
		CurrentHealth -= finalDamage;
		if (finalDamage > 0) FloatingDamageText.Spawn(this, GlobalPosition, finalDamage, FloatingDamageText.ElementColor(element, new Color(1f, 0.95f, 0.85f)));
		if (CurrentHealth > 0) FlashRed();
		hasBeenHit = true;
		QueueRedraw();
		if (Stats?.IsCore == true && CurrentHealth <= 0 && !coreDeathTriggered) {
			coreDeathTriggered = true;
			TriggerCoreDeath();
		}
	}

	private Vector2 GetSeparationVector(float radius)
	{
		if (radius <= 0f) return Vector2.Zero;
		Vector2 sep = Vector2.Zero;
		int count = 0;
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy other && other != this && other.CurrentHealth > 0) {
				float d = GlobalPosition.DistanceTo(other.GlobalPosition);
				if (d > 0f && d < radius) {
					Vector2 away = (GlobalPosition - other.GlobalPosition) / d;
					sep += away * (1f - d / radius);
					count++;
				}
			}
		}
		return count > 0 ? sep / count : Vector2.Zero;
	}

	private float GetLeaderBoost(LeaderType type)
	{
		float total = 0f;
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e != this && e.CurrentHealth > 0
				&& e.Stats?.IsLeader == true && e.Stats.LeaderType == type) {
				total += e.Stats.LeaderBoostPercentage;
			}
		}
		return total / 100f;
	}

	private float EffectiveMovementSpeed => Stats.MovementSpeed
		* StatusEffects.GetSpeedMultiplier()
		* (1f + GetLeaderBoost(LeaderType.Speed));

	private void TriggerCoreDeath()
	{
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e != this && e.CurrentHealth > 0) {
				e.CurrentHealth = 0;
				if (e.animatedSprite2D != null) {
					e.animatedSprite2D.Animation = "Death";
					e.animatedSprite2D.Play();
				}
			}
		}
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
		if (Stats?.IsLeader == true && CurrentHealth > 0) DrawLeaderAura();
		if (currentArmor > 0 && armorShellTimer > 0f && CurrentHealth > 0) {
			DrawCircle(Vector2.Zero, 100f, new Color(0.7f, 0.85f, 1f, 0.3f));
		}
		if (!hasBeenHit || CurrentHealth <= 0) return;
		var p = GetParent()?.GetNodeOrNull<Player>("Player");
		if (p == null || !p.HasSeeEnemyHealth) return;
		float barWidth = 72f;
		float barHeight = 10f;
		float yOffset = -42f;
		float maxHealth = Mathf.Max(1, scaledMaxHealth);
		float healthRatio = Mathf.Clamp((float)CurrentHealth / maxHealth, 0f, 1f);
		Vector2 barPos = new Vector2(-barWidth / 2f, yOffset);
		DrawRect(new Rect2(barPos, new Vector2(barWidth, barHeight)), new Color(0.1f, 0.1f, 0.1f, 0.85f));
		DrawRect(new Rect2(barPos, new Vector2(barWidth * healthRatio, barHeight)), new Color(0.9f, 0.2f, 0.2f));
		DrawRect(new Rect2(barPos, new Vector2(barWidth, barHeight)), Colors.Black, false, 1.0f);
	}

	private void DrawLeaderAura()
	{
		Color c = Stats.LeaderType switch {
			LeaderType.Attack => new Color(1f, 0.2f, 0.2f),
			LeaderType.Defense => new Color(0.3f, 0.5f, 1f),
			LeaderType.FireRate => new Color(1f, 0.9f, 0.2f),
			LeaderType.Speed => new Color(0.3f, 1f, 0.4f),
			_ => Colors.White,
		};
		float pulse = 0.85f + 0.15f * Mathf.Sin(Time.GetTicksMsec() / 200f);
		float r = 55f * pulse;
		DrawCircle(Vector2.Zero, r * 1.6f, new Color(c.R, c.G, c.B, 0.12f));
		DrawCircle(Vector2.Zero, r * 1.1f, new Color(c.R, c.G, c.B, 0.22f));
		DrawCircle(Vector2.Zero, r * 0.7f, new Color(c.R, c.G, c.B, 0.32f));
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
			int othersAlive = GetParent().GetChildren()
				.Where(child => child != this && child is Enemy other && other.CurrentHealth > 0)
				.Count();
			if (othersAlive == 0) {
				GetParent<Main>().CallDeferred(nameof(Main.SpawnEnemyGroup));
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

	private void SpawnAttackIndicator() {
		var p = GetParent()?.GetNodeOrNull<Player>("Player");
		if (p == null) return;
		Vector2 dir = (p.GlobalPosition - GlobalPosition).Normalized();
		if (dir == Vector2.Zero) return;
		var ind = new AttackIndicator();
		ind.Anchor = this;
		ind.Duration = TelegraphLeadTime;
		ind.GlobalPosition = GlobalPosition;
		ind.Rotation = dir.Angle();
		GetParent().AddChild(ind);
	}

	private void TrySwingMelee() {
		if (Stats.Melee.Attack == null) return;
		var p = GetParent()?.GetNodeOrNull<Player>("Player");
		if (p == null) return;
		Vector2 toPlayer = p.GlobalPosition - GlobalPosition;
		if (toPlayer.Length() > Stats.MeleeRange) return;
		MeleeAttack a = Stats.Melee.Attack.Instantiate<MeleeAttack>();
		a.Direction = toPlayer.Normalized();
		a.Damage = Mathf.Max(1, Mathf.RoundToInt(Stats.Melee.Damage * ComputeStageScale()));
		a.SwingDuration = Stats.Melee.SwingDuration;
		a.SwingArc = Stats.Melee.SwingArc;
		a.OffsetDistance = Stats.Melee.OffsetDistance;
		a.SetCollisionLayerValue(5, true);
		a.SetCollisionMaskValue(2, true);
		AddChild(a);
		meleeCoolDown = Stats.MeleeCooldown;
	}

	private void Shoot() {
		int boostedDamage = Mathf.RoundToInt(scaledDamage * (1f + GetLeaderBoost(LeaderType.Attack)));
		var playerLocation = ((GetParent().GetNode("Player") as Node2D).GlobalPosition - GlobalPosition).Normalized();
		animatedSprite2D.Animation = "Shoot";
		animatedSprite2D.LookAt(playerLocation);
		animatedSprite2D.Play();
		int dirCount = Mathf.Max(1, Stats.Gun.DirectionalCount);
		float dirStep = Mathf.DegToRad(Stats.Gun.DirectionalAngle);
		for (int d = 0; d < dirCount; d++) {
			Vector2 dir = playerLocation.Rotated(dirStep * d);
			for (int i = 0; i < Stats.Gun.BulletCount; i++) {
				bool crit = Stats.Gun.CriticalChance > 0f && rng.Randf() < Stats.Gun.CriticalChance;
				int dmg = crit ? Mathf.RoundToInt(boostedDamage * Stats.Gun.CriticalMultiplier) : boostedDamage;
				Bullet b = Stats.Gun.BulletType.Instantiate<Bullet>();
				b.Set("Direction", dir);
				b.Set("Damage", dmg);
				b.Set("BulletLifetime", Stats.Gun.BulletLifetime);
				b.Gun = Stats.Gun;
				if (Stats.Gun.BulletSpeed > 0) b.BulletSpeed = Stats.Gun.BulletSpeed;
				b.AuraColor = crit ? new Color(1f, 0.84f, 0.1f, 0.95f) : new Color(1f, 0.3f, 0.3f, 0.8f);
				b.Position = Position;
				b.Rotation = dir.Angle();
				b.SetCollisionLayerValue(5, true);
				b.SetCollisionMaskValue(2, true);
				GetParent().AddChild(b);
			}
		}
		GunCoolDown = Stats.Gun.FireRate * StatusEffects.GetFireRateMultiplier() / (1f + GetLeaderBoost(LeaderType.FireRate));
	}
}
