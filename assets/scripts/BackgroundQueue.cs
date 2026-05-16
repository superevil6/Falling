using Godot;
using System.Collections.Generic;

public partial class BackgroundQueue : Node2D
{
	private PackedScene[] tiles;
	private int nextTileIndex = 0;
	private List<Node2D> activeTiles = new List<Node2D>();
	private float viewportHeight;
	private float viewportWidth;
	private WallQueue scrollSource;
	private ShaderMaterial darkenMaterial;

	public override void _Ready()
	{
		var rect = GetViewportRect().Size;
		viewportWidth = rect.X;
		viewportHeight = rect.Y;
		var main = GetParent() as Main;
		if (main != null && main.Stages != null && main.Stages.Length > 0) {
			tiles = main.Stages[main.CurrentStage].BackgroundImages;
		}
		scrollSource = GetParent()?.GetNodeOrNull<WallQueue>("Left Wall Queue");
	}

	public override void _Process(double delta)
	{
		if (tiles == null || tiles.Length == 0) return;

		Vector2 movement = scrollSource?.GetCurrentScrollMovement(delta) ?? Vector2.Zero;
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
		if (sprite != null) {
			if (darkenMaterial == null) {
				darkenMaterial = new ShaderMaterial();
				darkenMaterial.Shader = GD.Load<Shader>("res://assets/shaders/Darken.gdshader");
			}
			sprite.Material = darkenMaterial;
		}

		float height = GetTileHeight(tile);
		float globalY;
		if (activeTiles.Count == 0) {
			globalY = height / 2f;
		} else {
			var prev = activeTiles[activeTiles.Count - 1];
			globalY = prev.GlobalPosition.Y + GetTileHeight(prev) / 2f + height / 2f;
		}
		tile.GlobalPosition = new Vector2(viewportWidth / 2f, globalY);
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
