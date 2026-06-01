using Godot;

public partial class OffScreenEnemyIndicator : Node2D
{
	[Export]
	public float LeftBound {get;set;} = 120f;
	[Export]
	public float RightBound {get;set;} = 1380f;
	[Export]
	public float EdgeMargin {get;set;} = 36f;
	[Export]
	public Color IndicatorColor {get;set;} = new Color(1f, 0.2f, 0.2f, 0.95f);

	private float viewportHeight;

	public override void _Ready()
	{
		viewportHeight = GetViewportRect().Size.Y;
		ZIndex = 100;
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		foreach (var n in GetTree().GetNodesInGroup("Enemy")) {
			if (n is Enemy e && e.CurrentHealth > 0) {
				float ey = e.GlobalPosition.Y;
				bool offBelow = ey > viewportHeight;
				bool offAbove = ey < 0f;
				if (!offBelow && !offAbove) continue;
				float ix = Mathf.Clamp(e.GlobalPosition.X, LeftBound, RightBound);
				float iy = offBelow ? viewportHeight - EdgeMargin : EdgeMargin;
				DrawExclamation(new Vector2(ix, iy), offBelow);
			}
		}
	}

	private void DrawExclamation(Vector2 center, bool pointsDown)
	{
		float barWidth = 6f;
		float barHeight = 16f;
		float dotRadius = 3.5f;
		float dotGap = 6f;
		if (pointsDown) {
			float barTop = center.Y - barHeight - dotGap * 0.5f;
			DrawRect(new Rect2(center.X - barWidth / 2f, barTop, barWidth, barHeight), IndicatorColor);
			DrawCircle(new Vector2(center.X, barTop + barHeight + dotGap), dotRadius, IndicatorColor);
		} else {
			float dotY = center.Y - barHeight * 0.5f - dotGap * 0.5f;
			DrawCircle(new Vector2(center.X, dotY), dotRadius, IndicatorColor);
			float barTop = dotY + dotGap;
			DrawRect(new Rect2(center.X - barWidth / 2f, barTop, barWidth, barHeight), IndicatorColor);
		}
	}
}
