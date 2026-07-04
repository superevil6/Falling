using System;
using System.Collections.Generic;
using Godot;

public static class Helpers
{
	public static bool IsEqualApproxCustom(Vector2 a, Vector2 b)
	{
		return Math.Abs(a.X - b.X) < 0.0005;
	}

	// Experience needed to advance from `level` to `level + 1`.
	// Gentle linear curve: cost grows by `step` each level (arithmetic progression),
	// so total XP to reach a level scales quadratically without exploding like an
	// exponential curve would. step = 0 gives the old flat cost of `baseXp` per level.
	public static int ExperienceForLevel(int level, int baseXp, int step)
	{
		return baseXp + step * level;
	}

	// Centers a menu's direct Control children as a single group within the viewport,
	// preserving their relative layout. Works for single- and multi-panel menus.
	// Call from _Ready once the panels exist (they carry fixed offsets from the scene).
	public static void CenterMenu(CanvasLayer menu)
	{
		if (menu == null) return;

		var controls = new List<Control>();
		foreach (Node child in menu.GetChildren()) {
			if (child is Control c) controls.Add(c);
		}
		if (controls.Count == 0) return;

		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;
		foreach (var c in controls) {
			Vector2 pos = c.Position;
			Vector2 size = c.Size;
			minX = Mathf.Min(minX, pos.X);
			minY = Mathf.Min(minY, pos.Y);
			maxX = Mathf.Max(maxX, pos.X + size.X);
			maxY = Mathf.Max(maxY, pos.Y + size.Y);
		}

		Vector2 groupSize = new Vector2(maxX - minX, maxY - minY);
		Vector2 viewport = menu.GetViewport().GetVisibleRect().Size;
		Vector2 shift = ((viewport - groupSize) / 2f).Round() - new Vector2(minX, minY);

		foreach (var c in controls) {
			c.Position += shift;
		}
	}
}
