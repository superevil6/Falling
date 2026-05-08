using Godot;

public partial class Wall : Resource
{
	[Export]
	public Shape2D CollisionShape {get;set;}
	[Export]
	public Texture2D Graphic {get;set;}
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float SpeedReduction {get;set;} = 1.0f;
	[Export]
	public float Height {get;set;}
}
