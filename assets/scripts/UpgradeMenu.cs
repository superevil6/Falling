using Godot;
using System.Text;

public partial class UpgradeMenu : CanvasLayer
{
	private Label contentLabel;
	private Player player;

	public override void _Ready()
	{
		contentLabel = GetNode<Label>("Panel/Label");
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("menu")) {
			Visible = !Visible;
			GetTree().Paused = Visible;
			if (Visible) RefreshContent();
		}
	}

	private void RefreshContent()
	{
		if (player == null) {
			player = GetTree().Root.GetNode<Node>("Node2D")?.GetNodeOrNull<Player>("Player");
		}
		if (player == null) return;
		var sb = new StringBuilder();
		sb.AppendLine($"Health: {player.CurrentHealth}/{player.MaxHealth}");
		sb.AppendLine($"Total XP: {player.CurrentExperience}");
		sb.AppendLine();
		sb.AppendLine("=== Guns ===");
		sb.AppendLine();
		if (player.Guns != null) {
			for (int i = 0; i < player.Guns.Length; i++) {
				var gun = player.Guns[i];
				if (gun == null) {
					sb.AppendLine($"Slot {i + 1}: (empty)");
				} else {
					string name = !string.IsNullOrEmpty(gun.ResourcePath)
						? System.IO.Path.GetFileNameWithoutExtension(gun.ResourcePath)
						: "?";
					sb.AppendLine($"Slot {i + 1}: {name}");
					sb.AppendLine($"  Level: {gun.CurrentLevel}");
					sb.AppendLine($"  Experience: {gun.CurrentExperience} / {gun.ExperiencePerLevel}");
					sb.AppendLine($"  Skill Points: {gun.SkillPoints}");
				}
				sb.AppendLine();
			}
		}
		contentLabel.Text = sb.ToString();
	}
}
