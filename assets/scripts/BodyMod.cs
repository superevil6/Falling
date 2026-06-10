using Godot;
using System.Collections.Generic;

public partial class BodyMod : Mod
{
    [Export]
	public BodyModType type { get; set; }
	[Export]
	public Texture2D ModImage {get;set;}
    [Export]
    public string ModDescription {get;set;}
    public List<BodyUpgrade> AppliedUpgrades {get;set;} = new List<BodyUpgrade>();
}
