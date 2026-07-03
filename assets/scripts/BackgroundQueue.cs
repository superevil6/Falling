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

	private Texture2D[] tiles;
	private float[] tileDarkness;
	private int nextTileIndex = 0;
	private List<Sprite2D> activeTiles = new List<Sprite2D>();
	private float viewportHeight;
	private float targetTileWidth;
	private WallQueue scrollSource;
	private ShaderMaterial darkenMaterial;

	public override void _Ready()
	{
		var rect = GetViewportRect().Size;
		viewportHeight = rect.Y;
		targetTileWidth = rect.X / 3f;
		float queueX;
		switch (QueuePosition) {
			case BackgroundQueuePosition.Left:   queueX = targetTileWidth * 0.5f; break;
			case BackgroundQueuePosition.Middle: queueX = targetTileWidth * 1.5f; break;
			case BackgroundQueuePosition.Right:  queueX = targetTileWidth * 2.5f; break;
			default: queueX = targetTileWidth * 0.5f; break;
		}
		GlobalPosition = new Vector2(queueX, GlobalPosition.Y);

		if (BackgroundShader == null) {
			BackgroundShader = GD.Load<Shader>(PixelArtShaderPath);
		}

		var main = GetParent() as Main;
		Texture2D[] stageTiles = null;
		float[] stageDarkness = null;
		if (main != null && main.Stages != null && main.Stages.Length > 0) {
			var stage = main.Stages[main.CurrentStage];
			bool middle = QueuePosition == BackgroundQueuePosition.Middle;
			stageTiles = middle ? stage.Background2Images : stage.BackgroundImages;
			stageDarkness = middle ? stage.Background2Darkness : stage.BackgroundDarkness;
		}
		if (stageTiles != null && stageTiles.Length > 0) {
			tiles = stageTiles;
			tileDarkness = stageDarkness;
		} else if (TilesOverride != null && TilesOverride.Length > 0) {
			tiles = TilesOverride;
			tileDarkness = TilesOverrideDarkness;
		}
		scrollSource = GetParent()?.GetNodeOrNull<WallQueue>("Left Wall Queue");
	}

	public override void _Process(double delta)
	{
		if (tiles == null || tiles.Length == 0) return;

		Vector2 movement = (scrollSource?.GetCurrentScrollMovement(delta) ?? Vector2.Zero) * ScrollSpeedMultiplier;
		for (int i = 0; i < activeTiles.Count; i++) {
			activeTiles[i].Position += movement;
		}

		while (activeTiles.Count > 0) {
			var top = activeTiles[0];
			float topBottomEdge = top.GlobalPosition.Y + GetTileHeight(top) / 2f;
			if (topBottomEdge < 0) {
				top.QueueFree();
				activeTiles.RemoveAt(0);
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
		float bottomEdge = bottom.GlobalPosition.Y + GetTileHeight(bottom) / 2f;
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
			Material = BuildMaterial(tileIndex),
		};
		ApplyTiling(tile, texture);
		AddChild(tile);

		float height = GetTileHeight(tile);
		float globalY;
		if (activeTiles.Count == 0) {
			globalY = height / 2f;
		} else {
			var prev = activeTiles[activeTiles.Count - 1];
			globalY = prev.GlobalPosition.Y + GetTileHeight(prev) / 2f + height / 2f;
		}
		tile.GlobalPosition = new Vector2(GlobalPosition.X, globalY);
		activeTiles.Add(tile);
	}

	// Sizes the sprite to fill the column width and sets up region-based repetition.
	// One repetition is TileScreenSize px on screen; the region rect is widened so the
	// texture loops horizontally, and the queue stacks strips to loop vertically.
	private void ApplyTiling(Sprite2D tile, Texture2D texture)
	{
		float texW = texture.GetWidth();
		float texH = texture.GetHeight();
		if (texW <= 0f || texH <= 0f) return;

		int columns = TileScreenSize.X > 0f
			? Mathf.Max(1, Mathf.RoundToInt(targetTileWidth / TileScreenSize.X))
			: 1;
		float scaleX = (targetTileWidth / columns) / texW;
		float scaleY = TileScreenSize.Y > 0f ? (TileScreenSize.Y / texH) : scaleX;

		tile.RegionEnabled = true;
		tile.RegionRect = new Rect2(0f, 0f, texW * columns, texH);
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
