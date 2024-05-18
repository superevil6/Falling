using System;
using System.Linq;
using Godot;

public partial class Bullet : Attack
{
	[Export]
	public float BulletSpeed {get;set;}= 100;
	public float BulletLifetime {get;set;}
	public Vector2 Direction {get;set;}
    [Export]
	public BulletMod[] BulletMods {get;set;}
	[Export]
	public PackedScene Explosion {get;set;}
	private int ExplosionDamage;

	//for wave movement
	private double time;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (BulletMods.Any(mod => mod.Explode)) {
			// ExplosionDamage = Damage;
			// Damage = 0;
		}
		if (BulletMods.Any(mod => mod.SizeMultiplier > 0)) {
			float scale = BulletMods.Sum(mod => mod.SizeMultiplier);
			Scale = new Vector2(Scale.X + scale, Scale.Y + scale);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (BulletMods !=null) {
			if (BulletMods.Any(mod => mod.Wave > 0)) {
				float wave = BulletMods.Sum(mod => mod.Wave);
				time += delta;
				Position += new Vector2(
				((float)Mathf.Cos(time * (wave * 1.5))) * Direction.Y, 
				((float)Mathf.Cos(time * (wave * 1))) * Direction.X).Normalized() * BulletSpeed * (float)delta;

			}
		}
		GlobalPosition = GlobalPosition + BulletSpeed * Direction.Normalized() * (float)delta;
		if (BulletLifetime > 0) {
			BulletLifetime -= (float)delta;
		} else {
			QueueFree();
		}

	}

	private void _on_area_entered(Node2D node){
		if (BulletMods != null) {
			if (!BulletMods.Any(mod => mod.Pierce == true)) {
				QueueFree();
			}
			if (BulletMods.Any(mod => mod.Explode == true)) {
				CallDeferred("GenerateExplosion");
			}
		}
		 else {
			QueueFree();
		}
	}

	private void GenerateExplosion() {
		Explosion e = Explosion.Instantiate<Explosion>();
		e.Position = Position;
		e.Damage = Damage;
		GetParent().AddChild(e);
		QueueFree();	
	}
}
