using Godot;

public partial class ItemDrop : Resource
{
	[Export]
	public PackedScene Item {get;set;}
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float Chance {get;set;} = 1.0f;
}
