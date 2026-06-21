using Godot;

// Basic title screen: the game's title and Start / Exit options.
public partial class TitleScreen : Control
{
	private const string MainScenePath = "res://assets/objects/Main.tscn";

	public override void _Ready()
	{
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

		var start = MakeButton("Start Game");
		start.Pressed += () => GetTree().ChangeSceneToFile(MainScenePath);
		vbox.AddChild(start);

		var exit = MakeButton("Exit Game");
		exit.Pressed += () => GetTree().Quit();
		vbox.AddChild(exit);

		start.GrabFocus();
	}

	private static Button MakeButton(string text)
	{
		var b = new Button { Text = text };
		b.CustomMinimumSize = new Vector2(340, 70);
		b.AddThemeFontSizeOverride("font_size", 40);
		return b;
	}
}
