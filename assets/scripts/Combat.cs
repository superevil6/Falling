using Godot;

// Pure damage arithmetic, extracted from Player / Enemy / StatusEffectController so the
// numbers can be unit-tested without a running Godot tree. Everything here is a pure
// function of its inputs: no node access, no RNG, no side effects. Callers keep the
// engine-facing work (RNG rolls, spawning damage text, playing sfx / animations).
//
// Uses Godot.Mathf deliberately so rounding / lerp semantics stay byte-identical to the
// original inline code (RoundToInt = banker's rounding, CeilToInt, single-precision
// Lerp). Do NOT swap these for System.Math — it changes edge-case results, which defeats
// the point of testing them.
public static class Combat
{
	public readonly struct PlayerHit
	{
		public readonly int HealthLoss;
		public readonly int ShieldAbsorbed;
		public readonly float RemainingShield;
		public PlayerHit(int healthLoss, int shieldAbsorbed, float remainingShield)
		{
			HealthLoss = healthLoss;
			ShieldAbsorbed = shieldAbsorbed;
			RemainingShield = remainingShield;
		}
	}

	// Player incoming damage: elemental defense stacks halve the hit per stack, then flat
	// DamageReduction subtracts, then the shield soaks what it can before health takes the rest.
	public static PlayerHit ResolvePlayerHit(int amount, int elementDefenseStacks, int damageReduction, float currentShield)
	{
		float dmg = amount;
		if (elementDefenseStacks > 0) dmg *= Mathf.Pow(0.5f, elementDefenseStacks);
		int finalDamage = Mathf.Max(0, Mathf.RoundToInt(dmg) - damageReduction);
		int absorbed = 0;
		float shield = currentShield;
		if (shield > 0f && finalDamage > 0) {
			absorbed = Mathf.Min(Mathf.CeilToInt(shield), finalDamage);
			shield = Mathf.Max(0f, shield - absorbed);
			finalDamage -= absorbed;
		}
		return new PlayerHit(finalDamage, absorbed, shield);
	}

	public readonly struct EnemyHit
	{
		public readonly int HealthLoss;
		public readonly int RemainingArmor;
		public EnemyHit(int healthLoss, int remainingArmor)
		{
			HealthLoss = healthLoss;
			RemainingArmor = remainingArmor;
		}
	}

	// Enemy incoming damage: element weakness doubles the hit / resist halves it, then flat
	// reduction subtracts, then armor halves what's left (rounded up) and soaks an
	// acid-boosted amount before breaking. armorPierce bullets ignore the shield entirely:
	// full damage passes through and the armor is left untouched.
	public static EnemyHit ResolveEnemyHit(int damage, bool isWeakness, bool isResisted, int effectiveReduction, int currentArmor, int acidStacks, bool armorPierce = false)
	{
		float dmg = damage;
		if (isWeakness) dmg *= 2f;
		else if (isResisted) dmg *= 0.5f;
		int finalDamage = Mathf.Max(0, Mathf.RoundToInt(dmg) - effectiveReduction);
		int armor = currentArmor;
		if (armor > 0 && !armorPierce) {
			finalDamage = Mathf.CeilToInt(finalDamage * 0.5f);
			int armorDmg = finalDamage * (1 + acidStacks);
			armor = Mathf.Max(0, armor - armorDmg);
		}
		return new EnemyHit(finalDamage, armor);
	}

	// Crit decision is split from crit damage so the RNG stays in the caller: pass rng.Randf()
	// as `roll` and the branch becomes deterministic and testable.
	public static bool RollsCrit(float criticalChance, float roll) => criticalChance > 0f && roll < criticalChance;
	public static int CritDamage(int baseDamage, float criticalMultiplier) => Mathf.RoundToInt(baseDamage * criticalMultiplier);

	// Charged-shot base damage: lerp between the gun's min and max by charge ratio (0..1).
	public static int ChargedBaseDamage(int minDamage, int maxDamage, float ratio) => Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, ratio));

	// Damage-over-time applied on a single tick given the active stack count.
	public static int DotTickDamage(int dotStacks, float damagePerStackPerSecond) => Mathf.RoundToInt(dotStacks * damagePerStackPerSecond);
}
