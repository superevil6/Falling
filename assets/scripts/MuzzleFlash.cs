using Godot;

// A brief burst of light emitted when a gun fires. Spawned at the muzzle by whoever
// pulls the trigger (player or enemy), it flares to full brightness for a frame then
// fades and frees itself. It is a real PointLight2D, so it lights the surrounding
// scene like the stage decorations do. The colour, reach, and brightness come from
// the firing Gun (MuzzleFlashColor/Radius/Energy) so each weapon has its own signature.
public partial class MuzzleFlash : PointLight2D
{
	// The gradient disc built below spans this many pixels of radius at TextureScale 1.
	private const float GradientRadius = 128f;

	// On-screen reach of the flash in pixels; set from the gun before _Ready.
	private float radius = 120f;
	// Seconds the flash takes to fade out and free itself.
	private const float Duration = 0.09f;

	private static Texture2D sharedGradient;

	// Spawns a one-shot muzzle flash as a child of `parent` (the shooter, so it rides
	// along as they move), offset forward from the muzzle along the aim direction.
	// Does nothing when the gun disables its flash (MuzzleFlashRadius <= 0).
	public static void Spawn(Node2D parent, Gun gun, Vector2 aimDirection)
	{
		if (parent == null || gun == null || gun.MuzzleFlashRadius <= 0f) return;
		Vector2 aim = aimDirection == Vector2.Zero ? Vector2.Right : aimDirection.Normalized();
		var flash = new MuzzleFlash {
			radius = gun.MuzzleFlashRadius,
			Color = gun.MuzzleFlashColor,
			Energy = gun.MuzzleFlashEnergy,
			Position = aim * gun.MuzzleFlashOffset,
			Rotation = aim.Angle(),
		};
		parent.AddChild(flash);
	}

	public override void _Ready()
	{
		Texture = GetGradient();
		TextureScale = radius / GradientRadius;
		var tween = CreateTween();
		tween.TweenProperty(this, "energy", 0f, Duration);
		tween.Parallel().TweenProperty(this, "texture_scale", TextureScale * 1.4f, Duration);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	// A soft radial gradient (opaque centre → transparent edge) shared by every flash,
	// built on first use. Reach is driven entirely by TextureScale, so one texture
	// serves every size and colour.
	private static Texture2D GetGradient()
	{
		if (sharedGradient != null) return sharedGradient;
		var gradient = new Gradient();
		gradient.SetColor(0, Colors.White);
		gradient.SetColor(1, new Color(1f, 1f, 1f, 0f));
		sharedGradient = new GradientTexture2D {
			Gradient = gradient,
			Width = 256,
			Height = 256,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f),
		};
		return sharedGradient;
	}
}
