using Godot;
using System.Collections.Generic;

public partial class WallQueue : Node2D
{
	[Export]
	public bool IsRightSide {get;set;}
	[Export]
	public PackedScene[] chunks {get;set;}
	[Export]
	public float InputSpeedFactor {get;set;} = 0.5f;
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float WallContactSpeedFactor {get;set;} = 0.1f;
	private float speed;
	private Player player;
	private int nextChunkIndex = 0;
	private List<Node2D> activeChunks = new List<Node2D>();
	private float viewportHeight;

	public override void _Ready()
	{
		viewportHeight = GetViewportRect().Size.Y;
		var main = GetParent() as Main;
		if (main == null || main.Stages == null || main.Stages.Length == 0) return;
		var stage = main.Stages[main.CurrentStage];
		speed = stage.ScrollSpeed;
		if (chunks == null || chunks.Length == 0) {
			chunks = IsRightSide ? stage.RightWallChunks : stage.LeftWallChunks;
		}
	}

	public override void _Process(double delta)
	{
		if (chunks == null || chunks.Length == 0) return;

		if (player == null) player = GetParent()?.GetNodeOrNull<Player>("Player");
		float inputY = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
		float wallFactor = (player != null && player.IsTouchingWall) ? WallContactSpeedFactor : 1f;
		float currentSpeed = speed * (1 + inputY * InputSpeedFactor) * wallFactor;
		Vector2 movement = new Vector2(0, -currentSpeed * ((float)delta * 5));
		for (int i = 0; i < activeChunks.Count; i++) {
			activeChunks[i].Position += movement;
		}

		while (activeChunks.Count > 0) {
			var top = activeChunks[0];
			float topBottomEdge = top.GlobalPosition.Y + GetChunkHeight(top) / 2f;
			if (topBottomEdge < 0) {
				top.QueueFree();
				activeChunks.RemoveAt(0);
			} else break;
		}

		while (NeedSpawn()) {
			SpawnChunk();
		}
	}

	private bool NeedSpawn()
	{
		if (activeChunks.Count == 0) return true;
		var bottom = activeChunks[activeChunks.Count - 1];
		float bottomEdge = bottom.GlobalPosition.Y + GetChunkHeight(bottom) / 2f;
		return bottomEdge < viewportHeight;
	}

	private void SpawnChunk()
	{
		var scene = chunks[nextChunkIndex];
		nextChunkIndex = (nextChunkIndex + 1) % chunks.Length;
		var chunk = scene.Instantiate<Node2D>();
		AddChild(chunk);
		if (IsRightSide) {
			var sprite = chunk.GetNodeOrNull<Sprite2D>("Sprite2D");
			if (sprite != null) sprite.FlipH = true;
		}

		float height = GetChunkHeight(chunk);
		float globalY;
		if (activeChunks.Count == 0) {
			globalY = viewportHeight + height / 2f;
		} else {
			var prev = activeChunks[activeChunks.Count - 1];
			globalY = prev.GlobalPosition.Y + GetChunkHeight(prev) / 2f + height / 2f;
		}
		chunk.GlobalPosition = new Vector2(GlobalPosition.X, globalY);
		activeChunks.Add(chunk);
	}

	private float GetChunkHeight(Node2D chunk)
	{
		var sprite = chunk.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null && sprite.Texture != null) {
			return sprite.GetRect().Size.Y * sprite.GlobalScale.Y;
		}
		GD.PrintErr($"WallQueue: chunk '{chunk.Name}' has no Sprite2D with texture — using fallback 100");
		return 100f;
	}
}
