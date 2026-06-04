using Godot;

public partial class PauseMenu : Node2D
{
	private VBoxContainer gunsList;
	private VBoxContainer modsList;
	private Player player;

	public override void _Ready()
	{
		gunsList = GetNodeOrNull<VBoxContainer>("GunsPanel/List");
		modsList = GetNodeOrNull<VBoxContainer>("BodyModsPanel/Scroll/List");
		GetNode<Button>("VBoxContainer/Unpause").GrabFocus();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause"))
		{
			GetTree().Paused = !GetTree().Paused;
			if (GetTree().Paused)
			{
				GetNode<Button>("VBoxContainer/Unpause").GrabFocus();
				Engine.TimeScale = 0;
				RefreshLoadout();
				Show();
			}
			else
			{
				Engine.TimeScale = 1;
				Hide();
			}
		}
	}

	private void RefreshLoadout()
	{
		if (player == null) {
			player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		}
		ClearList(gunsList);
		ClearList(modsList);
		if (player == null) return;

		if (gunsList != null && player.Guns != null) {
			for (int i = 0; i < player.Guns.Length; i++) {
				if (i > 0) gunsList.AddChild(BuildSpacer());
				var gun = player.Guns[i];
				if (gun == null) {
					gunsList.AddChild(BuildRow(null, $"Slot {i + 1}: (empty)"));
				} else {
					string name = !string.IsNullOrEmpty(gun.SourceName)
						? gun.SourceName
						: (!string.IsNullOrEmpty(gun.ResourcePath)
							? System.IO.Path.GetFileNameWithoutExtension(gun.ResourcePath)
							: "?");
					string text = $"Slot {i + 1}: {name}    Lv {gun.CurrentLevel}   XP {gun.CurrentExperience}/{gun.ExperiencePerLevel}\n"
						+ $"Damage: {gun.Damage}   Fire Rate: {gun.FireRate}   Bullets: {gun.BulletCount}   Element: {gun.Element}";
					gunsList.AddChild(BuildGunRow(gun, text));
				}
			}
		}

		if (modsList != null && player.BodyMods != null) {
			for (int i = 0; i < player.BodyMods.Length; i++) {
				var mod = player.BodyMods[i];
				if (mod == null) {
					modsList.AddChild(BuildRow(null, $"Slot {i + 1}: (empty)"));
				} else {
					string name = !string.IsNullOrEmpty(mod.Name)
						? mod.Name
						: (!string.IsNullOrEmpty(mod.ResourcePath)
							? System.IO.Path.GetFileNameWithoutExtension(mod.ResourcePath)
							: "?");
					modsList.AddChild(BuildRow(mod.ModImage, $"Slot {i + 1}: {name}"));
				}
			}
		}
	}

	private Control BuildGunRow(Gun gun, string text)
	{
		var col = new VBoxContainer();
		col.AddThemeConstantOverride("separation", 4);
		col.AddChild(BuildRow(gun.GunImage, text));
		if (gun.AppliedUpgrades != null && gun.AppliedUpgrades.Count > 0) {
			var icons = new HBoxContainer();
			icons.AddThemeConstantOverride("separation", 8);
			var counts = new System.Collections.Generic.Dictionary<GunUpgrade, int>();
			var order = new System.Collections.Generic.List<GunUpgrade>();
			foreach (var up in gun.AppliedUpgrades) {
				if (up?.UpgradeImage == null) continue;
				if (!counts.ContainsKey(up)) {
					counts[up] = 0;
					order.Add(up);
				}
				counts[up]++;
			}
			foreach (var up in order) {
				var slot = new HBoxContainer();
				slot.AddThemeConstantOverride("separation", 2);
				var icon = new TextureRect();
				icon.CustomMinimumSize = new Vector2(24f, 24f);
				icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
				icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
				icon.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
				icon.Texture = up.UpgradeImage;
				slot.AddChild(icon);
				if (counts[up] > 1) {
					var lbl = new Label();
					lbl.Text = $"x{counts[up]}";
					lbl.VerticalAlignment = VerticalAlignment.Center;
					slot.AddChild(lbl);
				}
				icons.AddChild(slot);
			}
			col.AddChild(icons);
		}
		return col;
	}

	private Control BuildSpacer()
	{
		var spacer = new Control();
		spacer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		return spacer;
	}

	private void ClearList(VBoxContainer list)
	{
		if (list == null) return;
		foreach (Node child in list.GetChildren()) {
			list.RemoveChild(child);
			child.QueueFree();
		}
	}

	private Control BuildRow(Texture2D image, string text)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);

		var tex = new TextureRect();
		tex.CustomMinimumSize = new Vector2(40f, 40f);
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
