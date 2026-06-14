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
