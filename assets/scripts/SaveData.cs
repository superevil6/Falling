using Godot;

// The on-disk save payload: how far the player has progressed plus their loadout.
public partial class SaveData : Resource
{
	[Export] public int StageBeaten {get;set;}
	[Export] public GameMode Mode {get;set;}
	[Export] public PlayerState State {get;set;}
}
