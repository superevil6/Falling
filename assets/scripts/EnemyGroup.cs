using Godot;

public partial class EnemyGroup : Resource
{
	[Export]
	public PackedScene[] Enemies {get;set;}
	[Export]
	public Vector2[] SpawnLocations {get;set;}
	[Export]
	public Vector2[] PostSpawnDestination {get;set;}
	[Export]
	public float TimeBetweenSpawns {get;set;} = 1;
	[Export]
	public float TimeBeforeNextGroup {get;set;} = 0;
}
