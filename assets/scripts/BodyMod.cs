using Godot;
using System;

public partial class BodyMod : Mod
{
    [Export]
	public BodyModType type { get; set; }
	[Export]
	public Texture2D ModImage {get;set;}
    [Export]
    public string ModDescription {get;set;}
}
