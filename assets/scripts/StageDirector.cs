using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

// Runtime scheduler for a stage's timed events. Reads its config from the current
// Stage resource (via Main) and fires one event every EventInterval seconds, either
// sequentially or at random. Events are suppressed while a boss is active if the
// stage opts in. Lives as a child of Main; pausable, so it halts during menus.
public partial class StageDirector : Node
{
	private float timer;
	private bool running;
	private int sequentialIndex;
	private readonly RandomNumberGenerator rng = new RandomNumberGenerator();
	private static PackedScene indicatorScene;

	private Vector2 ViewportSize => GetViewport().GetVisibleRect().Size;

	public override void _Ready()
	{
		rng.Randomize();
		if (indicatorScene == null) {
			indicatorScene = GD.Load<PackedScene>("res://assets/objects/SpawnIndicator.tscn");
		}
		var stage = (GetParent() as Main)?.CurrentStageData();
		timer = stage != null ? Mathf.Max(0.1f, stage.EventInterval) : 1f;
	}

	public override void _Process(double delta)
	{
		if (running) return;
		var main = GetParent() as Main;
		var stage = main?.CurrentStageData();
		if (stage?.StageEvents == null || stage.StageEvents.Length == 0) return;
		// Freeze the timer (don't just skip firing) while a boss suppresses events,
		// so the next event arrives a full interval after the fight ends.
		if (stage.DisableEventsDuringBoss && main.BossActive) return;

		timer -= (float)delta;
		if (timer <= 0f) FireNextEvent(stage);
	}

	private async void FireNextEvent(Stage stage)
	{
		running = true;
		try {
			StageEvent e = SelectEvent(stage);
			if (e != null) await ExecuteEvent(e);
		} finally {
			running = false;
			timer = Mathf.Max(0.1f, stage.EventInterval);
		}
	}

	private StageEvent SelectEvent(Stage stage)
	{
		var events = stage.StageEvents;
		if (events.Length == 0) return null;
		if (stage.RandomEventOrder) return events[rng.RandiRange(0, events.Length - 1)];
		StageEvent e = events[sequentialIndex % events.Length];
		sequentialIndex = (sequentialIndex + 1) % events.Length;
		return e;
	}

	private async Task ExecuteEvent(StageEvent e)
	{
		switch (e.Type) {
			case StageEventType.WallContract:
				await ExecuteWallContract(e);
				break;
			case StageEventType.SpawnObstacle:
				await ExecuteSpawnObstacle(e);
				break;
		}
	}

	private async Task ExecuteWallContract(StageEvent e)
	{
		List<WallQueue> queues = SelectQueues(e.Side);
		if (queues.Count == 0) return;
		float height = ViewportSize.Y;
		foreach (var q in queues) {
			SpawnWarningAt(new Vector2(q.Position.X, height * 0.5f), e.WarningDuration, new Color(1f, 0.4f, 0.2f));
		}
		if (e.WarningDuration > 0f) await Wait(e.WarningDuration);

		float delta = e.ContractFraction * ViewportSize.X;
		foreach (var q in queues) q.AnimateEdgeOffset(q.BaseEdgeOffset + delta, e.ContractDuration);

		if (e.Permanent) {
			await Wait(e.ContractDuration);
			foreach (var q in queues) q.CommitEdgeOffset();
		} else {
			await Wait(e.ContractDuration + e.HoldDuration);
			foreach (var q in queues) q.AnimateEdgeOffset(q.BaseEdgeOffset, e.ContractDuration);
			await Wait(e.ContractDuration);
		}
	}

	private async Task ExecuteSpawnObstacle(StageEvent e)
	{
		if (e.ObstacleScenes == null || e.ObstacleScenes.Length == 0) return;
		Vector2 size = ViewportSize;
		var spots = new List<Vector2>();
		int count = Mathf.Max(1, e.ObstacleCount);
		for (int i = 0; i < count; i++) {
			float x = rng.RandfRange(size.X * 0.1f, size.X * 0.9f);
			spots.Add(new Vector2(x, size.Y + 100f));
			SpawnWarningAt(new Vector2(x, size.Y - 80f), e.WarningDuration, new Color(0.3f, 0.6f, 1f));
		}
		if (e.WarningDuration > 0f) await Wait(e.WarningDuration);
		foreach (var spot in spots) {
			var scene = e.ObstacleScenes[rng.RandiRange(0, e.ObstacleScenes.Length - 1)];
			if (scene == null) continue;
			var obs = scene.Instantiate<Node2D>();
			obs.GlobalPosition = spot;
			GetParent().AddChild(obs);
		}
	}

	private List<WallQueue> SelectQueues(WallSide side)
	{
		var list = new List<WallQueue>();
		var left = GetParent()?.GetNodeOrNull<WallQueue>("Left Wall Queue");
		var right = GetParent()?.GetNodeOrNull<WallQueue>("Right Wall Queue");
		WallSide resolved = side == WallSide.Random
			? (rng.Randf() < 0.5f ? WallSide.Left : WallSide.Right)
			: side;
		if ((resolved == WallSide.Left || resolved == WallSide.Both) && left != null) list.Add(left);
		if ((resolved == WallSide.Right || resolved == WallSide.Both) && right != null) list.Add(right);
		return list;
	}

	private void SpawnWarningAt(Vector2 position, float duration, Color color)
	{
		if (indicatorScene == null || duration <= 0f) return;
		var ind = indicatorScene.Instantiate<SpawnIndicator>();
		ind.Duration = duration;
		ind.TriangleColor = color;
		ind.GlobalPosition = position;
		GetParent().AddChild(ind);
	}

	private SignalAwaiter Wait(float seconds)
	{
		// processAlways: false so these timers pause with the game (menus), keeping
		// the event sequence in sync with the pausable wall tweens.
		return ToSignal(GetTree().CreateTimer(Mathf.Max(0.01f, seconds), false), SceneTreeTimer.SignalName.Timeout);
	}
}
