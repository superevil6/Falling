using System;
using Godot;

public static class Helpers
{
	public static bool IsEqualApproxCustom(Vector2 a, Vector2 b)
	{
		return Math.Abs(a.X - b.X) < 0.0005;
	}
}
