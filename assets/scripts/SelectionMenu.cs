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
	public int PickCount {get;set;} = 4;

	[Signal]
	public delegate void SelectionMadeEventHandler(int index);

	private VBoxContainer container;
	private int selectedIndex = 0;
	private Resource[] currentPicks;

	public override void _Ready()
	{
		container = GetNode<VBoxContainer>("Panel/VBoxContainer");
		Helpers.CenterMenu(this);
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
