public enum BossActionType
{
	// Fire one volley of the action's gun (or the boss's main gun) at the player.
	Fire,
	// Show an attack-warning indicator for the action's Duration (a wind-up).
	Telegraph,
	// Spawn the action's minion scenes at the given offsets.
	Summon,
	// Change the boss's movement mode for the rest of the phase.
	Move,
	// Do nothing for the action's Duration (a pause between attacks).
	Wait,
	// Launch a small beam upward, then rain large telegraphed lasers from the top.
	RainLasers,
	// Drop a single large damaging mine at the boss's position.
	DropMine,
}
