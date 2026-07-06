using Godot;
using System.Collections.Generic;

// Central lookup for how each status effect is shown floating on an entity: the stack
// icon and a fallback tint. Icons are supplied by dropping a PNG named after the enum
// member (e.g. "DamageOverTime.png", "Slow.png") into res://assets/status effects/.
// Missing art falls back to a colored dot so stacks are still visible before the icons
// are added.
public static class StatusEffectVisuals
{
	private const string IconFolder = "res://assets/status effects/";
	private static readonly Dictionary<StatusEffectType, Texture2D> iconCache = new();

	// Left-to-right order the stack icons are laid out in.
	public static readonly StatusEffectType[] DisplayOrder = {
		StatusEffectType.DamageOverTime,
		StatusEffectType.Slow,
		StatusEffectType.ReducedFireRate,
		StatusEffectType.Blind,
	};

	// Returns the icon for a status, or null if no art has been supplied for it yet.
	// Results (including "no icon") are cached so we only hit the filesystem once.
	public static Texture2D Icon(StatusEffectType type)
	{
		if (iconCache.TryGetValue(type, out var cached)) return cached;
		string path = IconFolder + type + ".png";
		Texture2D tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		iconCache[type] = tex;
		return tex;
	}

	// Placeholder/label tint used when a status has no icon art yet.
	public static Color Tint(StatusEffectType type) => type switch {
		StatusEffectType.DamageOverTime => new Color(1f, 0.3f, 0.2f),
		StatusEffectType.Slow => new Color(0.4f, 0.6f, 1f),
		StatusEffectType.ReducedFireRate => new Color(1f, 0.9f, 0.25f),
		StatusEffectType.Blind => new Color(0.55f, 0.35f, 0.75f),
		_ => Colors.White,
	};
}
