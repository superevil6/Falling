using Godot;

public enum PickupType { Experience, Health }

public partial class Pickup : Area2D
{
	// What this pickup grants when collected. Set per-scene so a single script
	// (and one .tscn workflow) covers every pickup variant.
	[Export]
	public PickupType Type {get;set;} = PickupType.Experience;
	// Amount granted on collection — health restored for Health, XP gained for Experience.
	[Export]
	public int Amount {get;set;} = 1;
	// Probability (0-1) that an enemy listing this pickup in its ItemDrops actually spawns
	// it on a given drop roll. Lives on the pickup so no separate drop resource is needed.
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float DropChance {get;set;} = 1f;
	// Visual/collision size multiplier: 1 = 100% of the sprite's native size, 2 = 200%, etc.
	[Export]
	public float PickupScale {get;set;} = 1f;
	// Seconds before this pickup despawns if left uncollected. 0 (or less) = never expires.
	[Export]
	public float ExpireTime {get;set;} = 0f;

	private float lifeTimer = 0f;
	private AnimatedSprite2D sprite;
	// How fast the sprite flickers on/off during the final stretch before expiring.
	private const float FlickerInterval = 0.08f;
	[Export]
	public float MagnetRadius {get;set;} = 100f;
	[Export]
	public float MagnetSpeed {get;set;} = 300f;
	// Sound played when the player collects this pickup.
	[Export]
	public AudioStream PickupSound {get;set;}

	// --- Glow ---
	// On-screen reach (pixels) of the pickup's emitted light. 0 disables the glow.
	[Export]
	public float LightRadius {get;set;} = 40f;
	// Brightness of the emitted light.
	[Export]
	public float LightEnergy {get;set;} = 0.8f;
	// Tint of the emitted light. Leave fully transparent (default) to auto-pick a colour
	// from the pickup Type — greenish for Experience, reddish for Health.
	[Export]
	public Color LightColor {get;set;} = new Color(0f, 0f, 0f, 0f);

	// A soft radial gradient (opaque centre -> transparent edge) shared by every pickup
	// light, built on first use. Radius is driven by each light's TextureScale, so one
	// texture serves all sizes. Mirrors the wall-decoration lights in WallQueue.
	private const float LightGradientRadius = 128f;
	private static Texture2D lightGradient;

	private Player player;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		// Scale the whole node so the sprite and its collision shape grow/shrink together.
		Scale = new Vector2(PickupScale, PickupScale);
		AttachLight();
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null && sprite.SpriteFrames != null) {
			// Prefer an "Idle" loop if the scene has one, otherwise fall back to whatever
			// animation the scene defaults to — then keep it playing for the pickup's life.
			if (sprite.SpriteFrames.HasAnimation("Idle")) sprite.Animation = "Idle";
			sprite.Play();
		}
	}

	// Gives the pickup a PointLight2D so it glows against the ambient-dimmed stage, the
	// same technique the wall decorations use (see WallQueue.AttachLight).
	private void AttachLight()
	{
		if (LightRadius <= 0f) return;

		// A fully-transparent LightColor means "auto": tint by what the pickup grants.
		Color color = LightColor.A > 0f
			? LightColor
			: Type switch {
				PickupType.Health => new Color(1f, 0.4f, 0.4f),
				_ => new Color(0.5f, 1f, 0.6f),
			};

		var light = new PointLight2D {
			Texture = GetLightGradient(),
			Color = color,
			Energy = LightEnergy,
			// The gradient disc spans LightGradientRadius px; divide out the node's scale
			// (this child inherits it) so the on-screen reach stays `LightRadius` px
			// whatever the PickupScale.
			TextureScale = LightRadius / (LightGradientRadius * Mathf.Max(PickupScale, 0.0001f)),
		};
		AddChild(light);
	}

	private static Texture2D GetLightGradient()
	{
		if (lightGradient != null) return lightGradient;
		var gradient = new Gradient();
		gradient.SetColor(0, Colors.White);
		gradient.SetColor(1, new Color(1f, 1f, 1f, 0f));
		lightGradient = new GradientTexture2D {
			Gradient = gradient,
			Width = 256,
			Height = 256,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f),
		};
		return lightGradient;
	}

	public override void _Process(double delta)
	{
		if (ExpireTime > 0f) {
			lifeTimer += (float)delta;
			if (lifeTimer >= ExpireTime) {
				QueueFree();
				return;
			}
			// In the final 30% of its life, flicker fully on/off to warn it's about to vanish.
			if (sprite != null) {
				float remaining = ExpireTime - lifeTimer;
				bool warning = remaining <= ExpireTime * 0.3f;
				sprite.Visible = !warning || (int)(lifeTimer / FlickerInterval) % 2 == 0;
			}
		}
		if (player == null) {
			player = GetParent()?.GetNodeOrNull<Player>("Player");
			if (player == null) return;
		}
		float mult = player.ItemMagnetMultiplier;
		float dist = GlobalPosition.DistanceTo(player.GlobalPosition);
		if (dist < MagnetRadius * mult && dist > 0.01f) {
			Vector2 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition += toPlayer * MagnetSpeed * mult * (float)delta;
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.GetParent() is Player p) {
			OnCollected(p);
			Sfx.Play(this, PickupSound);
			QueueFree();
		}
	}

	private void OnCollected(Player player)
	{
		switch (Type) {
			case PickupType.Health:
				CollectHealth(player);
				break;
			case PickupType.Experience:
				CollectExperience(player);
				break;
		}
	}

	private void CollectHealth(Player player)
	{
		player.CurrentHealth = Mathf.Min(player.MaxHealth, player.CurrentHealth + Amount);
		player.GetParent().GetNode<TextureProgressBar>("Health Bar").Value = player.CurrentHealth;
	}

	private void CollectExperience(Player player)
	{
		int gained = Mathf.Max(1, Mathf.RoundToInt(Amount * player.ExperienceMultiplier));
		player.CurrentExperience += gained;
		player.GetParent().GetNode<Label>("Experience Counter").Text = $"XP: {player.CurrentExperience}";
		var menu = player.GetParent().GetNodeOrNull<LevelUpMenu>("Level Up Menu");
		if (player.Guns != null) {
			foreach (var gun in player.Guns) {
				if (gun == null) continue;
				int prev = gun.CurrentLevel;
				gun.AddExperience(gained);
				if (gun.CurrentLevel > prev) {
					string name = !string.IsNullOrEmpty(gun.SourceName)
						? gun.SourceName
						: (!string.IsNullOrEmpty(gun.ResourcePath)
							? System.IO.Path.GetFileNameWithoutExtension(gun.ResourcePath)
							: "Gun");
					menu?.Open(name, gun, null);
				}
			}
		}
		if (player.BodyMods != null) {
			foreach (var mod in player.BodyMods) {
				if (mod == null) continue;
				int prev = mod.Level;
				mod.AddExperience(gained);
				if (mod.Level > prev) {
					string name = !string.IsNullOrEmpty(mod.Name) ? mod.Name : "BodyMod";
					menu?.Open(name, null, mod);
				}
			}
		}
		player.UpdateGunLabel();
	}
}
