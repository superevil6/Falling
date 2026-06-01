using Godot;

public partial class ExperiencePickup : Pickup
{
	[Export]
	public int ExperienceAmount {get;set;} = 1;

	protected override void OnCollected(Player player)
	{
		player.CurrentExperience += ExperienceAmount;
		player.GetParent().GetNode<Label>("Experience Counter").Text = $"XP: {player.CurrentExperience}";
		var menu = player.GetParent().GetNodeOrNull<LevelUpMenu>("Level Up Menu");
		if (player.Guns != null) {
			foreach (var gun in player.Guns) {
				if (gun == null) continue;
				int prev = gun.CurrentLevel;
				gun.AddExperience(ExperienceAmount);
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
				mod.AddExperience(ExperienceAmount);
				if (mod.Level > prev) {
					string name = !string.IsNullOrEmpty(mod.Name) ? mod.Name : "BodyMod";
					menu?.Open(name, null, mod);
				}
			}
		}
		player.UpdateGunLabel();
	}
}
