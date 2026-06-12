using Godot;

public partial class DestructibleObstacle : Area2D
{
	[Export]
	public float Speed {get;set;} = 200f;
	[Export]
	public float Height {get;set;} = 100f;
	[Export]
	public int Damage {get;set;} = 1;
	[Export]
	public int MaxHealth {get;set;} = 3;
	[Export]
	public float KnockbackSpeed {get;set;} = 600f;
	[Export]
	public bool ExplodesOnDestroy {get;set;} = false;
	[Export]
	public int ExplosionDamage {get;set;} = 5;
	[Export]
	public float ExplosionRadius {get;set;} = 100f;
	[Export(PropertyHint.Range, "-1.0,1.0,0.05")]
	public float InputSpeedFactor {get;set;} = 0.5f;
	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	public float WallContactSpeedFactor {get;set;} = 0.1f;

	private int currentHealth;
	private Player player;
	private Tween flashTween;
	private Sprite2D sprite;
	private static PackedScene explosionScene;

	public override void _Ready()
	{
		currentHealth = MaxHealth;
		if (explosionScene == null) {
			explosionScene = GD.Load<PackedScene>("res://assets/objects/Explosion.tscn");
		}
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null && sprite.Texture != null && Height > 0f) {
			float texHeight = sprite.Texture.GetHeight();
			if (texHeight > 0f) {
				sprite.Scale = new Vector2(sprite.Scale.X, Height / texHeight);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (player == null) player = GetParent()?.GetNodeOrNull<Player>("Player");
		float inputY = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
		float wallFactor = (player != null && player.IsTouchingWall) ? WallContactSpeedFactor : 1f;
		float effectiveSpeed = Speed * (1 + inputY * InputSpeedFactor) * wallFactor;
		Position += new Vector2(0, -effectiveSpeed * (float)delta);
		if (GlobalPosition.Y + Height / 2f < 0f) QueueFree();
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player p) {
			HitPlayer(p);
			return;
		}
		if (area is Attack attack) {
			TakeDamage(attack.Damage);
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Player p) HitPlayer(p);
	}

	private void HitPlayer(Player p)
	{
		p.TakeDamage(Damage);
		float dirX = p.GlobalPosition.X < GlobalPosition.X ? -1f : 1f;
		p.ApplyKnockback(new Vector2(dirX, 0f), KnockbackSpeed);
	}

	public void TakeDamage(int amount)
	{
		currentHealth -= amount;
		FlashRed();
		if (currentHealth <= 0) {
			if (ExplodesOnDestroy) Explode();
			QueueFree();
		}
	}

	private void Explode()
	{
		foreach (var node in GetTree().GetNodesInGroup("Enemy")) {
			if (node is Enemy e && e.CurrentHealth > 0
				&& e.GlobalPosition.DistanceTo(GlobalPosition) <= ExplosionRadius) {
				e.TakeDamage(ExplosionDamage, ElementType.NonElemental);
			}
		}
		var p = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (p != null && p.CurrentHealth > 0
			&& p.GlobalPosition.DistanceTo(GlobalPosition) <= ExplosionRadius) {
			p.TakeDamage(ExplosionDamage);
		}
		if (explosionScene != null && Explosion.CanSpawn()) {
			var ex = explosionScene.Instantiate<Explosion>();
			ex.GlobalPosition = GlobalPosition;
			ex.Damage = ExplosionDamage;
			GetParent().AddChild(ex);
		}
	}

	private void FlashRed()
	{
		if (sprite == null) return;
		flashTween?.Kill();
		sprite.Modulate = new Color(1f, 0.3f, 0.3f);
		flashTween = CreateTween();
		flashTween.TweenProperty(sprite, "modulate", Colors.White, 0.2f);
	}
}
