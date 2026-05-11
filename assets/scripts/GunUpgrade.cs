using Godot;

public partial class GunUpgrade: Upgrade
{
	[Export]
	public GunUpgradeType Type {get; set;}
	[Export]
	public Godot.Collections.Array<GunType> ApplicableGunTypes {get; set;} = new Godot.Collections.Array<GunType>();
	[Export]
	public ElementType Element {get; set;}
}
