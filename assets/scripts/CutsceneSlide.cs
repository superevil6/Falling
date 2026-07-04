using Godot;

// One "slide" of a cutscene: a line (or block) of text anchored to a screen edge,
// plus an optional visual (a static image or a looping animation) anchored to its
// own edge. The slide holds for Duration seconds before the cutscene advances; the
// player can advance early or skip the whole cutscene. A Duration <= 0 means the
// slide waits for the player's advance input instead of auto-progressing.
public partial class CutsceneSlide : Resource
{
	[Export(PropertyHint.MultilineText)]
	public string Text {get;set;}
	[Export]
	public ScreenEdge TextPosition {get;set;} = ScreenEdge.Bottom;
	// Reveal the text one character at a time (typewriter) instead of all at once.
	[Export]
	public bool TypewriterText {get;set;}
	// Characters revealed per second while TypewriterText is on.
	[Export]
	public float TypewriterSpeed {get;set;} = 30f;

	// Static image for the slide. Ignored if Animation is set.
	[Export]
	public Texture2D Image {get;set;}
	// Optional looping animation; takes precedence over Image when present.
	[Export]
	public SpriteFrames Animation {get;set;}
	// Which animation in the SpriteFrames to play; falls back to "default" / the first.
	[Export]
	public string AnimationName {get;set;} = "default";
	[Export]
	public ScreenEdge ImagePosition {get;set;} = ScreenEdge.Top;
	// When set, the visual fills the whole screen (ImagePosition is ignored) and text
	// is laid out as a full-width caption over it, still anchored by TextPosition.
	[Export]
	public bool Fullscreen {get;set;}

	// Solid colour drawn behind the slide's content. Defaults to opaque black.
	[Export]
	public Color BackgroundColor {get;set;} = new Color(0f, 0f, 0f);

	[Export]
	public float Duration {get;set;} = 5f;
}
