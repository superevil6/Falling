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
	private float dotTickTimer = 0f;
	private float currentTime = 0f;

	public int GetStackCount(StatusEffectType type)
	{
		return stackExpiries.TryGetValue(type, out var list) ? list.Count : 0;
	}

	public void AddStack(StatusEffectType type)
	{
		if (!stackExpiries.ContainsKey(type)) {
			stackExpiries[type] = new List<float>();
		}
		stackExpiries[type].Add(currentTime + StackDuration);
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
			return Mathf.RoundToInt(dotStacks * DamagePerStackPerSecond);
		}
		return 0;
	}
}
