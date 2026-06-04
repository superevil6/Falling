using Godot;

public partial class Upgrade : Resource
{
	[Export]
	public Texture2D UpgradeImage {get;set;}
	[Export]
	public string UpgradeName { get; set; }
	[Export]
	public string UpgradeDescription { get; set; }
	[Export]
	public float Value { get; set; }
	[Export]
	public UpgradeRarity Rarity {get; set;}
}
