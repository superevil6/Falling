using Godot;
using System.Collections.Generic;

// Drives advanced boss behavior by composition: attach this as a child of a boss
// Enemy and assign Phases in the editor. It takes over the boss's attacks
// (ExternalAttackControl) and runs the active phase's action pattern, calling the
// Enemy's public primitives (FireGun / ShowTelegraph / SummonMinion).
public partial class BossController : Node2D
{
	[Export]
	public BossPhase[] Phases {get;set;}

	private Enemy boss;
	private int phaseIndex = -1;
	private int actionIndex = 0;
	private float actionTimer = 0f;
	private readonly List<Enemy> minions = new List<Enemy>();
	private bool minionsCleared = false;

	public override void _Ready()
	{
		boss = GetParent<Enemy>();
		if (boss != null) boss.ExternalAttackControl = true;
	}

	public override void _Process(double delta)
	{
		if (boss == null || Phases == null || Phases.Length == 0) return;
		// When the boss dies, kill off any minions it summoned (once).
		if (boss.CurrentHealth <= 0) {
			if (!minionsCleared) {
				ClearMinions();
				minionsCleared = true;
			}
			return;
		}
		// Wait until the spawn animation has finished before attacking.
		if (!boss.SpawnComplete) return;

		UpdatePhase();
		if (phaseIndex < 0) return;
		var phase = Phases[phaseIndex];
		if (phase?.Actions == null || phase.Actions.Length == 0) return;

		actionTimer -= (float)delta;
		if (actionTimer <= 0f) {
			var action = phase.Actions[actionIndex];
			ExecuteAction(action);
			actionTimer = Mathf.Max(0.01f, action?.Duration ?? 0.5f);
			actionIndex = (actionIndex + 1) % phase.Actions.Length;
		}
	}

	// Selects the active phase: the last phase whose threshold is still >= the
	// boss's current health fraction. Resets the pattern when the phase changes.
	private void UpdatePhase()
	{
		float ratio = boss.HealthFraction;
		int target = phaseIndex < 0 ? 0 : phaseIndex;
		for (int i = 0; i < Phases.Length; i++) {
			if (Phases[i] != null && ratio <= Phases[i].HealthThreshold) target = i;
		}
		if (target != phaseIndex) {
			phaseIndex = target;
			actionIndex = 0;
			actionTimer = 0f;
			ApplyPhase(Phases[phaseIndex]);
		}
	}

	private void ApplyPhase(BossPhase phase)
	{
		if (phase == null || !phase.OverrideMovement) return;
		boss.MovementType = phase.MovementType;
		if (phase.MovementSpeed > 0f) boss.MovementSpeed = phase.MovementSpeed;
	}

	private void ExecuteAction(BossAction action)
	{
		if (action == null) return;
		switch (action.Type) {
			case BossActionType.Fire:
				boss.FireGun(action.OverrideGun ?? boss.Gun, action.AimOffsetDegrees);
				break;
			case BossActionType.Telegraph:
				boss.ShowTelegraph(action.Duration);
				break;
			case BossActionType.Summon:
				Summon(action);
				break;
			case BossActionType.Move:
				boss.MovementType = action.MovementType;
				break;
			case BossActionType.Wait:
				break;
		}
	}

	private void Summon(BossAction action)
	{
		if (action.SummonScenes == null) return;
		for (int i = 0; i < action.SummonScenes.Length; i++) {
			Vector2 offset = (action.SummonOffsets != null && i < action.SummonOffsets.Length)
				? action.SummonOffsets[i] : Vector2.Zero;
			Enemy minion = boss.SummonMinion(action.SummonScenes[i], offset);
			if (minion != null) minions.Add(minion);
		}
	}

	// Kills any still-living summoned minions through the normal death pipeline.
	private void ClearMinions()
	{
		foreach (var minion in minions) {
			if (IsInstanceValid(minion) && minion.CurrentHealth > 0) {
				minion.CurrentHealth = 0;
				minion.StartDying();
			}
		}
		minions.Clear();
	}
}
