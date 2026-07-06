using Godot;
using System.Collections.Generic;

// A floating row of status-effect icons, each labelled with its current stack count,
// shown below the player or an enemy. Add it as a child of the entity and assign
// Controller; it redraws every frame from the live stack counts and hides itself when
// no statuses are active.
public partial class StatusEffectDisplay : Node2D
{
	// The stack source to read from (the owning entity's StatusEffects).
	public StatusEffectController Controller;
	// How far below the entity origin the row sits.
	public float YOffset = 60f;
	public float IconSize = 24f;
	// Horizontal gap between an icon and its count, and between adjacent entries.
	public float IconTextGap = 3f;
	public float EntrySpacing = 10f;
	public int FontSize = 15;

	private Font font;
	private readonly List<(StatusEffectType type, int count)> active = new();

	public override void _Ready()
	{
		ZIndex = 40; // above the sprite, below floating damage text (ZIndex 50)
		font = ThemeDB.FallbackFont;
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Controller == null) return;

		active.Clear();
		foreach (var type in StatusEffectVisuals.DisplayOrder) {
			int count = Controller.GetStackCount(type);
			if (count > 0) active.Add((type, count));
		}
		if (active.Count == 0) return;

		// Measure so the whole row can be centered under the entity.
		var labels = new string[active.Count];
		var textSizes = new Vector2[active.Count];
		float totalWidth = 0f;
		for (int i = 0; i < active.Count; i++) {
			labels[i] = active[i].count.ToString();
			textSizes[i] = font.GetStringSize(labels[i], HorizontalAlignment.Left, -1f, FontSize);
			totalWidth += IconSize + IconTextGap + textSizes[i].X;
			if (i > 0) totalWidth += EntrySpacing;
		}

		float x = -totalWidth / 2f;
		float rowCenterY = YOffset + IconSize / 2f;
		for (int i = 0; i < active.Count; i++) {
			var (type, _) = active[i];

			Rect2 iconRect = new Rect2(x, YOffset, IconSize, IconSize);
			Texture2D tex = StatusEffectVisuals.Icon(type);
			if (tex != null) {
				DrawTextureRect(tex, iconRect, false);
			} else {
				// No art supplied for this status yet — draw a colored dot placeholder.
				DrawCircle(new Vector2(x + IconSize / 2f, rowCenterY), IconSize / 2f, StatusEffectVisuals.Tint(type));
			}

			// Count text, vertically centered against the icon and just to its right.
			float textX = x + IconSize + IconTextGap;
			float baselineY = rowCenterY + textSizes[i].Y / 2f - font.GetDescent(FontSize);
			Vector2 textPos = new Vector2(textX, baselineY);
			DrawStringOutline(font, textPos, labels[i], HorizontalAlignment.Left, -1f, FontSize, 4, new Color(0f, 0f, 0f, 0.85f));
			DrawString(font, textPos, labels[i], HorizontalAlignment.Left, -1f, FontSize, Colors.White);

			x += IconSize + IconTextGap + textSizes[i].X + EntrySpacing;
		}
	}
}
