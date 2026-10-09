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

	// --- Wall decorations ---
	// Pool of decoration textures for this stage (null/empty leaves the wall bare) and
	// the tuning copied over from the Stage resource in _Ready.
	private Texture2D[] decorations;
	private WallSide decorationSide;
	private float decorationMinInterval;
	private float decorationMaxInterval;
	private float decorationInset;
	private float decorationScale;
	private int decorationZIndex;
	// Countdown to the next decoration spawn; reset to a fresh random gap after each one.
	private float decorationTimer;
	// Live decoration sprites, scrolled and despawned alongside the wall chunks.
	private List<Node2D> activeDecorations = new List<Node2D>();
	// Per-decoration light config, index-aligned with `decorations`, plus a lazily-built
	// radial gradient shared by every light this queue spawns.
	private Color[] decorationLightColors;
	private float[] decorationLightRadii;
	private float[] decorationLightEnergies;
	private Texture2D lightGradient;
	// On-screen radius (px) of the shared gradient disc; a light's reach scales from this.
	private const float LightGradientRadius = 128f;

	public override void _Ready()
	{
		var viewport = GetViewportRect().Size;
		viewportHeight = viewport.Y;
		AnchorToEdge(viewport.X);
		var main = GetParent() as Main;
		if (main == null || main.Stages == null || main.Stages.Length == 0) return;
		// Demo-loop colour wash. Set on the queue so it propagates to every wall chunk
		// spawned as a child (the PixelArt shader preserves modulate); the fill below is
		// parented to Main, so it gets the tint applied directly.
		Modulate = main.LoopTint;
		var stage = main.Stages[main.StageIndex];
		speed = stage.ScrollSpeed;
		var stageChunks = IsRightSide ? stage.RightWallChunks : stage.LeftWallChunks;
		if (stageChunks != null && stageChunks.Length > 0) {
			chunks = stageChunks;
		}
		if (stage.WallDecorations != null && stage.WallDecorations.Length > 0) {
			decorations = stage.WallDecorations;
			decorationSide = stage.DecorationSide;
			decorationMinInterval = stage.DecorationMinInterval;
			decorationMaxInterval = stage.DecorationMaxInterval;
			decorationInset = stage.DecorationInset;
			decorationScale = stage.DecorationScale;
			decorationZIndex = stage.DecorationZIndex;
			decorationLightColors = stage.DecorationLightColors;
			decorationLightRadii = stage.DecorationLightRadii;
			decorationLightEnergies = stage.DecorationLightEnergies;
			decorationTimer = NextDecorationDelay();
		}
		if (stage.WallFillTiles != null && stage.WallFillTiles.Length > 0) {
			var fill = new WallFill();
			fill.ZIndex = -5; // behind the wall chunks and gameplay, above the background
			fill.Initialize(this, stage.WallFillTiles, stage.WallFillTileSize, stage.WallSpriteHalfWidth, stage.WallFillTileDarkness);
			fill.Modulate = main.LoopTint; // matches the wall chunks' wash (fill is parented to Main, not this queue)
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

		UpdateDecorations(delta, movement);
	}

	// Scrolls live decorations with the walls, frees ones that have left the top of the
	// screen, and spawns a new one when the random timer elapses.
	private void UpdateDecorations(double delta, Vector2 movement)
	{
		if (decorations == null || decorations.Length == 0) return;

		for (int i = activeDecorations.Count - 1; i >= 0; i--) {
			var deco = activeDecorations[i];
			deco.Position += movement;
			float topEdge = deco.GlobalPosition.Y + GetDecorationHeight(deco) / 2f;
			if (topEdge < 0) {
				deco.QueueFree();
				activeDecorations.RemoveAt(i);
			}
		}

		decorationTimer -= (float)delta;
		if (decorationTimer <= 0f) {
			SpawnDecoration();
			decorationTimer = NextDecorationDelay();
		}
	}

	private void SpawnDecoration()
	{
		// Honour the configured side: Random rolls a coin per spawn so a decoration lands
		// on either wall at random; the fixed sides only spawn on their own queue.
		switch (decorationSide) {
			case WallSide.Left:   if (IsRightSide) return; break;
			case WallSide.Right:  if (!IsRightSide) return; break;
			case WallSide.Random: if (GD.Randf() < 0.5f) return; break;
		}

		int index = (int)(GD.Randi() % (uint)decorations.Length);
		var texture = decorations[index];
		if (texture == null) return;

		var deco = new Sprite2D {
			Texture = texture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Scale = new Vector2(decorationScale, decorationScale),
			// Art is authored anchored to its left edge, so flip it on the right wall to
			// face into the corridor.
			FlipH = IsRightSide,
			ZIndex = decorationZIndex,
		};
		// Anchor by the art's attachment edge rather than its centre: the left wall pins
		// the texture's left edge to the inner wall and extends right into the corridor;
		// the right wall mirrors this (flipped art, pinned at the right edge, extends
		// left). Offset is in unscaled texture pixels (applied before Scale).
		deco.Offset = new Vector2((IsRightSide ? -1f : 1f) * texture.GetWidth() / 2f, 0f);
		// The wall texture is centred on the queue and its visible inner (corridor-facing)
		// edge sits right around that centre line, so the queue's own X is the attach line.
		// DecorationInset nudges from there: positive pushes into the corridor, negative
		// pulls back onto the wall. Mirrored for the right wall, whose corridor is left.
		float innerEdgeX = (IsRightSide ? -1f : 1f) * decorationInset;
		// Spawn just below the viewport so it scrolls up into view like the wall chunks.
		float spawnY = viewportHeight + GetDecorationHeight(deco) / 2f;
		deco.Position = new Vector2(innerEdgeX, spawnY);
		AttachLight(deco, index);
		AddChild(deco);
		activeDecorations.Add(deco);
	}

	// Gives a decoration a PointLight2D child when its index carries a positive radius.
	// The light rides along as a child, so it scrolls and frees with the sprite.
	private void AttachLight(Sprite2D deco, int index)
	{
		if (decorationLightRadii == null || index >= decorationLightRadii.Length) return;
		float radius = decorationLightRadii[index];
		if (radius <= 0f) return;

		Color color = (decorationLightColors != null && index < decorationLightColors.Length)
			? decorationLightColors[index]
			: Colors.White;
		float energy = (decorationLightEnergies != null && index < decorationLightEnergies.Length)
			? decorationLightEnergies[index]
			: 1f;

		var light = new PointLight2D {
			Texture = GetLightGradient(),
			Color = color,
			Energy = energy,
			// The gradient disc spans LightGradientRadius px; divide out the sprite's scale
			// (this child inherits it) so the on-screen reach is `radius` px whatever the
			// DecorationScale.
			TextureScale = radius / (LightGradientRadius * Mathf.Max(decorationScale, 0.0001f)),
			// Centre on the art's midpoint — the sprite is anchored by its edge via Offset,
			// which is the art's centre in the sprite's local (pre-scale) space.
			Position = deco.Offset,
		};
		deco.AddChild(light);
	}

	// A soft radial gradient (opaque centre → transparent edge) shared by every light this
	// queue spawns, built on first use. Radius is driven entirely by each light's
	// TextureScale, so one texture serves all sizes.
	private Texture2D GetLightGradient()
	{
		if (lightGradient != null) return lightGradient;
		var gradient = new Gradient();
		gradient.SetColor(0, Colors.White);
		gradient.SetColor(1, new Color(1f, 1f, 1f, 0f));
		lightGradient = new GradientTexture2D {
			Gradient = gradient,
			Width = 256,
			Height = 256,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f),
		};
		return lightGradient;
	}

	// A fresh random gap until the next decoration, clamped so a min above the max can't
	// stall spawns entirely.
	private float NextDecorationDelay()
	{
		float min = Mathf.Min(decorationMinInterval, decorationMaxInterval);
		float max = Mathf.Max(decorationMinInterval, decorationMaxInterval);
		return (float)GD.RandRange(min, max);
	}

	private float GetDecorationHeight(Node2D deco)
	{
		if (deco is Sprite2D sprite && sprite.Texture != null) {
			return sprite.GetRect().Size.Y * sprite.GlobalScale.Y;
		}
		return 0f;
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
