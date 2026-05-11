using Godot;

public partial class BodyUpgrade: Upgrade
{
    [Export]
    public BodyUpgradeType BodyUpgradeType { get; set; }
    [Export]
    public BodyModType BodyModType { get; set; }
}