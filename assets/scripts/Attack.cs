using Godot;
using System;

public partial class Attack : Area2D
{
	//This class is so that melee attacks and gun attacks can inherit damage, and both can be used interchangably.
	public int Damage {get;set;}
}
