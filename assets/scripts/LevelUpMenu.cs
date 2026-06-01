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

	private Control radial;
	private Label titleLabel;
	private Label descriptionLabel;
	private Control cursor;
	private int selectedIndex = 0;
	private int rotationSteps = 0;
	private float currentRotation = 0f;
	private float targetRotation = 0f;
	private ulong lastAnimTick = 0;
	private string currentEntityName = "";
	private GunUpgrade[] currentGunUpgrades;
	private Gun currentGun;
	private BodyUpgrade[] currentBodyUpgrades;
	private BodyMod currentBodyMod;
	private Upgrade[] currentUpgrades;
	private readonly List<Control> optionWidgets = new();
	private Queue<(string name, Gun gun, BodyMod mod)> pendingLevelUps = new();

	public override void _Ready()
	{
		titleLabel = GetNode<Label>("Panel/Title");
		radial = GetNode<Control>("Panel/Radial");
		descriptionLabel = GetNodeOrNull<Label>("Panel/Description");
		cursor = GetNodeOrNull<Control>("Panel/Cursor");
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!Visible || Options == null || Options.Length == 0) return;
		int n = Options.Length;
		if (Input.IsActionJustPressed("move_down") || Input.IsActionJustPressed("move_right")) {
			rotationSteps++;
			selectedIndex = ((rotationSteps % n) + n) % n;
			targetRotation = -rotationSteps * (Mathf.Tau / n);
			UpdateHighlight();
		} else if (Input.IsActionJustPressed("move_up") || Input.IsActionJustPressed("move_left")) {
			rotationSteps--;
			selectedIndex = ((rotationSteps % n) + n) % n;
			targetRotation = -rotationSteps * (Mathf.Tau / n);
			UpdateHighlight();
		}
		AnimateRotation();
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
		currentUpgrades = null;
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
		currentUpgrades = new Upgrade[take];
		Options = new string[take];
		for (int i = 0; i < take; i++) {
			currentGunUpgrades[i] = applicable[i];
			currentUpgrades[i] = applicable[i];
			Options[i] = !string.IsNullOrEmpty(applicable[i].UpgradeName)
				? applicable[i].UpgradeName
				: "Upgrade";
		}
	}

	private void BuildBodyUpgradeOptions(BodyMod mod)
	{
		currentUpgrades = null;
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
		currentUpgrades = new Upgrade[take];
		Options = new string[take];
		for (int i = 0; i < take; i++) {
			currentBodyUpgrades[i] = applicable[i];
			currentUpgrades[i] = applicable[i];
			Options[i] = !string.IsNullOrEmpty(applicable[i].UpgradeName)
				? applicable[i].UpgradeName
				: "Upgrade";
		}
	}

	private static readonly Vector2 WidgetSize = new Vector2(140f, 150f);

	private void BuildOptions()
	{
		if (radial == null) return;
		foreach (var w in optionWidgets) {
			w.QueueFree();
		}
		optionWidgets.Clear();
		rotationSteps = 0;
		currentRotation = 0f;
		targetRotation = 0f;
		lastAnimTick = 0;
		if (Options == null || Options.Length == 0) return;

		int n = Options.Length;
		for (int i = 0; i < n; i++) {
			var widget = CreateOptionWidget(i);
			radial.AddChild(widget);
			optionWidgets.Add(widget);
		}
		LayoutWidgets();
		PositionCursor();
		UpdateHighlight();
	}

	private Vector2 RadialCenter()
	{
		Vector2 area = radial.Size;
		if (area.X <= 0f || area.Y <= 0f) area = new Vector2(600f, 560f);
		return new Vector2(area.X / 2f, area.Y / 2f + 30f);
	}

	private float RadialRadius()
	{
		Vector2 area = radial.Size;
		if (area.X <= 0f || area.Y <= 0f) area = new Vector2(600f, 560f);
		return Mathf.Min(area.X, area.Y) * 0.30f;
	}

	private void LayoutWidgets()
	{
		if (radial == null || optionWidgets.Count == 0) return;
		Vector2 center = RadialCenter();
		float radius = RadialRadius();
		int n = optionWidgets.Count;
		for (int i = 0; i < n; i++) {
			float angle = -Mathf.Pi / 2f + i * (Mathf.Tau / n) + currentRotation;
			Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			optionWidgets[i].Position = point - WidgetSize / 2f;
		}
	}

	private void PositionCursor()
	{
		if (cursor == null) return;
		Vector2 topSlot = RadialCenter() + new Vector2(0f, -RadialRadius());
		cursor.Position = topSlot - cursor.Size / 2f;
	}

	private void AnimateRotation()
	{
		ulong now = Time.GetTicksMsec();
		float realDelta = lastAnimTick == 0 ? 0f : (now - lastAnimTick) / 1000f;
		lastAnimTick = now;
		if (optionWidgets.Count == 0) return;
		if (Mathf.Abs(currentRotation - targetRotation) < 0.0005f) {
			if (currentRotation != targetRotation) {
				currentRotation = targetRotation;
				LayoutWidgets();
			}
			return;
		}
		currentRotation = Mathf.Lerp(currentRotation, targetRotation, Mathf.Min(1f, realDelta * 12f));
		LayoutWidgets();
	}

	private Control CreateOptionWidget(int index)
	{
		var root = new Control();
		root.Size = WidgetSize;
		root.CustomMinimumSize = WidgetSize;
		root.PivotOffset = WidgetSize / 2f;
		root.MouseFilter = Control.MouseFilterEnum.Ignore;

		Texture2D img = (currentUpgrades != null && index < currentUpgrades.Length)
			? currentUpgrades[index]?.UpgradeImage
			: null;
		float labelTop = 12f;
		if (img != null) {
			var tex = new TextureRect();
			tex.Texture = img;
			tex.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			tex.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			tex.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			var imgSize = new Vector2(52f, 52f);
			tex.Size = imgSize;
			tex.Position = new Vector2((WidgetSize.X - imgSize.X) / 2f, 16f);
			root.AddChild(tex);
			labelTop = 76f;
		}

		var label = new Label();
		label.Text = Options[index];
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Top;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.Size = new Vector2(WidgetSize.X, WidgetSize.Y - labelTop);
		label.Position = new Vector2(0f, labelTop);
		root.AddChild(label);

		return root;
	}

	private void UpdateHighlight()
	{
		for (int i = 0; i < optionWidgets.Count; i++) {
			bool sel = i == selectedIndex;
			optionWidgets[i].Modulate = sel ? Colors.Yellow : Colors.White;
			optionWidgets[i].Scale = sel ? new Vector2(1.18f, 1.18f) : Vector2.One;
		}
		if (descriptionLabel != null) {
			string desc = (currentUpgrades != null && selectedIndex < currentUpgrades.Length)
				? currentUpgrades[selectedIndex]?.UpgradeDescription
				: null;
			descriptionLabel.Text = desc ?? "";
		}
	}
}
