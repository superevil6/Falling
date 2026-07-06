using Godot;

// The health bar shown above an enemy once it has been hit. Drawn on its own canvas
// item with a raised ZIndex so it renders above the enemy's sprite (which is a child of
// the enemy and would otherwise paint over a bar drawn in the enemy's own _Draw) and
// above any overlapping neighbouring enemies. Add it as a child of the enemy and assign
// Owner; it redraws every frame and hides itself when the bar shouldn't be shown.
public partial class EnemyHealthBar : Node2D
{
	// The enemy whose health this bar reflects.
	public Enemy Owner;
	public float BarWidth = 100f;
	public float BarHeight = 16f;
	// How far above the enemy origin the bar sits (negative is up).
	public float YOffset = -60f;

	public override void _Ready()
	{
		ZIndex = 45; // above enemy sprites (0) and status displays (40), below floating damage text (50)
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Owner == null || !Owner.HasBeenHit || Owner.CurrentHealth <= 0) return;
		var p = Owner.GetParent()?.GetNodeOrNull<Player>("Player");
		if (p == null || !p.HasSeeEnemyHealth) return;

		float healthRatio = Mathf.Clamp(Owner.HealthFraction, 0f, 1f);
		Vector2 barPos = new Vector2(-BarWidth / 2f, YOffset);
		DrawRect(new Rect2(barPos, new Vector2(BarWidth, BarHeight)), new Color(0.1f, 0.1f, 0.1f, 0.85f));
		DrawRect(new Rect2(barPos, new Vector2(BarWidth * healthRatio, BarHeight)), new Color(0.9f, 0.2f, 0.2f));
		DrawRect(new Rect2(barPos, new Vector2(BarWidth, BarHeight)), Colors.Black, false, 1.0f);
	}
}
