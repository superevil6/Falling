using Godot;

public partial class Enemy : Area2D
{
	[Export]
	public int MaxHealth {get;set;}
	public int CurrentHealth {get;set;}
	[Export]
	public Gun Gun {get;set;}
	[Export]
	public DamageType DamageTypeStrength {get;set;}
	[Export]
	public DamageType DamageTypeWeakness {get;set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	private void _on_area_entered(Node2D node2D){
		Hide();
	}
}
