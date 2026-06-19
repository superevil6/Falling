using Godot;

public partial class Stage : Resource
{
	[Export]
	public string Name {get;set;}
	[Export]
	public EnemyGroup[] EnemyGroup {get;set;}
	[Export]
	public PackedScene[] Hazards {get;set;}
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
	public PackedScene[] BackgroundImages {get;set;}
	[Export]
	public PackedScene[] Background2Images {get;set;}
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
