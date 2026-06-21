using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Enemy : Area2D
{
	[Export]
	public int MaxHealth {get;set;}
	[Export]
	public int DamageReduction {get;set;}
	[Export]
	public int Armor {get;set;}
	[Export]
	public Gun Gun {get;set;}
	[Export]
	public bool CanMelee {get;set;} = true;
	[Export]
	public MeleeWeapon Melee {get;set;}
	[Export]
	public float MeleeRange {get;set;} = 120f;
	[Export]
	public float MeleeCooldown {get;set;} = 1.5f;
	[Export]
	public MovementType MovementType {get;set;}
	[Export]
	public int SpawnMovementSpeed { get; set; } = 5;
	[Export]
	public bool InstantSpawn { get; set; } = false;
	[Export]
	public float MovementSpeed {get;set;}
	[Export]
	public float MovementLimit {get;set;}
	[Export]
	public float Size {get;set;}
	[Export]
	public Texture2D EnemySprite {get;set;}
	// Sound played once when this enemy dies/explodes.
	[Export]
	public AudioStream DeathSound {get;set;}
	// Spawned over the enemy on death (the scene handles its own random variation).
	// Null = nothing.
	[Export]
	public PackedScene DeathExplosions {get;set;}
	[Export]
	public AttackDirection AttackDirection {get; set;}
	[Export]
	public ItemDrop[] ItemDrops {get;set;}
	[Export]
	public bool IsBoss { get; set; }
	[Export]
	public bool IsCore { get; set; }
	[Export]
	public bool IsLeader { get; set; }
	[Export]
	public LeaderType LeaderType { get; set; }
	[Export]
	public float LeaderBoostPercentage { get; set; } = 25f;
	[Export]
	public bool TeleportMovement {get;set;} = false;
	[Export]
	public float TeleportHesitationTime {get;set;} = 0.5f;
	[Export]
	public bool DropsBombs {get;set;} = false;
	[Export]
	public int BombDamage {get;set;} = 5;
	[Export]
	public float BombRadius {get;set;} = 100f;
	[Export]
	public float BombFuseTime {get;set;} = 2f;
	[Export]
	public float BombCooldown {get;set;} = 5f;
	[Export]
	public int BombMaxCount {get;set;} = 3;
	[Export]
	public bool UsesMines {get;set;} = false;
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
	private Node2D leftWallNode;
	private Node2D rightWallNode;
	private float fallbackRightWallX;
	// Read the wall boundaries live so enemies respect walls that move at runtime
	// (e.g. stage wall-contraction events) rather than a value cached at spawn.
	private float LeftWallX => leftWallNode != null ? leftWallNode.Position.X : 0f;
	private float RightWallX => rightWallNode != null ? rightWallNode.Position.X : fallbackRightWallX;
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
	private float firePatternAngle = 0f; // accumulates for Spiral fire patterns
	private bool telegraphActive = false;
	private const float TelegraphLeadTime = 0.5f;
	public AnimatedSprite2D animatedSprite2D;
	public Vector2 PostSpawnDestination {get;set;}
	private bool ReachedPostSpawnDestination = false;
	private bool spawnComplete = false;
	// When true, the enemy stops firing/meleeing on its own — an external driver
	// (e.g. BossController) is responsible for triggering attacks.
	public bool ExternalAttackControl {get;set;} = false;
	public bool SpawnComplete => spawnComplete;
	public float HealthFraction => scaledMaxHealth > 0 ? (float)CurrentHealth / scaledMaxHealth : 0f;
	private Player player;
	public StatusEffectController StatusEffects = new StatusEffectController();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		ForceOneShotAnimations(animatedSprite2D?.SpriteFrames);
		float scale = ComputeStageScale();
		scaledMaxHealth = Mathf.RoundToInt(this.MaxHealth * scale);
		currentArmor = Mathf.RoundToInt(this.Armor * scale);
		scaledDamage = this.Gun != null ? Mathf.RoundToInt(this.Gun.Damage * scale) : 0;
		scaledDamageReduction = Mathf.RoundToInt(this.DamageReduction * scale);
		CurrentHealth = scaledMaxHealth;
		rng.Randomize();
		if (this.Gun != null) GunCoolDown = TelegraphLeadTime;
		if (this.TeleportMovement) {
			teleportTimer = this.TeleportHesitationTime;
		}
		leftWallNode = GetParent()?.GetNodeOrNull<Node2D>("Left Wall Queue");
		rightWallNode = GetParent()?.GetNodeOrNull<Node2D>("Right Wall Queue");
		fallbackRightWallX = GetViewportRect().Size.X;
		SpawnDestinationIndicator();
	}

	// Aseprite imports these one-shot animations with loop=true, which stops
	// AnimationFinished from ever firing — so Spawn/Fire/Death never transition
	// back to Idle (and the spawn gate would never release). Force them non-looping.
	private static readonly string[] OneShotAnimations = {"Spawn", "Fire", "Shoot", "Death", "Explode"};
	private static void ForceOneShotAnimations(SpriteFrames frames)
	{
		if (frames == null) return;
		foreach (var anim in OneShotAnimations) {
			if (frames.HasAnimation(anim)) frames.SetAnimationLoop(anim, false);
		}
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
		if (this.IsLeader == true && CurrentHealth > 0) QueueRedraw();
		if (armorShellTimer > 0f) {
			armorShellTimer -= (float)delta;
			QueueRedraw();
		}
		if (this.Gun != null && CurrentHealth > 0 && spawnComplete && !ExternalAttackControl) {
			if (!telegraphActive && GunCoolDown > 0f && GunCoolDown <= TelegraphLeadTime) {
				SpawnAttackIndicator(TelegraphLeadTime);
				telegraphActive = true;
			}
			if (GunCoolDown <= 0f) {
				Shoot();
				telegraphActive = false;
			}
			if (GunCoolDown > 0) {
				GunCoolDown -= (float)delta;
			}
		}
		if (meleeCoolDown > 0) {
			meleeCoolDown -= (float)delta;
		}
		if (this.CanMelee && this.Melee != null && meleeCoolDown <= 0 && CurrentHealth > 0 && spawnComplete && !ExternalAttackControl) {
			TrySwingMelee();
		}
		int dotDamage = StatusEffects.Tick((float)delta);
		if (dotDamage > 0) TakeDamage(dotDamage, ElementType.NonElemental);
		if (animatedSprite2D != null) animatedSprite2D.SelfModulate = StatusEffects.GetTint();
		if ((this.DropsBombs || this.UsesMines) && CurrentHealth > 0) {
			activeDeployables.RemoveAll(d => !IsInstanceValid(d));
			bombCooldownRemaining -= (float)delta;
			if (bombCooldownRemaining <= 0f && activeDeployables.Count < this.BombMaxCount) {
				if (this.UsesMines) PlaceMine();
				else PlaceBomb();
				bombCooldownRemaining = this.BombCooldown;
			}
		}
		if (CurrentHealth <= 0 && !isDying) {
			StartDying();
		}
		if (isDying && !deathHandled) {
			deathAnimTimer -= (float)delta;
			if (deathAnimTimer <= 0f) {
				HandleDeathFinished();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentHealth > 0) {
			if (ReachedPostSpawnDestination) {
				if (this.TeleportMovement) {
					teleportTimer -= (float)delta;
					if (teleportTimer <= 1f && teleportTimer > 0f && !isPhasing) {
						ApplyPhasingShader();
						isPhasing = true;
					}
					if (teleportTimer > 0f) return;
					teleportTimer = this.TeleportHesitationTime;
					delta = this.TeleportHesitationTime;
					if (isPhasing) {
						RemovePhasingShader();
						isPhasing = false;
					}
				}
				var playerLocation = ((GetParent().GetNode("Player") as Node2D).GlobalPosition - GlobalPosition).Normalized();
				switch (this.MovementType)
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
						Mathf.Clamp(Position.X, LeftWallX + WallMargin, RightWallX - WallMargin),
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
				if (this.MovementType != MovementType.WallOnly) {
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
						Mathf.Clamp(Position.X, LeftWallX + WallMargin, RightWallX - WallMargin),
						Position.Y
					);
				}
			}
			else {
				var frames = animatedSprite2D?.SpriteFrames;
				if (frames != null && frames.HasAnimation("PreSpawn") && animatedSprite2D.Animation != "PreSpawn") {
					animatedSprite2D.Animation = "PreSpawn";
					animatedSprite2D.Play();
				}
				if (this.InstantSpawn) {
					Position = PostSpawnDestination;
					ReachedPostSpawnDestination = true;
				} else {
					Vector2 toDestination = PostSpawnDestination - Position;
					if (toDestination.Length() <= this.SpawnMovementSpeed) {
						Position = PostSpawnDestination;
						ReachedPostSpawnDestination = true;
					} else {
						Position += toDestination.Normalized() * this.SpawnMovementSpeed;
					}
				}
				if (ReachedPostSpawnDestination) {
					bool hasSpawn = animatedSprite2D != null && frames != null && frames.HasAnimation("Spawn");
					if (hasSpawn && animatedSprite2D.Animation != "Spawn") {
						animatedSprite2D.Animation = "Spawn";
						animatedSprite2D.Play();
					} else {
						if (animatedSprite2D != null && animatedSprite2D.Animation == "PreSpawn" && frames != null && frames.HasAnimation("Idle")) {
							animatedSprite2D.Animation = "Idle";
							animatedSprite2D.Play();
						}
						spawnComplete = true;
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
	private bool isDying = false;
	private bool deathHandled = false;
	private float deathAnimTimer = 0f;
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
		if (finalDamage > 0 && CurrentHealth > 0) Sfx.PlayHit(this); // death sound covers lethal hits
		hasBeenHit = true;
		QueueRedraw();
		if (this.IsCore == true && CurrentHealth <= 0 && !coreDeathTriggered) {
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
				&& e.IsLeader == true && e.LeaderType == type) {
				total += e.LeaderBoostPercentage;
			}
		}
		return total / 100f;
	}

	private float EffectiveMovementSpeed => this.MovementSpeed
		* StatusEffects.GetSpeedMultiplier()
		* (1f + GetLeaderBoost(LeaderType.Speed));

	private void TriggerCoreDeath()
	{
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e != this && e.CurrentHealth > 0) {
				e.CurrentHealth = 0;
				e.StartDying();
			}
		}
	}

	public void StartDying()
	{
		if (isDying) return;
		isDying = true;
		Sfx.Play(this, DeathSound);
		SpawnDeathExplosion();
		PlayDeathAnimation(animatedSprite2D);
		var frames = animatedSprite2D?.SpriteFrames;
		string anim = animatedSprite2D?.Animation;
		if (frames != null && !string.IsNullOrEmpty(anim) && frames.HasAnimation(anim)) {
			int frameCount = frames.GetFrameCount(anim);
			float fps = (float)frames.GetAnimationSpeed(anim);
			if (fps > 0f) deathAnimTimer = frameCount / fps;
		}
		if (deathAnimTimer <= 0f) deathAnimTimer = 0.5f;
	}

	// Spawns the DeathExplosions scene over the enemy (the scene handles its own random
	// variation). Parented to the enemy's parent so it outlives the enemy being freed.
	private void SpawnDeathExplosion()
	{
		if (DeathExplosions == null) return;
		Vector2 pos = GlobalPosition;
		var explosion = DeathExplosions.Instantiate<Node2D>();
		explosion.ZIndex = 20; // over the enemy
		GetParent().AddChild(explosion);
		explosion.GlobalPosition = pos;
	}

	private void HandleDeathFinished()
	{
		if (deathHandled) return;
		deathHandled = true;
		DropItems();
		if (this.IsBoss) {
			(GetParent() as Main)?.OnBossDefeated();
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
		bomb.Damage = this.BombDamage;
		bomb.Radius = this.BombRadius;
		bomb.FuseTime = this.BombFuseTime;
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
		mine.Damage = this.BombDamage;
		mine.Radius = this.BombRadius;
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
		if (this.IsLeader == true && CurrentHealth > 0) DrawLeaderAura();
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
		Color c = this.LeaderType switch {
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
			case "Explode":
			case "Death":
			HandleDeathFinished();
			break;
			case "Shoot":
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
			break;
			case "Spawn":
			spawnComplete = true;
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
			break;
			case "Fire":
			animatedSprite2D.Animation = "Idle";
			animatedSprite2D.Play();
			break;
		}
	}
	private void DropItems() {
		if (this.ItemDrops == null) return;
		foreach (var drop in this.ItemDrops) {
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

	private void SpawnAttackIndicator(float duration) {
		var p = GetParent()?.GetNodeOrNull<Player>("Player");
		if (p == null) return;
		Vector2 dir = (p.GlobalPosition - GlobalPosition).Normalized();
		if (dir == Vector2.Zero) return;
		var ind = new AttackIndicator();
		ind.Anchor = this;
		ind.Duration = duration;
		ind.GlobalPosition = GlobalPosition;
		ind.Rotation = dir.Angle();
		GetParent().AddChild(ind);
	}

	private void TrySwingMelee() {
		if (this.Melee.Attack == null) return;
		var p = GetParent()?.GetNodeOrNull<Player>("Player");
		if (p == null) return;
		Vector2 toPlayer = p.GlobalPosition - GlobalPosition;
		if (toPlayer.Length() > this.MeleeRange) return;
		MeleeAttack a = this.Melee.Attack.Instantiate<MeleeAttack>();
		a.Direction = toPlayer.Normalized();
		a.Damage = Mathf.Max(1, Mathf.RoundToInt(this.Melee.Damage * ComputeStageScale()));
		a.SwingDuration = this.Melee.SwingDuration;
		a.SwingArc = this.Melee.SwingArc;
		a.OffsetDistance = this.Melee.OffsetDistance;
		a.SetCollisionLayerValue(5, true);
		a.SetCollisionMaskValue(2, true);
		AddChild(a);
		PlayAttackAnimation();
		animatedSprite2D?.Play();
		meleeCoolDown = this.MeleeCooldown;
	}

	private static void PlayDeathAnimation(AnimatedSprite2D sprite)
	{
		if (sprite == null) return;
		var frames = sprite.SpriteFrames;
		if (frames != null && frames.HasAnimation("Explode")) {
			sprite.Animation = "Explode";
		} else if (frames != null && frames.HasAnimation("Death")) {
			sprite.Animation = "Death";
		}
		sprite.Play();
	}

	private void PlayAttackAnimation()
	{
		if (animatedSprite2D == null) return;
		var frames = animatedSprite2D.SpriteFrames;
		if (frames != null && frames.HasAnimation("Fire")) {
			animatedSprite2D.Animation = "Fire";
		} else if (frames != null && frames.HasAnimation("Shoot")) {
			animatedSprite2D.Animation = "Shoot";
		}
	}

	private void Shoot() {
		FireGun(this.Gun);
		GunCoolDown = this.Gun.FireRate * StatusEffects.GetFireRateMultiplier() / (1f + GetLeaderBoost(LeaderType.FireRate));
	}

	// Fires a single volley of the given gun aimed at the player, plus an optional
	// angular offset (degrees). Public so a BossController can drive scripted
	// attack patterns with alternate guns/angles. Does not touch GunCoolDown.
	public void FireGun(Gun gun, float aimOffsetDegrees = 0f) {
		if (gun == null || gun.BulletType == null || animatedSprite2D == null) return;
		FirePattern pattern = gun.FirePattern;
		Vector2 baseDir;
		if (pattern != null && !pattern.AimAtPlayer) {
			baseDir = Vector2.Right.Rotated(Mathf.DegToRad(pattern.FixedAngleDegrees));
		} else {
			baseDir = AimAtPlayerDirection();
			if (baseDir == Vector2.Zero) return;
		}
		baseDir = baseDir.Rotated(Mathf.DegToRad(aimOffsetDegrees));
		Sfx.Play(this, gun.FireSound);
		int baseDamage = Mathf.RoundToInt(gun.Damage * ComputeStageScale());
		int boostedDamage = Mathf.RoundToInt(baseDamage * (1f + GetLeaderBoost(LeaderType.Attack)));
		PlayAttackAnimation();
		animatedSprite2D.LookAt(baseDir);
		animatedSprite2D.Play();
		foreach (Vector2 baseDirection in BuildPatternDirections(gun, pattern, baseDir)) {
			Vector2 dir = baseDirection;
			if (gun.BulletSpread > 0f) {
				dir = dir.Rotated(Mathf.DegToRad(rng.RandfRange(0f, gun.BulletSpread)));
			}
			for (int i = 0; i < gun.BulletCount; i++) {
				bool crit = gun.CriticalChance > 0f && rng.Randf() < gun.CriticalChance;
				int dmg = crit ? Mathf.RoundToInt(boostedDamage * gun.CriticalMultiplier) : boostedDamage;
				Bullet b = gun.BulletType.Instantiate<Bullet>();
				b.Set("Direction", dir);
				b.Set("Damage", dmg);
				b.Set("BulletLifetime", gun.BulletLifetime);
				b.Gun = gun;
				if (gun.BulletSpeed > 0) b.BulletSpeed = gun.BulletSpeed;
				b.AuraColor = crit ? new Color(1f, 0.84f, 0.1f, 0.95f) : new Color(1f, 0.3f, 0.3f, 0.8f);
				b.Position = Position;
				b.Rotation = dir.Angle();
				b.SetCollisionLayerValue(5, true);
				b.SetCollisionMaskValue(2, true);
				GetParent().AddChild(b);
			}
		}
	}

	// Builds the base directions for one shot from the gun's FirePattern. With no
	// pattern it falls back to the legacy DirectionalCount/DirectionalAngle fan.
	private List<Vector2> BuildPatternDirections(Gun gun, FirePattern pattern, Vector2 baseDir) {
		var dirs = new List<Vector2>();
		if (pattern == null) {
			int dc = Mathf.Max(1, gun.DirectionalCount);
			float step = Mathf.DegToRad(gun.DirectionalAngle);
			for (int d = 0; d < dc; d++) dirs.Add(baseDir.Rotated(step * d));
			return dirs;
		}
		int count = Mathf.Max(1, pattern.Count);
		switch (pattern.Type) {
			case FirePatternType.Spread:
				if (count == 1) { dirs.Add(baseDir); break; }
				float arc = Mathf.DegToRad(pattern.ArcDegrees);
				float spreadStart = -arc / 2f;
				float spreadStep = arc / (count - 1);
				for (int i = 0; i < count; i++) dirs.Add(baseDir.Rotated(spreadStart + spreadStep * i));
				break;
			case FirePatternType.Ring:
				float ringStep = Mathf.Tau / count;
				for (int i = 0; i < count; i++) dirs.Add(baseDir.Rotated(ringStep * i));
				break;
			case FirePatternType.Spiral:
				// Advance the aim a step each shot so successive shots trace a circle.
				Vector2 spun = baseDir.Rotated(Mathf.DegToRad(firePatternAngle));
				firePatternAngle = (firePatternAngle + pattern.SpinDegrees) % 360f;
				if (count == 1) { dirs.Add(spun); break; }
				float armStep = Mathf.Tau / count;
				for (int i = 0; i < count; i++) dirs.Add(spun.Rotated(armStep * i));
				break;
			default: // Targeted
				dirs.Add(baseDir);
				break;
		}
		return dirs;
	}

	// Direction from this enemy to the player (zero if no player is found).
	public Vector2 AimAtPlayerDirection() {
		var p = GetParent()?.GetNodeOrNull<Node2D>("Player");
		if (p == null) return Vector2.Zero;
		return (p.GlobalPosition - GlobalPosition).Normalized();
	}

	// Shows an attack-warning indicator for the given lead time. Public so bosses
	// can telegraph special attacks before they land.
	public void ShowTelegraph(float duration) => SpawnAttackIndicator(duration);

	// Spawns a minion at this enemy's position that walks out to Position + offset
	// using the normal spawn-animation flow. Returns the spawned enemy.
	public Enemy SummonMinion(PackedScene scene, Vector2 offset) {
		if (scene == null) return null;
		Enemy minion = scene.Instantiate<Enemy>();
		minion.Position = Position;
		minion.PostSpawnDestination = Position + offset;
		GetParent().AddChild(minion);
		return minion;
	}
}
