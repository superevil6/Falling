using Godot;

// Fire-and-forget one-shot sound effects. Spawns a temporary AudioStreamPlayer under
// the current scene (so the sound survives the emitter being freed) and frees it when
// it finishes. Also exposes the shared UI / hit sounds.
public static class Sfx
{
	private static AudioStream selectSound;
	private static AudioStream cancelSound;
	private static AudioStream hitSound;
	private static ulong lastHitMs;
	private const ulong HitThrottleMs = 40; // cap rapid multi-hit spam

	public static void Play(Node context, AudioStream stream, float volumeDb = 0f, float pitch = 1f)
	{
		if (stream == null || context == null) return;
		SceneTree tree = context.GetTree();
		if (tree == null) return;
		Node parent = tree.CurrentScene ?? (Node)tree.Root;
		if (parent == null) return;
		var player = new AudioStreamPlayer();
		player.Stream = stream;
		player.VolumeDb = volumeDb;
		player.PitchScale = pitch;
		// Always: keep playing even while menus pause the game (so UI sounds are heard).
		player.ProcessMode = Node.ProcessModeEnum.Always;
		parent.AddChild(player);
		player.Play();
		player.Finished += player.QueueFree;
	}

	public static void PlaySelect(Node context) =>
		Play(context, selectSound ??= GD.Load<AudioStream>("res://assets/SFX/Select.wav"));

	public static void PlayCancel(Node context) =>
		Play(context, cancelSound ??= GD.Load<AudioStream>("res://assets/SFX/Cancel.wav"));

	// Shared hit sound for player/enemy damage. Throttled so a barrage of hits in the
	// same instant doesn't flood the audio channels.
	public static void PlayHit(Node context)
	{
		ulong now = Time.GetTicksMsec();
		if (now - lastHitMs < HitThrottleMs) return;
		lastHitMs = now;
		Play(context, hitSound ??= GD.Load<AudioStream>("res://assets/SFX/Hit.wav"));
	}
}
