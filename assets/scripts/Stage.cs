using Godot;

public partial class Stage : Resource
{
	[Export]
	public string Name {get;set;}
	// Optional cutscene played (via the CutscenePlayer) right before this stage loads.
	// Leave null for stages that begin immediately.
	[Export]
	public Cutscene Cutscene {get;set;}
	[Export]
	public EnemyGroup[] EnemyGroup {get;set;}

	// Shown once this stage's boss is beaten and the player has picked their reward:
	// LevelEndText flashes in large red text while the whole screen (gameplay, HUD and
	// player) fades to LevelEndColor, right before the next stage loads.
	[Export]
	public string LevelEndText {get;set;}
	[Export]
	public Color LevelEndColor {get;set;} = new Color(0f, 0f, 0f, 1f);

	// --- Obstacle spawner ---
	// The ObstacleSpawner node inherits these at runtime (see ObstacleSpawner._Ready).
	// Hazards is the pool of obstacle scenes it cycles through; an empty pool leaves the
	// spawner idle. The rest tune its timing/placement/warning for this stage.
	[Export]
	public PackedScene[] Hazards {get;set;}
	[Export]
	public float ObstacleMinInterval {get;set;} = 2f;
	[Export]
	public float ObstacleMaxInterval {get;set;} = 5f;
	[Export]
	public float ObstacleMinX {get;set;} = 100f;
	[Export]
	public float ObstacleMaxX {get;set;} = 1400f;
	[Export]
	public float ObstacleSpawnYBelow {get;set;} = 100f;
	[Export]
	public float ObstacleWarningDuration {get;set;} = 1f;
	[Export]
	public float ObstacleWarningYOffset {get;set;} = 80f;
	[Export]
	public Color ObstacleWarningColor {get;set;} = new Color(0.3f, 0.6f, 1f);
	[Export]
	public float ObstacleStaticSpawnMargin {get;set;} = 120f;
	[Export]
	public PackedScene[] LeftWallChunks {get;set;}
	[Export]
	public PackedScene[] RightWallChunks {get;set;}
	// Tiles used to fill the area exposed behind a wall when it contracts inward.
	// The fill is a randomized, vertically-scrolling grid drawn from these textures.
	[Export]
	public Texture2D[] WallFillTiles {get;set;}
	[Export]
	public float WallFillTileSize {get;set;} = 64f;
	// Darkens the wall-fill tiles (0 = untouched, 1 = black), matching the darkness
	// term of the PixelArt shader used for backgrounds. Applied uniformly to the fill.
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float WallFillTileDarkness {get;set;} = 0f;
	// The wall sprite is centred on its queue position and the wall graphic is drawn
	// from the texture's left edge, so the graphic's outer edge sits half a sprite
	// width out from the boundary. The fill stops at that edge (not at the boundary)
	// so it butts against the visible wall instead of spilling into the corridor.
	// Set this to half the wall sprite's width.
	[Export]
	public float WallSpriteHalfWidth {get;set;} = 128f;
	[Export]
	public float ScrollSpeed {get;set;} = 200f;
	[Export]
	public Texture2D[] BackgroundImages {get;set;}
	// Per-tile darkness for BackgroundImages, fed to the PixelArt shader. Indices line
	// up with BackgroundImages; missing entries fall back to the queue's default.
	[Export]
	public float[] BackgroundDarkness {get;set;}
	[Export]
	public Texture2D[] Background2Images {get;set;}
	[Export]
	public float[] Background2Darkness {get;set;}
	// Timed stage events (wall contractions, obstacle bursts). The StageDirector
	// fires one every EventInterval seconds — sequentially, or randomly when
	// RandomEventOrder is set. Suppressed during boss fights if DisableEventsDuringBoss.
	[Export]
	public StageEvent[] StageEvents {get;set;}
	[Export]
	public float EventInterval {get;set;} = 15f;
	[Export]
	public bool RandomEventOrder {get;set;} = false;
	[Export]
	public bool DisableEventsDuringBoss {get;set;} = true;

	// Music: each track is an optional intro (played once) followed by a looping
	// main track. Leave an intro null to start straight on the loop.
	[Export]
	public AudioStream StageIntroMusic {get;set;}
	[Export]
	public AudioStream StageMusic {get;set;}
	[Export]
	public AudioStream BossIntroMusic {get;set;}
	[Export]
	public AudioStream BossMusic {get;set;}

}
