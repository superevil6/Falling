using Godot;

// Draws the terrain that fills the area exposed behind a wall as it contracts. The
// fill spans from the screen edge to the wall's current boundary (so it grows as the
// wall closes in), is built from a randomized grid of the provided tiles, and scrolls
// vertically in sync with the wall. Created and driven by a WallQueue.
public partial class WallFill : Node2D
{
	private WallQueue queue;
	private Texture2D[] tiles;
	private float tileSize = 64f;
	private float halfWidth;
	private Color modulate = Colors.White;
	private int cols;
	private int rows;
	private int[] grid; // cols*rows of random tile indices, stable so the pattern is fixed
	private float scrollY;
	private float viewportWidth;
	private float viewportHeight;
	private readonly RandomNumberGenerator rng = new RandomNumberGenerator();

	// Stores config only. The grid is built in _Ready, once this node is in the tree
	// and the viewport size is actually available.
	public void Initialize(WallQueue wallQueue, Texture2D[] fillTiles, float cellSize, float wallHalfWidth, float darkness = 0f)
	{
		queue = wallQueue;
		tiles = fillTiles;
		tileSize = cellSize > 0f ? cellSize : 64f;
		halfWidth = wallHalfWidth;
		// Reproduce the PixelArt shader's darkness term (rgb *= 1 - darkness) as a
		// draw-time modulate, so the fill can be dimmed without a shader material.
		float shade = 1f - Mathf.Clamp(darkness, 0f, 1f);
		modulate = new Color(shade, shade, shade, 1f);
	}

	public override void _Ready()
	{
		var vp = GetViewportRect().Size;
		viewportWidth = vp.X;
		viewportHeight = vp.Y;
		rng.Randomize();
		if (tiles == null || tiles.Length == 0) return;
		// Grid covers up to half the screen width (a wall can't pass the centre) and
		// the full height, plus a margin row/col for scrolling and the partial edge.
		cols = Mathf.CeilToInt(viewportWidth * 0.5f / tileSize) + 2;
		rows = Mathf.CeilToInt(viewportHeight / tileSize) + 2;
		grid = new int[cols * rows];
		for (int i = 0; i < grid.Length; i++) grid[i] = rng.RandiRange(0, tiles.Length - 1);
	}

	private float FillWidth()
	{
		float boundaryX = queue.Position.X;
		float baseWidth = queue.IsRightSide ? viewportWidth - boundaryX : boundaryX;
		// Stop at the wall graphic's outer edge (half a sprite width out from the
		// centred boundary) rather than at the boundary, so the fill meets the
		// visible wall instead of spilling into the corridor.
		return baseWidth - halfWidth;
	}

	public override void _Process(double delta)
	{
		if (queue == null || tiles == null || tiles.Length == 0) return;
		// Scroll in lockstep with the wall chunks.
		scrollY += queue.GetCurrentScrollMovement(delta).Y;
		if (FillWidth() > 0.5f) QueueRedraw();
	}

	public override void _Draw()
	{
		if (queue == null || tiles == null || tiles.Length == 0) return;
		float fillWidth = FillWidth();
		if (fillWidth <= 0.5f) return;
		bool isRight = queue.IsRightSide;

		// Vertical band of tile rows currently on screen (absolute indices, so the
		// random pattern scrolls smoothly rather than reshuffling).
		int firstRow = Mathf.FloorToInt((0f - scrollY) / tileSize) - 1;
		int lastRow = Mathf.FloorToInt((viewportHeight - scrollY) / tileSize) + 1;

		int colCount = Mathf.CeilToInt(fillWidth / tileSize);
		for (int c = 0; c < colCount; c++) {
			float distFromEdge = c * tileSize;
			float cellW = Mathf.Min(tileSize, fillWidth - distFromEdge);
			if (cellW <= 0f) break;
			float fracW = cellW / tileSize;
			float worldX;
			float regionXFrac;
			if (isRight) {
				// Anchored to the right edge, filling leftward toward the wall; the
				// partial (innermost) column is clipped on its left, so show the
				// right portion of the tile.
				worldX = viewportWidth - distFromEdge - cellW;
				regionXFrac = 1f - fracW;
			} else {
				worldX = distFromEdge;
				regionXFrac = 0f;
			}
			int gridC = c % cols;
			for (int absRow = firstRow; absRow <= lastRow; absRow++) {
				float worldY = absRow * tileSize + scrollY;
				int gridR = ((absRow % rows) + rows) % rows;
				Texture2D tex = tiles[grid[gridC * rows + gridR]];
				if (tex == null) continue;
				float texW = tex.GetWidth();
				float texH = tex.GetHeight();
				var dest = new Rect2(worldX, worldY, cellW, tileSize);
				var src = new Rect2(regionXFrac * texW, 0f, fracW * texW, texH);
				DrawTextureRectRegion(tex, dest, src, modulate);
			}
		}
	}
}
