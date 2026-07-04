using Godot;

// Shown when the player dies. Fades the screen, displays GAME OVER, and offers a
// return to the title screen. Built in code (like TitleScreen) so it needs no scene.
public partial class GameOverMenu : CanvasLayer
{
	private const string TitleScenePath = "res://assets/objects/TitleScreen.tscn";

	public override void _Ready()
	{
		Layer = 128; // above all gameplay UI
		// The game is paused while this menu is up, so it must keep processing.
		ProcessMode = Node.ProcessModeEnum.Always;

		var bg = new ColorRect { Color = new Color(0f, 0f, 0f, 0.75f) };
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bg.MouseFilter = Control.MouseFilterEnum.Stop;
		AddChild(bg);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(center);

		var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		vbox.AddThemeConstantOverride("separation", 48);
		center.AddChild(vbox);

		var label = new Label { Text = "GAME OVER" };
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", 140);
		label.AddThemeColorOverride("font_color", new Color(1f, 0.2f, 0.2f));
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
		label.AddThemeConstantOverride("outline_size", 12);
		vbox.AddChild(label);

		var button = new Button { Text = "Return to Title" };
		button.CustomMinimumSize = new Vector2(340, 70);
		button.AddThemeFontSizeOverride("font_size", 40);
		button.Pressed += ReturnToTitle;
		vbox.AddChild(button);
		button.GrabFocus();
	}

	private void ReturnToTitle()
	{
		Sfx.PlaySelect(this);
		// Clear the pause the game-over screen applied, or the title starts frozen.
		GetTree().Paused = false;
		Engine.TimeScale = 1f;
		GetTree().ChangeSceneToFile(TitleScenePath);
	}
}
