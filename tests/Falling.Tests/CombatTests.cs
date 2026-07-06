using Godot;
using Xunit;

// Regression tests pinning the damage arithmetic in Combat. These lock in the CURRENT
// behavior — including Godot's banker's rounding (RoundToInt rounds .5 to the nearest
// even int: 2.5 -> 2, 7.5 -> 8). If you intend to change a formula, update the expected
// value here on purpose; an accidental change should make one of these go red.
public class CombatTests
{
	// ---- Player incoming damage ----------------------------------------------------

	[Fact]
	public void PlayerHit_PlainDamage_NoMitigation()
	{
		var hit = Combat.ResolvePlayerHit(amount: 10, elementDefenseStacks: 0, damageReduction: 0, currentShield: 0f);
		Assert.Equal(10, hit.HealthLoss);
		Assert.Equal(0, hit.ShieldAbsorbed);
		Assert.Equal(0f, hit.RemainingShield);
	}

	[Fact]
	public void PlayerHit_FlatReduction_Subtracts()
	{
		Assert.Equal(7, Combat.ResolvePlayerHit(10, 0, 3, 0f).HealthLoss);
	}

	[Fact]
	public void PlayerHit_ReductionNeverGoesNegative()
	{
		Assert.Equal(0, Combat.ResolvePlayerHit(10, 0, 15, 0f).HealthLoss);
	}

	[Theory]
	[InlineData(1, 5)]  // 10 * 0.5   = 5
	[InlineData(2, 2)]  // 10 * 0.25  = 2.5 -> 2 (banker's)
	[InlineData(3, 1)]  // 10 * 0.125 = 1.25 -> 1
	public void PlayerHit_ElementalDefenseStacksHalvePerStack(int stacks, int expected)
	{
		Assert.Equal(expected, Combat.ResolvePlayerHit(10, stacks, 0, 0f).HealthLoss);
	}

	[Fact]
	public void PlayerHit_ShieldAbsorbsBeforeHealth()
	{
		var hit = Combat.ResolvePlayerHit(10, 0, 0, currentShield: 4f);
		Assert.Equal(4, hit.ShieldAbsorbed);
		Assert.Equal(6, hit.HealthLoss);
		Assert.Equal(0f, hit.RemainingShield);
	}

	[Fact]
	public void PlayerHit_PartialShieldRoundsUpAndClampsToZero()
	{
		// ceil(3.5) = 4 absorbed even though only 3.5 shield existed; remainder clamps to 0.
		var hit = Combat.ResolvePlayerHit(10, 0, 0, currentShield: 3.5f);
		Assert.Equal(4, hit.ShieldAbsorbed);
		Assert.Equal(6, hit.HealthLoss);
		Assert.Equal(0f, hit.RemainingShield);
	}

	[Fact]
	public void PlayerHit_ShieldLargerThanDamageSoaksItAll()
	{
		var hit = Combat.ResolvePlayerHit(3, 0, 0, currentShield: 10f);
		Assert.Equal(3, hit.ShieldAbsorbed);
		Assert.Equal(0, hit.HealthLoss);
		Assert.Equal(7f, hit.RemainingShield);
	}

	// ---- Enemy incoming damage -----------------------------------------------------

	[Theory]
	[InlineData(false, false, 10)] // neutral
	[InlineData(true, false, 20)]  // weakness doubles
	[InlineData(false, true, 5)]   // resist halves
	public void EnemyHit_ElementMultiplier(bool weak, bool resist, int expected)
	{
		var hit = Combat.ResolveEnemyHit(10, weak, resist, effectiveReduction: 0, currentArmor: 0, acidStacks: 0);
		Assert.Equal(expected, hit.HealthLoss);
	}

	[Fact]
	public void EnemyHit_ReductionThenNoArmor()
	{
		Assert.Equal(6, Combat.ResolveEnemyHit(10, false, false, 4, 0, 0).HealthLoss);
	}

	[Fact]
	public void EnemyHit_ArmorHalvesDamageAndSoaksIt()
	{
		// ceil(10 * 0.5) = 5 to health; armor loses 5 * (1 + 0) = 5.
		var hit = Combat.ResolveEnemyHit(10, false, false, 0, currentArmor: 100, acidStacks: 0);
		Assert.Equal(5, hit.HealthLoss);
		Assert.Equal(95, hit.RemainingArmor);
	}

	[Fact]
	public void EnemyHit_AcidStacksMultiplyArmorDamageOnly()
	{
		// health still 5, but armor loses 5 * (1 + 2) = 15.
		var hit = Combat.ResolveEnemyHit(10, false, false, 0, currentArmor: 100, acidStacks: 2);
		Assert.Equal(5, hit.HealthLoss);
		Assert.Equal(85, hit.RemainingArmor);
	}

	[Fact]
	public void EnemyHit_ArmorHalvingRoundsUp()
	{
		// ceil(5 * 0.5) = ceil(2.5) = 3.
		Assert.Equal(3, Combat.ResolveEnemyHit(5, false, false, 0, currentArmor: 100, acidStacks: 0).HealthLoss);
	}

	[Fact]
	public void EnemyHit_ArmorBreaksToZero()
	{
		var hit = Combat.ResolveEnemyHit(10, false, false, 0, currentArmor: 3, acidStacks: 0);
		Assert.Equal(0, hit.RemainingArmor);
	}

	[Fact]
	public void EnemyHit_ArmorPierceIgnoresShield_FullDamageArmorUntouched()
	{
		// Piercing bullet: no halving (full 10 to health) and the armor is left intact.
		var hit = Combat.ResolveEnemyHit(10, false, false, 0, currentArmor: 100, acidStacks: 0, armorPierce: true);
		Assert.Equal(10, hit.HealthLoss);
		Assert.Equal(100, hit.RemainingArmor);
	}

	[Fact]
	public void EnemyHit_ArmorPierceStillAppliesReductionAndElement()
	{
		// Pierce only removes the armor step; flat reduction and resist still apply.
		// resist halves 10 -> 5, then reduction 2 -> 3, armor ignored.
		var hit = Combat.ResolveEnemyHit(10, false, true, effectiveReduction: 2, currentArmor: 100, acidStacks: 0, armorPierce: true);
		Assert.Equal(3, hit.HealthLoss);
		Assert.Equal(100, hit.RemainingArmor);
	}

	// ---- Crit ----------------------------------------------------------------------

	[Theory]
	[InlineData(0f, 0f, false)]   // zero chance never crits, even on a 0 roll
	[InlineData(0.5f, 0.4f, true)]
	[InlineData(0.5f, 0.5f, false)] // roll must be strictly below chance
	[InlineData(0.5f, 0.6f, false)]
	public void RollsCrit_ComparesRollToChance(float chance, float roll, bool expected)
	{
		Assert.Equal(expected, Combat.RollsCrit(chance, roll));
	}

	[Theory]
	[InlineData(10, 2f, 20)]
	[InlineData(5, 1.5f, 8)]   // 7.5 -> 8 (banker's)
	[InlineData(1, 2.5f, 2)]   // 2.5 -> 2 (banker's)
	public void CritDamage_RoundsProduct(int baseDamage, float mult, int expected)
	{
		Assert.Equal(expected, Combat.CritDamage(baseDamage, mult));
	}

	// ---- Charged shot & DoT --------------------------------------------------------

	[Theory]
	[InlineData(0f, 0)]     // lerp(0,10,0)    = 0
	[InlineData(1f, 10)]    // lerp(0,10,1)    = 10
	[InlineData(0.5f, 5)]   // lerp(0,10,0.5)  = 5
	[InlineData(0.25f, 2)]  // lerp(0,10,0.25) = 2.5 -> 2 (banker's)
	public void ChargedBaseDamage_LerpsMinToMax(float ratio, int expected)
	{
		Assert.Equal(expected, Combat.ChargedBaseDamage(minDamage: 0, maxDamage: 10, ratio: ratio));
	}

	[Theory]
	[InlineData(3, 1f, 3)]
	[InlineData(3, 0.5f, 2)]  // 1.5 -> 2 (banker's)
	[InlineData(5, 0.5f, 2)]  // 2.5 -> 2 (banker's)
	[InlineData(1, 0.5f, 0)]  // 0.5 -> 0 (banker's)
	public void DotTickDamage_RoundsStacksTimesRate(int stacks, float rate, int expected)
	{
		Assert.Equal(expected, Combat.DotTickDamage(stacks, rate));
	}

	// ---- Blind: aim spread & darkening --------------------------------------------

	[Fact]
	public void BlindSpread_ZeroWhenNotBlinded()
	{
		var sec = new StatusEffectController();
		Assert.Equal(0f, sec.GetBlindSpreadDegrees());
	}

	[Fact]
	public void BlindSpread_ScalesWithStacks()
	{
		var sec = new StatusEffectController();
		sec.AddStacks(StatusEffectType.Blind, 3);
		Assert.Equal(3 * sec.BlindSpreadDegreesPerStack, sec.GetBlindSpreadDegrees());
	}

	[Fact]
	public void BlindSpread_CapsAtMax()
	{
		var sec = new StatusEffectController();
		sec.AddStacks(StatusEffectType.Blind, 1000);
		Assert.Equal(sec.MaxBlindSpreadDegrees, sec.GetBlindSpreadDegrees());
	}

	[Fact]
	public void BlindTint_DarkensTowardBlackWithStacks()
	{
		var sec = new StatusEffectController();
		Assert.Equal(Colors.White, sec.GetTint());
		sec.AddStacks(StatusEffectType.Blind, 3);
		float mid = sec.GetTint().R;
		sec.AddStacks(StatusEffectType.Blind, 3);
		float darker = sec.GetTint().R;
		Assert.True(mid < 1f, "some darkening at 3 stacks");
		Assert.True(darker < mid, "more stacks = darker");
	}

	[Fact]
	public void BlindTint_ReachesBlackAtFullStacks()
	{
		var sec = new StatusEffectController();
		sec.AddStacks(StatusEffectType.Blind, 10);
		Assert.Equal(Colors.Black, sec.GetTint());
	}
}
