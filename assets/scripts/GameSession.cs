// Holds run-wide state that must survive scene changes/reloads. Static so it persists
// across the title screen -> gameplay transition.
public static class GameSession
{
	public static GameMode Mode = GameMode.Story;
}
