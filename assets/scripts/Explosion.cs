using Godot;

public partial class Explosion : Area2D
{
	public int Damage {get;set;}
	[Export]
	public float Durration {get;set;}
	[Export]
	public float Radius {get;set;} = 80f;
	[Export]
	public float BaseRadius {get;set;} = 80f;
	[Export]
	public SpriteFrames[] ExplosionAnimations {get;set;}
	AnimatedSprite2D animatedSprite2D;
	private float elapsedTime = 0f;
	private float animationDuration = 0f;

	private static RandomNumberGenerator rng;
	static Explosion()
	{
		rng = new RandomNumberGenerator();
		rng.Randomize();
	}

	public static int MaxActive = 15;
	private static int activeCount = 0;
	public static bool CanSpawn() => activeCount < MaxActive;

	public override void _ExitTree()
	{
		activeCount--;
	}

	public override void _Ready()
	{
		activeCount++;
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		if (ExplosionAnimations != null && ExplosionAnimations.Length > 0) {
			int idx = rng.RandiRange(0, ExplosionAnimations.Length - 1);
			var frames = ExplosionAnimations[idx];
			if (frames != null) {
				animatedSprite2D.SpriteFrames = frames;
				var names = frames.GetAnimationNames();
				if (names.Length > 0) animatedSprite2D.Animation = names[0];
			}
		} else {
			animatedSprite2D.Animation = "Explode";
		}
		var firstFrame = animatedSprite2D.SpriteFrames?.GetFrameTexture(animatedSprite2D.Animation, 0);
		if (firstFrame != null && firstFrame.GetWidth() > 0) {
			float spriteScale = (2f * Radius) / firstFrame.GetWidth();
			animatedSprite2D.Scale = new Vector2(spriteScale, spriteScale);
		}
		animatedSprite2D.Play();
		var spriteFrames = animatedSprite2D.SpriteFrames;
		string activeAnim = animatedSprite2D.Animation;
		if (spriteFrames != null && spriteFrames.HasAnimation(activeAnim)) {
			int frameCount = spriteFrames.GetFrameCount(activeAnim);
			float fps = (float)spriteFrames.GetAnimationSpeed(activeAnim);
			if (fps > 0f) animationDuration = frameCount / fps;
		}
		if (animationDuration <= 0f) animationDuration = Durration > 0f ? Durration : 0.5f;
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		SetDeferred(Area2D.PropertyName.Monitorable, false);
		ApplyDamageInRadius();
	}

	public override void _Process(double delta)
	{
		elapsedTime += (float)delta;
		if (elapsedTime >= animationDuration) {
			QueueFree();
		}
	}

	private void ApplyDamageInRadius()
	{
		float r = Radius * Mathf.Abs(Scale.X);
		float rSq = r * r;
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e.CurrentHealth > 0) {
				if (e.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= rSq) {
					e.TakeDamage(Damage, ElementType.NonElemental);
				}
			}
		}
		var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (player != null && player.CurrentHealth > 0
			&& player.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= rSq) {
			player.TakeDamage(Damage);
		}
	}

	private void _on_animated_sprite_2d_animation_finished()
	{
		QueueFree();
	}
}
