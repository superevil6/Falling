using Godot;
using System.Collections.Generic;

public partial class WallQueue : Node2D
{
	[Export]
	public bool IsRightSide {get;set;}
	// Distance (px) the wall sits in from its screen edge. On _Ready the queue
	// anchors itself to the left edge (or the right edge when IsRightSide) at this
	// inset, so walls track the viewport width instead of a hard-coded position.
	// Lower toward 0 to push the wall flush to the edge.
	[Export]
	public float EdgeOffset {get;set;}
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
		var viewport = GetViewportRect().Size;
		viewportHeight = viewport.Y;
		AnchorToEdge(viewport.X);
		var main = GetParent() as Main;
		if (main == null || main.Stages == null || main.Stages.Length == 0) return;
		var stage = main.Stages[main.StageIndex];
		speed = stage.ScrollSpeed;
		var stageChunks = IsRightSide ? stage.RightWallChunks : stage.LeftWallChunks;
		if (stageChunks != null && stageChunks.Length > 0) {
			chunks = stageChunks;
		}
		if (stage.WallFillTiles != null && stage.WallFillTiles.Length > 0) {
			var fill = new WallFill();
			fill.ZIndex = -5; // behind the wall chunks and gameplay, above the background
			fill.Initialize(this, stage.WallFillTiles, stage.WallFillTileSize, stage.WallSpriteHalfWidth);
			// Deferred: this runs during Main's own _Ready, when AddChild is rejected.
			GetParent().CallDeferred(Node.MethodName.AddChild, fill);
		}
	}

	// The resting inset this wall returns to (after a temporary contraction) and the
	// inset it is currently moving toward. Both start at EdgeOffset.
	public float BaseEdgeOffset {get; private set;}
	public float CurrentEdgeOffset {get; private set;}
	private Tween edgeTween;

	// Positions the queue at EdgeOffset in from its screen edge. Chunks are spawned
	// as children, so moving the queue later (e.g. a closing-walls mechanic that
	// tweens this X inward) carries the whole wall with it.
	private void AnchorToEdge(float viewportWidth)
	{
		float x = IsRightSide ? viewportWidth - EdgeOffset : EdgeOffset;
		Position = new Vector2(x, Position.Y);
		BaseEdgeOffset = EdgeOffset;
		CurrentEdgeOffset = EdgeOffset;
	}

	private float EdgeOffsetToX(float offset)
	{
		float viewportWidth = GetViewportRect().Size.X;
		return IsRightSide ? viewportWidth - offset : offset;
	}

	// Animates this wall to a new inset over `duration` seconds. A larger offset
	// means the wall sits further in from its edge (a tighter playfield).
	public void AnimateEdgeOffset(float newOffset, float duration)
	{
		CurrentEdgeOffset = newOffset;
		edgeTween?.Kill();
		if (duration <= 0f) {
			Position = new Vector2(EdgeOffsetToX(newOffset), Position.Y);
			return;
		}
		edgeTween = CreateTween();
		edgeTween.TweenProperty(this, "position:x", EdgeOffsetToX(newOffset), duration);
	}

	// Makes the current inset the new resting point (for a permanent contraction).
	public void CommitEdgeOffset()
	{
		BaseEdgeOffset = CurrentEdgeOffset;
	}

	public Vector2 GetCurrentScrollMovement(double delta)
	{
		if (player == null) player = GetParent()?.GetNodeOrNull<Player>("Player");
		float inputY = 0f;
		if (player != null) {
			float raw = Input.GetActionStrength("move_down") - Input.GetActionStrength("move_up");
			float screenH = player.ScreenSize.Y;
			bool pushingTopEdge = player.Position.Y <= screenH * 0.1f && raw < 0f;
			bool pushingBottomEdge = player.Position.Y >= screenH * 0.9f && raw > 0f;
			if (pushingTopEdge || pushingBottomEdge) inputY = raw;
		}
		float wallFactor = (player != null && player.IsTouchingWall) ? WallContactSpeedFactor : 1f;
		float currentSpeed = speed * (1 + inputY * InputSpeedFactor) * wallFactor;
		return new Vector2(0, -currentSpeed * ((float)delta * 5));
	}

	public override void _Process(double delta)
	{
		if (chunks == null || chunks.Length == 0) return;

		Vector2 movement = GetCurrentScrollMovement(delta);
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
