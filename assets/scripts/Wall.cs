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
	[Export]
	public float StatusTickInterval {get;set;} = 1f;
	[Export]
	public int DotStacksPerTick {get;set;} = 0;
	[Export]
	public int SlowStacksPerTick {get;set;} = 0;
	[Export]
	public int FireRateStacksPerTick {get;set;} = 0;
	[Export]
	public int BlindStacksPerTick {get;set;} = 0;
}
