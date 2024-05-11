using Godot;
using System;

public partial class BulletMod : Resource
{
    [Export]
    public bool Pierce {get;set;}
    [Export]
    public bool Explode {get;set;}
    [Export]
    public int Split {get;set;}
    [Export]
    public bool Ricochet {get;set;}
    [Export]
    public float Wave {get;set;}
    [Export]
    public float SizeMultiplier {get;set;}
    [Export]
    public DamageType DamageType {get;set;}
    [Export]
    public float HeatSeaking {get;set;}
    [Export]
    public float Slowing {get;set;}
    [Export]
    public int DamageModifier {get;set;}
}
