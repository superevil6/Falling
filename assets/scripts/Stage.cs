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
	public AnimatedSprite2D[] WallChunks {get;set;}
	[Export]
	public AnimatedSprite2D[] BackgroundImages {get;set;}
	[Export]
	public AudioStream BGM {get;set;}

}
