using Godot;

public partial class OrbitalShield : Area2D
{
	public float ShieldRadius {get;set;} = 18f;
	public Color ShieldColor {get;set;} = new Color(0.55f, 0.85f, 1f, 0.55f);

	public override void _Ready()
	{
		var coll = new CollisionShape2D();
		var shape = new CircleShape2D();
		shape.Radius = ShieldRadius;
		coll.Shape = shape;
		AddChild(coll);
		CollisionLayer = 0;
		CollisionMask = 0;
		SetCollisionLayerValue(6, true);
		SetCollisionMaskValue(5, true);
		AreaEntered += OnAreaEntered;
		ZIndex = 5;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is Bullet bullet) {
			if (!bullet.GetCollisionLayerValue(5)) return;
			area.QueueFree();
			return;
		}
		if (area is MeleeAttack melee) {
			if (!melee.GetCollisionLayerValue(5)) return;
			area.QueueFree();
		}
	}

	public override void _Draw()
	{
		Color halo = new Color(ShieldColor.R, ShieldColor.G, ShieldColor.B, 0.18f);
		DrawCircle(Vector2.Zero, ShieldRadius * 1.5f, halo);
		DrawCircle(Vector2.Zero, ShieldRadius, ShieldColor);
		DrawArc(Vector2.Zero, ShieldRadius, 0f, Mathf.Tau, 28, new Color(0.85f, 0.95f, 1f, 0.9f), 2f);
	}
}
