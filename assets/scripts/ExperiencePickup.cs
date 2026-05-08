using Godot;

public partial class ExperiencePickup : Pickup
{
	[Export]
	public int ExperienceAmount {get;set;} = 1;

	protected override void OnCollected(Player player)
	{
		player.CurrentExperience += ExperienceAmount;
		player.GetParent().GetNode<Label>("Experience Counter").Text = $"XP: {player.CurrentExperience}";
	}
}
