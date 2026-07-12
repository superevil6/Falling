using Godot;

public partial class PauseMenu : CanvasLayer
{
	private const string TitleScenePath = "res://assets/objects/TitleScreen.tscn";

	private VBoxContainer gunsList;
	private VBoxContainer modsList;
	private Player player;
	private ConfirmationDialog quitConfirm;

	public override void _Ready()
	{
		gunsList = GetNodeOrNull<VBoxContainer>("GunsPanel/List");
		modsList = GetNodeOrNull<VBoxContainer>("BodyModsPanel/Scroll/List");
		var quitButton = GetNodeOrNull<Button>("VBoxContainer/Quit");
		if (quitButton != null) quitButton.Pressed += OnQuitPressed;
		Helpers.CenterMenu(this);
		// Start hidden; _Process toggles this on the pause input. The scene root has no
		// explicit visibility set, so it would otherwise show at stage start.
		Visible = false;
	}

	// Quit asks for confirmation first, defaulting to "No" so an accidental click
	// can't drop the player out of their run.
	private void OnQuitPressed()
	{
		Sfx.PlaySelect(this);
		if (quitConfirm == null) {
			quitConfirm = new ConfirmationDialog();
			quitConfirm.Title = "Quit";
			quitConfirm.DialogText = "Are you sure you want to quit to the title screen?";
			quitConfirm.GetOkButton().Text = "Yes";
			quitConfirm.GetCancelButton().Text = "No";
			// The tree is paused while the menu is up, so the dialog must keep processing.
			quitConfirm.ProcessMode = Node.ProcessModeEnum.Always;
			quitConfirm.Confirmed += OnQuitConfirmed;
			// Focus "No" each time it opens (the OK button grabs focus by default).
			quitConfirm.AboutToPopup += () =>
				quitConfirm.GetCancelButton().CallDeferred(Control.MethodName.GrabFocus);
			AddChild(quitConfirm);
		}
		quitConfirm.PopupCentered();
	}

	private void OnQuitConfirmed()
	{
		Sfx.PlaySelect(this);
		// Clear the pause the menu applied, or the title starts frozen.
		GetTree().Paused = false;
		Engine.TimeScale = 1f;
		GetTree().ChangeSceneToFile(TitleScenePath);
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause"))
		{
			if (AnyOtherMenuOpen()) return;
			// Don't toggle the pause while the quit confirmation is up, or it would
			// hide the menu and leave the dialog floating over gameplay.
			if (quitConfirm != null && quitConfirm.Visible) return;
			GetTree().Paused = !GetTree().Paused;
			if (GetTree().Paused)
			{
				Sfx.PlaySelect(this);
				GetNode<Button>("VBoxContainer/Unpause").GrabFocus();
				Engine.TimeScale = 0;
				RefreshLoadout();
				Visible = true;
			}
			else
			{
				Sfx.PlayCancel(this);
				Engine.TimeScale = 1;
				Visible = false;
			}
		}
	}

	private bool AnyOtherMenuOpen()
	{
		var parent = GetParent();
		if (parent == null) return false;
		var lvl = parent.GetNodeOrNull<CanvasLayer>("Level Up Menu");
		if (lvl != null && lvl.Visible) return true;
		var upg = parent.GetNodeOrNull<CanvasLayer>("Upgrade Menu");
		if (upg != null && upg.Visible) return true;
		var sel = parent.GetNodeOrNull<CanvasLayer>("Selection Menu");
		if (sel != null && sel.Visible) return true;
		return false;
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
			modsList.AddChild(BuildStatsRow(player));
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
					string text = $"Slot {i + 1} ({mod.type}): {name}";
					modsList.AddChild(BuildBodyModRow(mod, text));
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

	private Control BuildStatsRow(Player player)
	{
		var label = new Label();
		label.Text = $"Health: {player.CurrentHealth}/{player.MaxHealth}    Damage Reduction: {player.DamageReduction}\n"
			+ $"Corrosive Def: {player.CorrosiveDefenseStacks}    Ice Def: {player.IceDefenseStacks}    Electric Def: {player.ElectricDefenseStacks}";
		return label;
	}

	private Control BuildBodyModRow(BodyMod mod, string text)
	{
		var col = new VBoxContainer();
		col.AddThemeConstantOverride("separation", 4);
		col.AddChild(BuildRow(mod.ModImage, text));
		if (mod.AppliedUpgrades != null && mod.AppliedUpgrades.Count > 0) {
			var icons = new HBoxContainer();
			icons.AddThemeConstantOverride("separation", 8);
			var counts = new System.Collections.Generic.Dictionary<BodyUpgrade, int>();
			var order = new System.Collections.Generic.List<BodyUpgrade>();
			foreach (var up in mod.AppliedUpgrades) {
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
