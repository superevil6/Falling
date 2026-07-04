using Godot;
using System.Collections.Generic;

public class StatusEffectController
{
	public float StackDuration = 5f;
	public float DamagePerStackPerSecond = 1f;
	public float SlowPerStack = 0.1f;
	public float FireRateReductionPerStack = 0.1f;
	public float SpreadPerStack = 0.05f;

	private Dictionary<StatusEffectType, List<float>> stackExpiries = new Dictionary<StatusEffectType, List<float>>();
	// Per-type resistance in [0, 0.95]; shortens the duration of newly-applied stacks.
	private Dictionary<StatusEffectType, float> resistances = new Dictionary<StatusEffectType, float>();
	private float dotTickTimer = 0f;
	private float currentTime = 0f;

	public int GetStackCount(StatusEffectType type)
	{
		return stackExpiries.TryGetValue(type, out var list) ? list.Count : 0;
	}

	// Sets the resistance fraction for a status type (0 = no resistance, 0.95 = 95% shorter).
	public void SetResistance(StatusEffectType type, float fraction)
	{
		resistances[type] = Mathf.Clamp(fraction, 0f, 0.95f);
	}

	private float EffectiveDuration(StatusEffectType type)
	{
		float r = resistances.TryGetValue(type, out var v) ? v : 0f;
		return StackDuration * (1f - r);
	}

	public void AddStack(StatusEffectType type)
	{
		if (!stackExpiries.ContainsKey(type)) {
			stackExpiries[type] = new List<float>();
		}
		stackExpiries[type].Add(currentTime + EffectiveDuration(type));
	}

	public void AddStacks(StatusEffectType type, int count)
	{
		for (int i = 0; i < count; i++) AddStack(type);
	}

	public float GetSpeedMultiplier()
	{
		return Mathf.Max(0.1f, 1f - GetStackCount(StatusEffectType.Slow) * SlowPerStack);
	}

	public float GetFireRateMultiplier()
	{
		return 1f + GetStackCount(StatusEffectType.ReducedFireRate) * FireRateReductionPerStack;
	}

	public float GetSpreadIncrease()
	{
		return GetStackCount(StatusEffectType.Blind) * SpreadPerStack;
	}

	public Color GetTint()
	{
		float weightSum = 0f;
		Vector3 accum = Vector3.Zero;
		foreach (var kv in stackExpiries) {
			int n = kv.Value.Count;
			if (n <= 0) continue;
			float ratio = Mathf.Min(1f, n / 10f);
			Color c = kv.Key switch {
				StatusEffectType.DamageOverTime => new Color(1f, 0.15f, 0.15f),
				StatusEffectType.Slow => new Color(0.3f, 0.5f, 1f),
				StatusEffectType.ReducedFireRate => new Color(1f, 0.95f, 0.2f),
				StatusEffectType.Blind => new Color(0.25f, 0.25f, 0.25f),
				_ => Colors.White,
			};
			accum += new Vector3(c.R, c.G, c.B) * ratio;
			weightSum += ratio;
		}
		if (weightSum <= 0f) return Colors.White;
		Vector3 avg = accum / weightSum;
		float intensity = Mathf.Min(1f, weightSum);
		return Colors.White.Lerp(new Color(avg.X, avg.Y, avg.Z), intensity);
	}

	public int Tick(float delta)
	{
		currentTime += delta;
		var keys = new List<StatusEffectType>(stackExpiries.Keys);
		foreach (var key in keys) {
			stackExpiries[key].RemoveAll(t => t < currentTime);
		}
		dotTickTimer += delta;
		if (dotTickTimer >= 1f) {
			dotTickTimer -= 1f;
			int dotStacks = GetStackCount(StatusEffectType.DamageOverTime);
			return Combat.DotTickDamage(dotStacks, DamagePerStackPerSecond);
		}
		return 0;
	}
}
