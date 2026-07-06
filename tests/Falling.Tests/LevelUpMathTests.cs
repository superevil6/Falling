using Xunit;

// Pins the pick-count arithmetic behind the "Foresight" head upgrade: applying it bumps
// the player's PickCountBonus, which the level-up menu adds on top of its base PickCount.
public class LevelUpMathTests
{
	// ---- PickCountBonusIncrement --------------------------------------------------------

	[Fact]
	public void PickCountIncrement_NormalValueRoundsToInt()
	{
		Assert.Equal(1, LevelUpMath.PickCountBonusIncrement(1f));
		Assert.Equal(2, LevelUpMath.PickCountBonusIncrement(2f));
	}

	[Theory]
	[InlineData(0f)]    // resource left Value unset...
	[InlineData(0.4f)]  // ...or fractional below 0.5
	[InlineData(-3f)]   // ...or negative
	public void PickCountIncrement_AlwaysGrantsAtLeastOne(float value)
	{
		Assert.Equal(1, LevelUpMath.PickCountBonusIncrement(value));
	}

	[Fact]
	public void PickCountIncrement_UsesBankersRounding()
	{
		// Godot's RoundToInt rounds .5 to the nearest even int: 2.5 -> 2, 3.5 -> 4.
		Assert.Equal(2, LevelUpMath.PickCountBonusIncrement(2.5f));
		Assert.Equal(4, LevelUpMath.PickCountBonusIncrement(3.5f));
	}

	// ---- EffectivePickCount -------------------------------------------------------------

	[Fact]
	public void EffectivePickCount_NoBonusIsJustTheBase()
	{
		Assert.Equal(4, LevelUpMath.EffectivePickCount(basePickCount: 4, pickCountBonus: 0));
	}

	[Theory]
	[InlineData(4, 1, 5)]
	[InlineData(4, 3, 7)]
	public void EffectivePickCount_AddsBonusToBase(int basePickCount, int bonus, int expected)
	{
		Assert.Equal(expected, LevelUpMath.EffectivePickCount(basePickCount, bonus));
	}

	[Fact]
	public void EffectivePickCount_NeverDropsBelowOne()
	{
		Assert.Equal(1, LevelUpMath.EffectivePickCount(basePickCount: 0, pickCountBonus: 0));
		Assert.Equal(1, LevelUpMath.EffectivePickCount(basePickCount: 1, pickCountBonus: -5));
	}

	// One Foresight pick (Value = 1) applied on top of the default menu PickCount of 4.
	[Fact]
	public void EffectivePickCount_OneForesightAddsOneChoice()
	{
		int bonus = LevelUpMath.PickCountBonusIncrement(1f);
		Assert.Equal(5, LevelUpMath.EffectivePickCount(basePickCount: 4, pickCountBonus: bonus));
	}
}
