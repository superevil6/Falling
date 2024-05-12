using Godot;
using System;

public partial class Explosion : Area2D
{
	// Called when the node enters the scene tree for the first time.
	public int Damage {get;set;}
	[Export]
	public float Durration {get;set;}
	AnimatedSprite2D animatedSprite2D;
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animatedSprite2D.Animation = "Explode";
		
		animatedSprite2D.Play();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	private void _on_area_entered(Node2D node2D) {
		if (node2D.Name == "Player") {
			(node2D as Player).CurrentHealth -= Damage;
		} 
		if (node2D.HasMeta("Enemey")) {
			(node2D as Enemy).CurrentHealth -= Damage;
		} 
	}

	private void _on_animated_sprite_2d_animation_finished() {
		QueueFree();
	}
}
