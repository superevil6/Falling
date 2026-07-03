using Godot;

// Basic title screen: the game's title and Start / Exit options.
public partial class TitleScreen : Control
{
	private const string MainScenePath = "res://assets/objects/Main.tscn";

	public override void _Ready()
	{
		EnsureGamepadUiBindings();
		SetAnchorsPreset(LayoutPreset.FullRect);

		var bg = new ColorRect { Color = new Color(0.05f, 0.05f, 0.09f) };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		var center = new CenterContainer();
		center.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(center);

		var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		vbox.AddThemeConstantOverride("separation", 28);
		center.AddChild(vbox);

		var title = new Label { Text = "Mercury Drop" };
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 130);
		title.AddThemeColorOverride("font_color", new Color(0.8f, 0.88f, 1f));
		title.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
		title.AddThemeConstantOverride("outline_size", 10);
		vbox.AddChild(title);

		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 50) }); // spacer

		var story = MakeButton(SaveSystem.HasSave(GameMode.Story) ? "Story Mode (Continue)" : "Story Mode");
		story.Pressed += () => StartOrContinue(GameMode.Story);
		vbox.AddChild(story);

		var rogue = MakeButton(SaveSystem.HasSave(GameMode.Roguelite) ? "Roguelite Mode (Continue)" : "Roguelite Mode");
		rogue.Pressed += () => StartOrContinue(GameMode.Roguelite);
		vbox.AddChild(rogue);

		if (SaveSystem.HasSave(GameMode.Story) || SaveSystem.HasSave(GameMode.Roguelite)) {
			var wipe = MakeButton("Delete Progress");
			wipe.Pressed += DeleteAllSaves;
			vbox.AddChild(wipe);
		}

		var exit = MakeButton("Exit Game");
		exit.Pressed += () => GetTree().Quit();
		vbox.AddChild(exit);
		exitButton = exit;

		story.GrabFocus();
	}

	// Testing aid: wipes both modes' saves and refreshes the menu.
	private void DeleteAllSaves()
	{
		SaveSystem.Delete(GameMode.Story);
		SaveSystem.Delete(GameMode.Roguelite);
		GetTree().ReloadCurrentScene();
	}

	private Button exitButton;

	// With A on ui_accept and d-pad/stick on ui_up/down, Godot's Button focus handles
	// navigation and confirm. B (cancel) has no "back" on the root menu, so jump to Exit.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("menu_cancel")) {
			exitButton?.GrabFocus();
			AcceptEvent();
		}
	}

	// This project's ui_* actions ship without gamepad bindings, so the controller
	// can't drive Button-based menus. Append them (A=confirm, B=cancel, d-pad + left
	// stick = navigate) once at startup; this persists on the global InputMap.
	private static void EnsureGamepadUiBindings()
	{
		AddJoyButton("ui_accept", JoyButton.A);
		AddJoyButton("ui_cancel", JoyButton.B);
		AddJoyButton("ui_up", JoyButton.DpadUp);
		AddJoyButton("ui_down", JoyButton.DpadDown);
		AddJoyButton("ui_left", JoyButton.DpadLeft);
		AddJoyButton("ui_right", JoyButton.DpadRight);
		AddJoyAxis("ui_up", JoyAxis.LeftY, -1f);
		AddJoyAxis("ui_down", JoyAxis.LeftY, 1f);
		AddJoyAxis("ui_left", JoyAxis.LeftX, -1f);
		AddJoyAxis("ui_right", JoyAxis.LeftX, 1f);
	}

	private static void AddJoyButton(string action, JoyButton button)
	{
		if (!InputMap.HasAction(action)) return;
		foreach (var e in InputMap.ActionGetEvents(action))
			if (e is InputEventJoypadButton jb && jb.ButtonIndex == button) return;
		InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = button });
	}

	private static void AddJoyAxis(string action, JoyAxis axis, float value)
	{
		if (!InputMap.HasAction(action)) return;
		foreach (var e in InputMap.ActionGetEvents(action))
			if (e is InputEventJoypadMotion jm && jm.Axis == axis && Mathf.Sign(jm.AxisValue) == Mathf.Sign(value)) return;
		InputMap.ActionAddEvent(action, new InputEventJoypadMotion { Axis = axis, AxisValue = value });
	}

	// Resumes that mode's saved progress if one exists, otherwise starts a fresh run.
	// Both modes currently start at stage 1; the chosen mode is recorded for later.
	private void StartOrContinue(GameMode mode)
	{
		var data = SaveSystem.HasSave(mode) ? SaveSystem.Load(mode) : null;
		if (data != null) {
			Main.PrepareContinue(data);
		} else {
			GameSession.Mode = mode;
			Player.CarriedState = null; // fresh run
		}
		GetTree().ChangeSceneToFile(MainScenePath);
	}

	private static Button MakeButton(string text)
	{
		var b = new Button { Text = text };
		b.CustomMinimumSize = new Vector2(340, 70);
		b.AddThemeFontSizeOverride("font_size", 40);
		return b;
	}
}
