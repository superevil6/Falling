using Godot;

public partial class Melee : Resource
{
    [Export]
    public Vector2 StartLocation {get;set;}
    [Export]
    public Vector2 EndLocation {get;set;}
    [Export]
    public int Damage {get;set;}
    [Export]
    public float SwingDuration {get;set;}
    [Export]
    public DamageType DamageType{get;set;}
    [Export]
    public float WidthMultiplier {get;set;}
    [Export]
    public float HeightMultiplier {get;set;}
    //Attack is meant to hold the animation, and the collider.
    [Export]
    public PackedScene Attack {get;set;}
}
