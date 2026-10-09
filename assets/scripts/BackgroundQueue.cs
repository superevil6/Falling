using Godot;
using System.Collections.Generic;

public enum BackgroundQueuePosition { Left, Middle, Right }

public partial class BackgroundQueue : Node2D
{
	private const string PixelArtShaderPath = "res://assets/shaders/PixelArt.gdshader";
	private const float DefaultDarkness = 0.4f;

	[Export]
	public BackgroundQueuePosition QueuePosition {get;set;} = BackgroundQueuePosition.Left;
	[Export]
	public Texture2D[] TilesOverride {get;set;}
	[Export]
	public float[] TilesOverrideDarkness {get;set;}
	// PixelArt shader applied to each background tile. Falls back to loading the
	// project shader if left unassigned.
	[Export]
	public Shader BackgroundShader {get;set;}
	// On-screen size (px) of a single repetition of the texture. The texture loops
	// horizontally across the column and vertically as strips stack. Set X to 0 to
	// stretch one copy across the full column width (no horizontal tiling); set Y to
	// 0 to keep the texture's own aspect ratio (uniform scale).
	[Export]
	public Vector2 TileScreenSize {get;set;} = Vector2.Zero;
	[Export]
	public float ScrollSpeedMultiplier {get;set;} = 1f;
	// Continuously breathe the column's on-screen width between its resting third-width
	// and that plus WidthBreathAmplitude px. The texture keeps its fixed pixel scale — a
	// wider column reveals MORE horizontal repetitions rather than stretching. The column
	// is anchored to its outer screen edge (Left grows rightward, Right grows leftward),
	// so the inner edge is what oscillates. Set to 0 to disable (e.g. the Middle column).
	[Export]
	public float WidthBreathAmplitude {get;set;} = 0f;
	// Seconds for one full expand-and-contract cycle.
	[Export]
	public float WidthBreathPeriod {get;set;} = 4f;
	// Radians added to the breath wave; offset Left vs Right to have them breathe out of sync.
	[Export]
	public float WidthBreathPhaseOffset {get;set;} = 0f;

	private Texture2D[] tiles;
	private float[] tileDarkness;
	private int nextTileIndex = 0;
	private List<Sprite2D> activeTiles = new List<Sprite2D>();
	// Smooth (sub-pixel) global positions, one per active tile. These are the source
	// of truth for scroll/spawn/despawn; the sprites themselves are rendered snapped to
	// the pixel grid so abutting strip edges never land on a fractional pixel (which
	// produces hairline seams while scrolling). Keeping the float here means slow
	// scroll (e.g. the 0.4x middle column) still advances instead of stalling on a round.
	private List<Vector2> tilePositions = new List<Vector2>();
	private float viewportHeight;
	private float viewportWidth;
	// Resting width of one column (a third of the viewport). The column breathes between
	// this and baseTileWidth + WidthBreathAmplitude.
	private float baseTileWidth;
	// The column's current animated width and horizontal centre, recomputed each frame.
	private float currentColumnWidth;
	private float currentCenterX;
	// Accumulated seconds driving the breath wave.
	private float breathPhase;
	private WallQueue scrollSource;
	private ShaderMaterial darkenMaterial;

	public override void _Ready()
	{
		var rect = GetViewportRect().Size;
		viewportHeight = rect.Y;
		viewportWidth = rect.X;
		baseTileWidth = rect.X / 3f;
		currentColumnWidth = baseTileWidth;
		currentCenterX = ColumnCenterX(baseTileWidth);
		GlobalPosition = new Vector2(currentCenterX, GlobalPosition.Y);

		if (BackgroundShader == null) {
			BackgroundShader = GD.Load<Shader>(PixelArtShaderPath);
		}

		var main = GetParent() as Main;
		Texture2D[] stageTiles = null;
		float[] stageDarkness = null;
		if (main != null && main.Stages != null && main.Stages.Length > 0) {
			var stage = main.Stages[main.StageIndex];
			bool middle = QueuePosition == BackgroundQueuePosition.Middle;
			stageTiles = middle ? stage.Background2Images : stage.BackgroundImages;
			stageDarkness = middle ? stage.Background2Darkness : stage.BackgroundDarkness;
		}
		if (stageTiles != null && stageTiles.Length > 0) {
			tiles = FlattenTiles(stageTiles);
			tileDarkness = stageDarkness;
		} else if (TilesOverride != null && TilesOverride.Length > 0) {
			tiles = FlattenTiles(TilesOverride);
			tileDarkness = TilesOverrideDarkness;
		}
		scrollSource = GetParent()?.GetNodeOrNull<WallQueue>("Left Wall Queue");
	}

	public override void _Process(double delta)
	{
		if (tiles == null || tiles.Length == 0) return;

		float width = BreathWidth(delta);
		bool widthChanged = !Mathf.IsEqualApprox(width, currentColumnWidth);
		currentColumnWidth = width;
		currentCenterX = ColumnCenterX(width);

		Vector2 movement = (scrollSource?.GetCurrentScrollMovement(delta) ?? Vector2.Zero) * ScrollSpeedMultiplier;
		for (int i = 0; i < activeTiles.Count; i++) {
			tilePositions[i] += movement;
			if (widthChanged) ApplyTiling(activeTiles[i], activeTiles[i].Texture, width);
			activeTiles[i].GlobalPosition = new Vector2(currentCenterX, tilePositions[i].Y).Round();
		}

		while (activeTiles.Count > 0) {
			var top = activeTiles[0];
			float topBottomEdge = tilePositions[0].Y + GetTileHeight(top) / 2f;
			if (topBottomEdge < 0) {
				top.QueueFree();
				activeTiles.RemoveAt(0);
				tilePositions.RemoveAt(0);
			} else break;
		}

		while (NeedSpawn()) {
			SpawnTile();
		}
	}

	private bool NeedSpawn()
	{
		if (activeTiles.Count == 0) return true;
		var bottom = activeTiles[activeTiles.Count - 1];
		float bottomEdge = tilePositions[activeTiles.Count - 1].Y + GetTileHeight(bottom) / 2f;
		return bottomEdge < viewportHeight;
	}

	private void SpawnTile()
	{
		var texture = tiles[nextTileIndex];
		int tileIndex = nextTileIndex;
		nextTileIndex = (nextTileIndex + 1) % tiles.Length;
		if (texture == null) return;

		var tile = new Sprite2D {
			Texture = texture,
			TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
			// Nearest filtering keeps strip edges crisp; linear filtering bleeds across the
			// abutting edge of stacked strips and reads as a seam. Matches the pixel-art look.
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Material = BuildMaterial(tileIndex),
		};
		ApplyTiling(tile, texture, currentColumnWidth);
		AddChild(tile);

		float height = GetTileHeight(tile);
		float globalY;
		if (activeTiles.Count == 0) {
			globalY = height / 2f;
		} else {
			float prevY = tilePositions[activeTiles.Count - 1].Y;
			globalY = prevY + GetTileHeight(activeTiles[activeTiles.Count - 1]) / 2f + height / 2f;
		}
		var smoothPos = new Vector2(currentCenterX, globalY);
		tilePositions.Add(smoothPos);
		tile.GlobalPosition = smoothPos.Round();
		activeTiles.Add(tile);
	}

	// Sprite2D region-based tiling samples the whole underlying texture, so an
	// AtlasTexture (what the Aseprite importer produces for a static frame) leaks the
	// empty atlas padding beside each frame — the grey squares. Bake each atlas frame
	// into a standalone ImageTexture up front so tiling only ever repeats the frame.
	private static Texture2D[] FlattenTiles(Texture2D[] source)
	{
		if (source == null) return null;
		var result = new Texture2D[source.Length];
		for (int i = 0; i < source.Length; i++) {
			if (source[i] is AtlasTexture atlas) {
				var image = atlas.GetImage();
				result[i] = image != null ? ImageTexture.CreateFromImage(image) : source[i];
			} else {
				result[i] = source[i];
			}
		}
		return result;
	}

	// Current animated column width. Breathes from baseTileWidth out to
	// baseTileWidth + WidthBreathAmplitude and back over WidthBreathPeriod seconds, easing
	// smoothly at both ends. Returns the resting width when breathing is disabled.
	private float BreathWidth(double delta)
	{
		if (WidthBreathAmplitude <= 0f || WidthBreathPeriod <= 0f) return baseTileWidth;
		breathPhase += (float)delta;
		float wave = 0.5f - 0.5f * Mathf.Cos(Mathf.Tau * (breathPhase / WidthBreathPeriod) + WidthBreathPhaseOffset);
		return baseTileWidth + WidthBreathAmplitude * wave;
	}

	// Horizontal centre for a column of the given width, anchored to the outer screen edge
	// so the outer edge stays put and only the inner edge moves as the width changes.
	private float ColumnCenterX(float width)
	{
		switch (QueuePosition) {
			case BackgroundQueuePosition.Left:  return width * 0.5f;
			case BackgroundQueuePosition.Right: return viewportWidth - width * 0.5f;
			default:                            return baseTileWidth * 1.5f;
		}
	}

	// Sizes the sprite to fill `width` px of column and sets up region-based repetition.
	// The horizontal scale is FIXED so one repetition is always TileScreenSize.X px on
	// screen regardless of the column's current width — widening the column widens the
	// region rect (revealing more repetitions) rather than stretching the texture. The
	// queue stacks strips to loop vertically.
	private void ApplyTiling(Sprite2D tile, Texture2D texture, float width)
	{
		float texW = texture.GetWidth();
		float texH = texture.GetHeight();
		if (texW <= 0f || texH <= 0f) return;

		// Fixed pixel scale: one texel maps to a constant number of screen pixels.
		float scaleX = TileScreenSize.X > 0f ? (TileScreenSize.X / texW) : (width / texW);
		float scaleY = TileScreenSize.Y > 0f ? (TileScreenSize.Y / texH) : scaleX;

		tile.RegionEnabled = true;
		tile.RegionRect = new Rect2(0f, 0f, width / scaleX, texH);
		tile.Scale = new Vector2(scaleX, scaleY);
	}

	private ShaderMaterial BuildMaterial(int tileIndex)
	{
		if (BackgroundShader == null) return null;
		float darkness = (tileDarkness != null && tileIndex < tileDarkness.Length)
			? tileDarkness[tileIndex]
			: DefaultDarkness;
		var mat = new ShaderMaterial { Shader = BackgroundShader };
		mat.SetShaderParameter("pixel_size", 4.0f);
		mat.SetShaderParameter("color_steps", 8);
		mat.SetShaderParameter("hue_shift", 0.0f);
		mat.SetShaderParameter("darkness", darkness);
		return mat;
	}

	private float GetTileHeight(Sprite2D tile)
	{
		if (tile.Texture != null) {
			return tile.GetRect().Size.Y * tile.GlobalScale.Y;
		}
		GD.PrintErr($"BackgroundQueue: tile '{tile.Name}' has no texture — using fallback 200");
		return 200f;
	}
}
