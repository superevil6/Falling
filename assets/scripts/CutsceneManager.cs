using Godot;

// Routes runtime cutscenes. Callers hand it a cutscene and the scene to load once the
// cutscene ends; it stashes those in statics (which survive the scene change) and
// switches to the shared CutscenePlayer scene. CutscenePlayer reads and clears them.
//
// The boot Intro scene does NOT go through here — it's a CutscenePlayer with its
// cutscene and next-scene authored directly as exported fields, so it plays on a cold
// launch when no statics are set.
public static class CutsceneManager
{
	public const string PlayerScenePath = "res://assets/objects/CutscenePlayer.tscn";

	// Set by Play(), consumed (and cleared) by CutscenePlayer._Ready.
	public static Cutscene Pending;
	public static string NextScenePath;

	// Switch to the cutscene player, showing `cutscene`, then loading `nextScenePath`.
	// If `cutscene` is null (or empty) this is a no-op-ish passthrough: it just loads
	// the next scene directly, so callers can funnel every transition through here.
	public static void Play(Node from, Cutscene cutscene, string nextScenePath)
	{
		if (from == null) return;
		if (cutscene == null || cutscene.Slides == null || cutscene.Slides.Length == 0) {
			from.GetTree().ChangeSceneToFile(nextScenePath);
			return;
		}
		Pending = cutscene;
		NextScenePath = nextScenePath;
		from.GetTree().ChangeSceneToFile(PlayerScenePath);
	}
}
