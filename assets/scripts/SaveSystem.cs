using Godot;

// Persists run progress (stage reached + player loadout) to disk, one save per mode.
public static class SaveSystem
{
	private static string PathFor(GameMode mode) => $"user://savegame_{mode}.tres";

	public static void Save(int stageBeaten, GameMode mode, Player player)
	{
		if (player == null) return;
		var data = new SaveData {
			StageBeaten = stageBeaten,
			Mode = mode,
			State = player.CaptureState(),
		};
		Error err = ResourceSaver.Save(data, PathFor(mode));
		if (err != Error.Ok) GD.PrintErr($"SaveSystem: failed to save ({err})");
	}

	public static bool HasSave(GameMode mode) => FileAccess.FileExists(PathFor(mode));

	public static SaveData Load(GameMode mode)
	{
		if (!HasSave(mode)) return null;
		// Replace cache so we always get the latest file from disk.
		return ResourceLoader.Load<SaveData>(PathFor(mode), null, ResourceLoader.CacheMode.Replace);
	}

	public static void Delete(GameMode mode)
	{
		if (HasSave(mode)) DirAccess.RemoveAbsolute(PathFor(mode));
	}
}
