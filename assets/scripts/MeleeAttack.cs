using Godot;

public partial class MeleeAttack : Attack
{
	public float SwingDuration {get;set;} = 0.3f;
	public Vector2 Direction {get;set;}
	public float SwingArc {get;set;} = Mathf.Pi * 0.75f;
	public float OffsetDistance {get;set;} = 90f;
	// Heal granted to the wielder per enemy hit (Player MeleeLifeSteal upgrade; 0 for enemies).
	public float LifeSteal {get;set;} = 0f;
	public AnimatedSprite2D animatedSprite2D;

	private float elapsed;
	private float baseAngle;

	public override void _Ready()
	{
		animatedSprite2D = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (animatedSprite2D != null) {
			animatedSprite2D.Animation = "Slash";
			animatedSprite2D.SpeedScale = SwingDuration > 0f ? 0.3f / SwingDuration : 1f;
			animatedSprite2D.Play();
		}
		baseAngle = Direction.Angle();
		UpdateSwingPose(0f);
	}

	public override void _Process(double delta)
	{
		elapsed += (float)delta;
		if (elapsed >= SwingDuration) {
			QueueFree();
			return;
		}
		float t = SwingDuration > 0f ? elapsed / SwingDuration : 1f;
		UpdateSwingPose(t);
	}

	private void UpdateSwingPose(float t)
	{
		float angle = baseAngle + Mathf.Lerp(-SwingArc / 2f, SwingArc / 2f, t);
		Position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * OffsetDistance;
		Rotation = angle;
	}
}
