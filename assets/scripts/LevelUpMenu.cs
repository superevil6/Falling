using Godot;
using System.Collections.Generic;

public partial class LevelUpMenu : CanvasLayer
{
	[Export]
	public string[] Options {get;set;} = new string[] { "Damage +1", "Fire Rate +10%", "Health +1" };
	[Export]
	public int PickCount {get;set;} = 4;

	[Signal]
	public delegate void UpgradeChosenEventHandler(int index, string entityName);

	private VBoxContainer container;
	private Label titleLabel;
	private int selectedIndex = 0;
	private string currentEntityName = "";
	private GunUpgrade[] currentGunUpgrades;
	private Gun currentGun;
	private BodyUpgrade[] currentBodyUpgrades;
	private BodyMod currentBodyMod;
	private Queue<(string name, Gun gun, BodyMod mod)> pendingLevelUps = new();

	public override void _Ready()
	{
		titleLabel = GetNode<Label>("Panel/Title");
		container = GetNode<VBoxContainer>("Panel/VBoxContainer");
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!Visible || Options == null || Options.Length == 0) return;
		if (Input.IsActionJustPressed("move_down")) {
			selectedIndex = (selectedIndex + 1) % Options.Length;
			UpdateHighlight();
		} else if (Input.IsActionJustPressed("move_up")) {
			selectedIndex = (selectedIndex - 1 + Options.Length) % Options.Length;
			UpdateHighlight();
		}
		if (Input.IsActionJustPressed("menu_confirm")) {
			if (currentGun != null && currentGunUpgrades != null
				&& selectedIndex >= 0 && selectedIndex < currentGunUpgrades.Length) {
				currentGun.ApplyUpgrade(currentGunUpgrades[selectedIndex]);
			}
			if (currentBodyMod != null && currentBodyUpgrades != null
				&& selectedIndex >= 0 && selectedIndex < currentBodyUpgrades.Length) {
				var player = GetTree().Root.GetNodeOrNull<Player>("Node2D/Player");
				player?.ApplyBodyUpgrade(currentBodyUpgrades[selectedIndex]);
			}
			EmitSignal(SignalName.UpgradeChosen, selectedIndex, currentEntityName);
			if (pendingLevelUps.Count > 0) {
				ShowNext();
			} else {
				Close();
			}
		}
	}

	public void Open(string entityName, Gun leveledUpGun = null, BodyMod leveledUpMod = null)
	{
		pendingLevelUps.Enqueue((entityName, leveledUpGun, leveledUpMod));
		if (!Visible) ShowNext();
	}

	private void ShowNext()
	{
		if (pendingLevelUps.Count == 0) {
			Close();
			return;
		}
		var next = pendingLevelUps.Dequeue();
		currentEntityName = next.name ?? "";
		currentGun = next.gun;
		currentBodyMod = next.mod;
		if (titleLabel != null) titleLabel.Text = $"{currentEntityName} Leveled Up!";
		if (next.gun != null) {
			BuildGunUpgradeOptions(next.gun);
			currentBodyUpgrades = null;
		} else if (next.mod != null) {
			BuildBodyUpgradeOptions(next.mod);
			currentGunUpgrades = null;
		} else {
			currentGunUpgrades = null;
			currentBodyUpgrades = null;
		}
		BuildOptions();
		selectedIndex = 0;
		UpdateHighlight();
		Visible = true;
		GetTree().Paused = true;
		Engine.TimeScale = 0;
	}

	public void Close()
	{
		Visible = false;
		GetTree().Paused = false;
		Engine.TimeScale = 1;
	}

	private void BuildGunUpgradeOptions(Gun gun)
	{
		var main = GetTree().Root.GetNodeOrNull<Main>("Node2D");
		if (main == null || main.PossibleGunUpgrades == null) {
			currentGunUpgrades = null;
			return;
		}
		var applicable = new List<GunUpgrade>();
		foreach (var up in main.PossibleGunUpgrades) {
			if (up == null) continue;
			bool typeMatches = up.ApplicableGunTypes == null
				|| up.ApplicableGunTypes.Count == 0
				|| up.ApplicableGunTypes.Contains(gun.GunType);
			if (!typeMatches) continue;
			if (up.Type == GunUpgradeType.Pierce && gun.Pierce) continue;
			if (up.Type == GunUpgradeType.Explode && gun.Explode) continue;
			if (up.Type == GunUpgradeType.Element && gun.Element != ElementType.NonElemental) continue;
			applicable.Add(up);
		}
		if (applicable.Count == 0) {
			currentGunUpgrades = null;
			return;
		}
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		for (int i = 0; i < applicable.Count; i++) {
			int j = rng.RandiRange(i, applicable.Count - 1);
			(applicable[i], applicable[j]) = (applicable[j], applicable[i]);
		}
		int take = Mathf.Min(PickCount, applicable.Count);
		currentGunUpgrades = new GunUpgrade[take];
		Options = new string[take];
		for (int i = 0; i < take; i++) {
			currentGunUpgrades[i] = applicable[i];
			Options[i] = !string.IsNullOrEmpty(applicable[i].UpgradeName)
				? applicable[i].UpgradeName
				: "Upgrade";
		}
	}

	private void BuildBodyUpgradeOptions(BodyMod mod)
	{
		var main = GetTree().Root.GetNodeOrNull<Main>("Node2D");
		if (main == null || main.PossibleBodyUpgrades == null) {
			currentBodyUpgrades = null;
			return;
		}
		var applicable = new List<BodyUpgrade>();
		foreach (var up in main.PossibleBodyUpgrades) {
			if (up == null) continue;
			if (up.BodyModType == mod.type) applicable.Add(up);
		}
		if (applicable.Count == 0) {
			currentBodyUpgrades = null;
			return;
		}
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		for (int i = 0; i < applicable.Count; i++) {
			int j = rng.RandiRange(i, applicable.Count - 1);
			(applicable[i], applicable[j]) = (applicable[j], applicable[i]);
		}
		int take = Mathf.Min(PickCount, applicable.Count);
		currentBodyUpgrades = new BodyUpgrade[take];
		Options = new string[take];
		for (int i = 0; i < take; i++) {
			currentBodyUpgrades[i] = applicable[i];
			Options[i] = !string.IsNullOrEmpty(applicable[i].UpgradeName)
				? applicable[i].UpgradeName
				: "Upgrade";
		}
	}

	private void BuildOptions()
	{
		if (container == null) return;
		foreach (Node child in container.GetChildren()) {
			container.RemoveChild(child);
			child.QueueFree();
		}
		if (Options == null) return;
		for (int i = 0; i < Options.Length; i++) {
			var label = new Label();
			label.Text = Options[i];
			container.AddChild(label);
		}
	}

	private void UpdateHighlight()
	{
		if (container == null || Options == null) return;
		var children = container.GetChildren();
		for (int i = 0; i < children.Count && i < Options.Length; i++) {
			if (children[i] is Label label) {
				label.Modulate = (i == selectedIndex) ? Colors.Yellow : Colors.White;
				label.Text = (i == selectedIndex ? "> " : "  ") + Options[i];
			}
		}
	}
}
