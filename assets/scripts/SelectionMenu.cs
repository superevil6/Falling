using Godot;
using System.Collections.Generic;

public partial class SelectionMenu : CanvasLayer
{
	[Export]
	public string[] Options {get;set;} = new string[] { "Choice A", "Choice B", "Choice C" };
	[Export]
	public BodyMod[] BodyMods {get;set;}
	[Export]
	public Gun[] Guns {get;set;}
	[Export]
	public int PickCount {get;set;} = 3;

	[Signal]
	public delegate void SelectionMadeEventHandler(int index);

	private Control radial;
	private Label titleLabel;
	private Label descriptionLabel;
	private Label entityNameLabel;
	private Control cursor;
	private TextureRect entityImage;
	private int selectedIndex = 0;
	private int rotationSteps = 0;
	private float currentRotation = 0f;
	private float targetRotation = 0f;
	private ulong lastAnimTick = 0;
	private Resource[] currentPicks;
	private readonly List<Control> optionWidgets = new();

	public override void _Ready()
	{
		titleLabel = GetNodeOrNull<Label>("Panel/Title");
		radial = GetNode<Control>("Panel/Radial");
		descriptionLabel = GetNodeOrNull<Label>("Panel/Description");
		cursor = GetNodeOrNull<Control>("Panel/Cursor");
		entityImage = GetNodeOrNull<TextureRect>("Panel/EntityImage");
		entityNameLabel = GetNodeOrNull<Label>("Panel/EntityName");
		Helpers.CenterMenu(this);
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
			Sfx.PlaySelect(this);
			if (currentPicks != null && selectedIndex >= 0 && selectedIndex < currentPicks.Length) {
				ApplyPickToPlayer(currentPicks[selectedIndex]);
			}
			EmitSignal(SignalName.SelectionMade, selectedIndex);
			Close();
		}
	}

	// Returns whether the menu actually opened (it won't if the player can't take any
	// more guns or body mods).
	public bool Open()
	{
		if (IsPlayerGunsFull() && IsPlayerBodyModsFull()) return false;
		BuildPicksFromArrays();
		if (currentPicks == null || currentPicks.Length == 0) return false;
		BuildOptions();
		selectedIndex = 0;
		UpdateHighlight();
		Visible = true;
		GetTree().Paused = true;
		Engine.TimeScale = 0;
		return true;
	}

	public void Close()
	{
		Visible = false;
		GetTree().Paused = false;
		Engine.TimeScale = 1;
	}

	public void SetOptions(string[] options)
	{
		Options = options;
		currentPicks = null;
		if (Visible) {
			BuildOptions();
			selectedIndex = 0;
			UpdateHighlight();
		}
	}

	private void BuildPicksFromArrays()
	{
		var pool = new List<Resource>();
		if (BodyMods != null) {
			foreach (var m in BodyMods) {
				if (m != null && !PlayerHasBodyType(m.type)) pool.Add(m);
			}
		}
		if (Guns != null && !IsPlayerGunsFull()) {
			foreach (var g in Guns) {
				if (g != null && !PlayerHasGunType(g.GunType)) pool.Add(g);
			}
		}
		if (pool.Count == 0) {
			currentPicks = null;
			Options = [];
			return;
		}
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		for (int i = 0; i < pool.Count; i++) {
			int j = rng.RandiRange(i, pool.Count - 1);
			(pool[i], pool[j]) = (pool[j], pool[i]);
		}
		int take = Mathf.Min(PickCount, pool.Count);
		currentPicks = new Resource[take];
		Options = new string[take];
		for (int i = 0; i < take; i++) {
			currentPicks[i] = pool[i];
			Options[i] = GetItemName(pool[i]);
		}
	}

	private bool IsPlayerGunsFull()
	{
		var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (player == null || player.Guns == null || player.Guns.Length == 0) return false;
		for (int i = 0; i < player.Guns.Length; i++) {
			if (player.Guns[i] == null) return false;
		}
		return true;
	}

	private bool IsPlayerBodyModsFull()
	{
		var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (player == null || player.BodyMods == null || player.BodyMods.Length == 0) return false;
		for (int i = 0; i < player.BodyMods.Length; i++) {
			if (player.BodyMods[i] == null) return false;
		}
		return true;
	}

	private bool PlayerHasGunType(GunType type)
	{
		var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (player == null || player.Guns == null) return false;
		for (int i = 0; i < player.Guns.Length; i++) {
			if (player.Guns[i] != null && player.Guns[i].GunType == type) return true;
		}
		return false;
	}

	private bool PlayerHasBodyType(BodyModType type)
	{
		var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (player == null || player.BodyMods == null) return false;
		for (int i = 0; i < player.BodyMods.Length; i++) {
			if (player.BodyMods[i] != null && player.BodyMods[i].type == type) return true;
		}
		return false;
	}

	private string GetItemName(Resource item)
	{
		if (item is Gun) {
			return !string.IsNullOrEmpty(item.ResourcePath)
				? System.IO.Path.GetFileNameWithoutExtension(item.ResourcePath)
				: "Gun";
		}
		if (item is BodyMod m) {
			if (!string.IsNullOrEmpty(m.Name)) return m.Name;
			return !string.IsNullOrEmpty(item.ResourcePath)
				? System.IO.Path.GetFileNameWithoutExtension(item.ResourcePath)
				: "BodyMod";
		}
		return "?";
	}

	private static Texture2D GetItemImage(Resource item)
	{
		if (item is Gun g) return g.GunImage;
		if (item is BodyMod m) return m.ModImage;
		return null;
	}

	private static string GetItemDescription(Resource item)
	{
		if (item is Gun g) return g.GunDescription;
		if (item is BodyMod m) return m.ModDescription;
		return null;
	}

	private void ApplyPickToPlayer(Resource pick)
	{
		if (pick == null) return;
		var player = GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (player == null) return;
		if (pick is Gun g && player.Guns != null) {
			for (int i = 0; i < player.Guns.Length; i++) {
				if (player.Guns[i] == null) {
					player.Guns[i] = (Gun)g.Duplicate();
					player.UpdateGunLabel();
					return;
				}
			}
		} else if (pick is BodyMod m && player.BodyMods != null) {
			for (int i = 0; i < player.BodyMods.Length; i++) {
				if (player.BodyMods[i] == null) {
					player.BodyMods[i] = (BodyMod)m.Duplicate();
					return;
				}
			}
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

		Texture2D img = (currentPicks != null && index < currentPicks.Length)
			? GetItemImage(currentPicks[index])
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
		label.AddThemeColorOverride("font_color", GetOptionColor(index));
		root.AddChild(label);

		return root;
	}

	// Tint guns and body mods differently so the two reward kinds read apart at a glance.
	private Color GetOptionColor(int index)
	{
		if (currentPicks == null || index >= currentPicks.Length || currentPicks[index] == null) {
			return Colors.White;
		}
		return currentPicks[index] switch {
			Gun => new Color(0.4f, 0.75f, 1f),
			BodyMod => new Color(0.5f, 1f, 0.5f),
			_ => Colors.White,
		};
	}

	private void UpdateHighlight()
	{
		for (int i = 0; i < optionWidgets.Count; i++) {
			bool sel = i == selectedIndex;
			optionWidgets[i].Modulate = sel ? Colors.Yellow : Colors.White;
			optionWidgets[i].Scale = sel ? new Vector2(1.18f, 1.18f) : Vector2.One;
		}
		Resource selected = (currentPicks != null && selectedIndex >= 0 && selectedIndex < currentPicks.Length)
			? currentPicks[selectedIndex]
			: null;
		if (entityImage != null) entityImage.Texture = GetItemImage(selected);
		if (entityNameLabel != null) {
			entityNameLabel.Text = (Options != null && selectedIndex < Options.Length)
				? Options[selectedIndex]
				: "";
		}
		if (descriptionLabel != null) {
			descriptionLabel.Text = GetItemDescription(selected) ?? "";
		}
	}
}
