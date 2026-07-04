using Godot;

// Plays a Cutscene as a full-screen sequence of slides, then loads the next scene.
//
// Two ways to feed it:
//   * Runtime  — CutsceneManager.Play() stashes the cutscene + next scene in statics
//                and switches to CutscenePlayer.tscn (no exports set).
//   * Authored — a scene (e.g. Intro.tscn) sets the exported Cutscene / NextScenePath
//                directly, so it works on a cold boot with no statics.
//
// Controls: an "advance" press jumps to the next slide early; a "skip" press ends the
// whole cutscene immediately. Slides also auto-advance after their Duration.
public partial class CutscenePlayer : Control
{
	// Authored fallbacks, used only when CutsceneManager has nothing pending.
	[Export]
	public Cutscene Cutscene {get;set;}
	[Export(PropertyHint.File, "*.tscn")]
	public string NextScenePath {get;set;}

	private Cutscene cutscene;
	private string nextScenePath;
	private int slideIndex = -1;
	private float slideTimer;

	// Full-screen backdrop, recoloured per slide.
	private ColorRect background;

	// Current slide's live nodes (freed and rebuilt each slide).
	private Control slideRoot;
	private Label textLabel;
	private bool typing;
	private float typedChars;
	private int totalChars;
	private float typeSpeed;
	private TextureRect visual;
	private SpriteFrames animFrames;
	private string animName;
	private int animFrame;
	private float animTimer;

	private bool finished;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		// Opaque backdrop so the cutscene fully covers whatever loaded it. Recoloured
		// per slide (defaults to black); black until the first slide sets its colour.
		background = new ColorRect { Color = new Color(0f, 0f, 0f) };
		background.SetAnchorsPreset(LayoutPreset.FullRect);
		background.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(background);

		// Prefer the runtime hand-off; fall back to authored exports for cold boot.
		cutscene = CutsceneManager.Pending ?? Cutscene;
		nextScenePath = !string.IsNullOrEmpty(CutsceneManager.NextScenePath)
			? CutsceneManager.NextScenePath
			: NextScenePath;
		CutsceneManager.Pending = null;
		CutsceneManager.NextScenePath = null;

		if (cutscene?.Slides == null || cutscene.Slides.Length == 0) {
			Finish();
			return;
		}
		ShowSlide(0);
	}

	public override void _Process(double delta)
	{
		if (finished) return;

		if (IsSkipPressed()) { Finish(); return; }

		var slide = CurrentSlide();
		if (slide == null) return;

		if (IsAdvancePressed()) {
			// While typing, the first advance press finishes the reveal instead of
			// skipping to the next slide.
			if (typing) { CompleteTyping(slide); return; }
			NextSlide();
			return;
		}

		AdvanceAnimation((float)delta);

		if (typing) {
			typedChars += (float)delta * typeSpeed;
			if (typedChars >= totalChars) {
				CompleteTyping(slide);
			} else if (textLabel != null) {
				textLabel.VisibleCharacters = (int)typedChars;
			}
			return; // hold the auto-advance timer until the text is fully revealed
		}

		if (slide.Duration > 0f) {
			slideTimer -= (float)delta;
			if (slideTimer <= 0f) NextSlide();
		}
	}

	// Reveal the full line and start the post-reveal hold (Duration counts from here).
	private void CompleteTyping(CutsceneSlide slide)
	{
		typing = false;
		if (textLabel != null) textLabel.VisibleCharacters = -1;
		slideTimer = slide.Duration;
	}

	private CutsceneSlide CurrentSlide()
	{
		if (cutscene?.Slides == null) return null;
		if (slideIndex < 0 || slideIndex >= cutscene.Slides.Length) return null;
		return cutscene.Slides[slideIndex];
	}

	private void NextSlide()
	{
		int next = slideIndex + 1;
		if (next >= cutscene.Slides.Length) { Finish(); return; }
		ShowSlide(next);
	}

	private void ShowSlide(int index)
	{
		slideIndex = index;
		var slide = cutscene.Slides[index];
		slideTimer = slide.Duration;

		if (background != null) background.Color = slide.BackgroundColor;

		slideRoot?.QueueFree();
		textLabel = null;
		typing = false;
		visual = null;
		animFrames = null;

		slideRoot = new Control();
		slideRoot.SetAnchorsPreset(LayoutPreset.FullRect);
		slideRoot.MouseFilter = MouseFilterEnum.Ignore;
		AddChild(slideRoot);

		Vector2 vp = GetViewportRect().Size;
		if (!string.IsNullOrEmpty(slide.Text)) BuildText(slide, vp);
		BuildVisual(slide, vp);

		// Gentle fade-in for each slide.
		slideRoot.Modulate = new Color(1f, 1f, 1f, 0f);
		Tween tween = slideRoot.CreateTween();
		tween.TweenProperty(slideRoot, "modulate:a", 1f, 0.3f);
	}

	private void BuildText(CutsceneSlide slide, Vector2 vp)
	{
		var label = new Label { Text = slide.Text };
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
		label.AddThemeConstantOverride("outline_size", 8);
		label.AddThemeFontSizeOverride("font_size", 44);

		// Fullscreen slides get a wider caption band so text reads over the whole image.
		var size = slide.Fullscreen
			? new Vector2(vp.X * 0.9f, vp.Y * 0.24f)
			: new Vector2(vp.X * 0.7f, vp.Y * 0.24f);
		PlaceOnEdge(label, slide.TextPosition, size, vp);
		slideRoot.AddChild(label);

		textLabel = label;
		if (slide.TypewriterText) {
			// Hide everything, then reveal a character at a time in _Process.
			label.VisibleCharacters = 0;
			typing = true;
			typedChars = 0f;
			totalChars = slide.Text.Length;
			typeSpeed = slide.TypewriterSpeed > 0f ? slide.TypewriterSpeed : 30f;
		} else {
			label.VisibleCharacters = -1; // -1 = show all
		}
	}

	private void BuildVisual(CutsceneSlide slide, Vector2 vp)
	{
		bool hasAnim = slide.Animation != null;
		if (!hasAnim && slide.Image == null) return;

		visual = new TextureRect();
		visual.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		// Fullscreen covers the viewport (cropping overflow); otherwise fit within its box.
		visual.StretchMode = slide.Fullscreen
			? TextureRect.StretchModeEnum.KeepAspectCovered
			: TextureRect.StretchModeEnum.KeepAspectCentered;
		visual.TextureFilter = TextureFilterEnum.Nearest;
		visual.MouseFilter = MouseFilterEnum.Ignore;

		if (hasAnim) {
			animFrames = slide.Animation;
			animName = ResolveAnimName(slide);
			animFrame = 0;
			animTimer = 0f;
			visual.Texture = animFrames.GetFrameCount(animName) > 0
				? animFrames.GetFrameTexture(animName, 0)
				: null;
		} else {
			visual.Texture = slide.Image;
		}

		if (slide.Fullscreen) {
			// Fill the screen; add behind the text so captions stay legible.
			visual.SetAnchorsPreset(LayoutPreset.FullRect);
			slideRoot.AddChild(visual);
			slideRoot.MoveChild(visual, 0);
		} else {
			var size = new Vector2(vp.X * 0.5f, vp.Y * 0.5f);
			PlaceOnEdge(visual, slide.ImagePosition, size, vp);
			slideRoot.AddChild(visual);
		}
	}

	private string ResolveAnimName(CutsceneSlide slide)
	{
		if (!string.IsNullOrEmpty(slide.AnimationName) && slide.Animation.HasAnimation(slide.AnimationName)) {
			return slide.AnimationName;
		}
		var names = slide.Animation.GetAnimationNames();
		return names.Length > 0 ? names[0] : "default";
	}

	private void AdvanceAnimation(float delta)
	{
		if (visual == null || animFrames == null) return;
		int count = animFrames.GetFrameCount(animName);
		if (count <= 0) return;
		float fps = (float)animFrames.GetAnimationSpeed(animName);
		if (fps <= 0f) return;

		animTimer += delta;
		float frameTime = 1f / fps;
		while (animTimer >= frameTime) {
			animTimer -= frameTime;
			animFrame = (animFrame + 1) % count;
			visual.Texture = animFrames.GetFrameTexture(animName, animFrame);
		}
	}

	// Anchors `node` (top-left layout) so a box of `size` is centred on the given edge.
	private static void PlaceOnEdge(Control node, ScreenEdge edge, Vector2 size, Vector2 vp)
	{
		Vector2 center = edge switch {
			ScreenEdge.Top    => new Vector2(vp.X * 0.5f, vp.Y * 0.22f),
			ScreenEdge.Bottom => new Vector2(vp.X * 0.5f, vp.Y * 0.78f),
			ScreenEdge.Left   => new Vector2(vp.X * 0.28f, vp.Y * 0.5f),
			ScreenEdge.Right  => new Vector2(vp.X * 0.72f, vp.Y * 0.5f),
			_                 => new Vector2(vp.X * 0.5f, vp.Y * 0.5f),
		};
		node.SetAnchorsPreset(LayoutPreset.TopLeft);
		node.Size = size;
		node.Position = center - size * 0.5f;
	}

	private static bool IsAdvancePressed()
	{
		return Pressed("menu_confirm") || Pressed("attack") || Pressed("ui_accept");
	}

	private static bool IsSkipPressed()
	{
		return Pressed("menu_cancel") || Pressed("pause") || Pressed("ui_cancel");
	}

	private static bool Pressed(string action)
	{
		return InputMap.HasAction(action) && Input.IsActionJustPressed(action);
	}

	private void Finish()
	{
		if (finished) return;
		finished = true;
		if (!string.IsNullOrEmpty(nextScenePath)) {
			GetTree().ChangeSceneToFile(nextScenePath);
		}
	}
}
