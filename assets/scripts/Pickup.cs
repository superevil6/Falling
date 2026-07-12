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

	private Player player;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		// Scale the whole node so the sprite and its collision shape grow/shrink together.
		Scale = new Vector2(PickupScale, PickupScale);
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null && sprite.SpriteFrames != null) {
			// Prefer an "Idle" loop if the scene has one, otherwise fall back to whatever
			// animation the scene defaults to — then keep it playing for the pickup's life.
			if (sprite.SpriteFrames.HasAnimation("Idle")) sprite.Animation = "Idle";
			sprite.Play();
		}
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
