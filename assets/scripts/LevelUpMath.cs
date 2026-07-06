using Godot;

// Pure level-up arithmetic, extracted from Player / LevelUpMenu so the numbers can be
// unit-tested without a running Godot tree. Everything here is a pure function of its
// inputs: no node access, no RNG, no side effects. Callers keep the engine-facing work.
//
// Uses Godot.Mathf deliberately so rounding stays byte-identical to the original inline
// code (RoundToInt = banker's rounding). Do NOT swap for System.Math.
public static class LevelUpMath
{
	// How much a PickCount body upgrade adds to the player's running bonus. Always grants
	// at least 1 extra choice even if the resource's Value is 0 or fractional.
	public static int PickCountBonusIncrement(float upgradeValue)
		=> Mathf.Max(1, Mathf.RoundToInt(upgradeValue));

	// The number of options a level-up should offer: the menu's base PickCount plus any
	// bonus the player has earned. Never drops below 1 so a level-up always shows a choice.
	public static int EffectivePickCount(int basePickCount, int pickCountBonus)
		=> Mathf.Max(1, basePickCount + pickCountBonus);
}
