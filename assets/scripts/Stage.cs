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
	[Export]
	public AudioStream BGM {get;set;}

}
