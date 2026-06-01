using Godot;

public partial class UpgradeMenu : CanvasLayer
{
	private Label headerLabel;
	private VBoxContainer list;
	private Player player;

	public override void _Ready()
	{
		headerLabel = GetNode<Label>("Panel/Header");
		list = GetNode<VBoxContainer>("Panel/Scroll/List");
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
			player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		}
		if (player == null) return;

		headerLabel.Text = $"Health: {player.CurrentHealth}/{player.MaxHealth}    Total XP: {player.CurrentExperience}";

		foreach (Node child in list.GetChildren()) {
			list.RemoveChild(child);
			child.QueueFree();
		}

		list.AddChild(SectionLabel("Guns"));
		if (player.Guns != null) {
			for (int i = 0; i < player.Guns.Length; i++) {
				var gun = player.Guns[i];
				if (gun == null) {
					list.AddChild(BuildRow(null, $"Slot {i + 1}: (empty)"));
				} else {
					string name = !string.IsNullOrEmpty(gun.SourceName)
						? gun.SourceName
						: (!string.IsNullOrEmpty(gun.ResourcePath)
							? System.IO.Path.GetFileNameWithoutExtension(gun.ResourcePath)
							: "?");
					string text = $"Slot {i + 1}: {name}\n"
						+ $"Lv {gun.CurrentLevel}   XP {gun.CurrentExperience}/{gun.ExperiencePerLevel}";
					list.AddChild(BuildRow(gun.GunImage, text));
				}
			}
		}

		list.AddChild(SectionLabel("Body Mods"));
		if (player.BodyMods != null) {
			for (int i = 0; i < player.BodyMods.Length; i++) {
				var mod = player.BodyMods[i];
				if (mod == null) {
					list.AddChild(BuildRow(null, $"Slot {i + 1}: (empty)"));
				} else {
					string name = !string.IsNullOrEmpty(mod.Name)
						? mod.Name
						: (!string.IsNullOrEmpty(mod.ResourcePath)
							? System.IO.Path.GetFileNameWithoutExtension(mod.ResourcePath)
							: "?");
					string text = $"Slot {i + 1}: {name}\n"
						+ $"Lv {mod.Level}   XP {mod.CurrentExperience}/{mod.ExperiencePerLevel}";
					list.AddChild(BuildRow(mod.ModImage, text));
				}
			}
		}
	}

	private Label SectionLabel(string text)
	{
		var label = new Label();
		label.Text = $"=== {text} ===";
		return label;
	}

	private Control BuildRow(Texture2D image, string text)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);

		var tex = new TextureRect();
		tex.CustomMinimumSize = new Vector2(48f, 48f);
		tex.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		tex.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		tex.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		if (image != null) tex.Texture = image;
		row.AddChild(tex);

		var label = new Label();
		label.Text = text;
		label.VerticalAlignment = VerticalAlignment.Center;
		row.AddChild(label);

		return row;
	}
}
