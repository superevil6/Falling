using Godot;
using System.Collections.Generic;

public enum BackgroundQueuePosition { Left, Middle, Right }

public partial class BackgroundQueue : Node2D
{
	[Export]
	public BackgroundQueuePosition QueuePosition {get;set;} = BackgroundQueuePosition.Left;
	[Export]
	public PackedScene[] TilesOverride {get;set;}
	[Export]
	public float ScrollSpeedMultiplier {get;set;} = 1f;

	private PackedScene[] tiles;
	private int nextTileIndex = 0;
	private List<Node2D> activeTiles = new List<Node2D>();
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

		var main = GetParent() as Main;
		PackedScene[] stageTiles = null;
		if (main != null && main.Stages != null && main.Stages.Length > 0) {
			var stage = main.Stages[main.CurrentStage];
			stageTiles = QueuePosition == BackgroundQueuePosition.Middle
				? stage.Background2Images
				: stage.BackgroundImages;
		}
		if (stageTiles != null && stageTiles.Length > 0) {
			tiles = stageTiles;
		} else if (TilesOverride != null && TilesOverride.Length > 0) {
			tiles = TilesOverride;
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
		var scene = tiles[nextTileIndex];
		nextTileIndex = (nextTileIndex + 1) % tiles.Length;
		if (scene == null) return;
		var tile = scene.Instantiate<Node2D>();
		AddChild(tile);

		var sprite = FindFirstSprite(tile);
		if (sprite != null && sprite.Texture != null) {
			float visibleWidth = sprite.GetRect().Size.X * sprite.GlobalScale.X;
			if (visibleWidth > 0f) {
				float factor = targetTileWidth / visibleWidth;
				tile.Scale = tile.Scale * factor;
			}
		}

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

	private float GetTileHeight(Node2D tile)
	{
		var sprite = FindFirstSprite(tile);
		if (sprite != null && sprite.Texture != null) {
			return sprite.GetRect().Size.Y * sprite.GlobalScale.Y;
		}
		GD.PrintErr($"BackgroundQueue: tile '{tile.Name}' has no Sprite2D with texture — using fallback 200");
		return 200f;
	}

	private Sprite2D FindFirstSprite(Node node)
	{
		foreach (var child in node.GetChildren()) {
			if (child is Sprite2D s && s.Texture != null) return s;
		}
		return null;
	}
}
