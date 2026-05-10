using Godot;

public partial class LevelUpMenu : CanvasLayer
{
	[Export]
	public string[] Options {get;set;} = new string[] { "Damage +1", "Fire Rate +10%", "Health +1" };

	[Signal]
	public delegate void UpgradeChosenEventHandler(int index, string entityName);

	private VBoxContainer container;
	private Label titleLabel;
	private int selectedIndex = 0;
	private string currentEntityName = "";

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
			EmitSignal(SignalName.UpgradeChosen, selectedIndex, currentEntityName);
			Close();
		}
	}

	public void Open(string entityName)
	{
		currentEntityName = entityName ?? "";
		if (titleLabel != null) titleLabel.Text = $"{currentEntityName} Leveled Up!";
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
