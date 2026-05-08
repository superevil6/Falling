using Godot;

public partial class HealthPickup : Pickup
{
	[Export]
	public int HealAmount {get;set;} = 1;

	protected override void OnCollected(Player player)
	{
		player.CurrentHealth = Mathf.Min(player.MaxHealth, player.CurrentHealth + HealAmount);
		player.GetParent().GetNode<TextureProgressBar>("Health Bar").Value = player.CurrentHealth;
	}
}
