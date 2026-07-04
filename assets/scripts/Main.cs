using Godot;
using System.Linq;
using System.Threading.Tasks;

public partial class Main : Node2D
{
	private const string MainScenePath = "res://assets/objects/Main.tscn";

	// Called when the node enters the scene tree for the first time.
	[Export]
	public Stage[] Stages {get;set;}
	[Export]
	public int CurrentStage {get;set;} = 0;
	[Export]
	public float EnemyInputDriftSpeed {get;set;} = 50f;
	[Export]
	public PackedScene Player {get;set;}
	[Export]
	public GunUpgrade[] PossibleGunUpgrades {get;set;}
	[Export]
	public BodyUpgrade[] PossibleBodyUpgrades {get;set;}
	private int CurrentEnemyGroupWave = 0;
	private bool hasSpawnedFirstGroup = false;
	private bool spawningWave = false;
	private MusicPlayer music;
	private int bossCount = 0;
	// True while at least one boss is alive, used to suppress stage events.
	public bool BossActive => bossCount > 0;
	// Set once every boss segment is down; the boss-reward selection then advances stage.
	private bool awaitingStageAdvance = false;
	// Carries the next stage index across the scene reload. Instance fields reset to
	// their .tscn values on reload, so a static survives it.
	private static int pendingStage = -1;
	// Apply the carried-over stage before children (wall/background queues) read
	// CurrentStage in their own _Ready (which runs before Main._Ready).
	public override void _EnterTree()
	{
		if (pendingStage >= 0) { CurrentStage = pendingStage; pendingStage = -1; }
	}

	// Loads saved progress into the carriers so the next Main scene resumes it.
	// Call right before changing to the Main scene (e.g. from a "Continue" button).
	public static void PrepareContinue(SaveData data)
	{
		if (data == null) return;
		pendingStage = data.StageBeaten;
		global::Player.CarriedState = data.State;
		GameSession.Mode = data.Mode;
	}

	public override void _Ready()
	{
		music = new MusicPlayer();
		// Keep music playing (and its intro->loop transition running) while the game
		// is paused by the pause/upgrade/level-up menus.
		music.ProcessMode = ProcessModeEnum.Always;
		AddChild(music);
		var stage = CurrentStageData();
		if (stage != null) music.PlayMusic(stage.StageIntroMusic, stage.StageMusic);
		AddChild(new StageDirector());
		var selectionMenu = GetNodeOrNull<SelectionMenu>("Selection Menu");
		if (selectionMenu != null) selectionMenu.SelectionMade += OnBossRewardSelected;
		// Show the stage name for a few seconds before the first enemies arrive.
		StageIntroThenSpawn();
		Player player = Player.Instantiate<Player>();
		AddChild(player);
		player.Position = new Vector2(500,500);
		// Restore the carried-over loadout/upgrades after advancing from a prior stage.
		if (global::Player.CarriedState != null) {
			player.RestoreState(global::Player.CarriedState);
			global::Player.CarriedState = null;
		}
	}

	public Stage CurrentStageData()
	{
		if (Stages == null || CurrentStage < 0 || CurrentStage >= Stages.Length) return null;
		return Stages[CurrentStage];
	}

	// Called by an Enemy with IsBoss when it dies. Once the last boss segment is down,
	// show the reward menu (advancing the stage after a pick); if no menu can open
	// (e.g. player's slots are full) advance straight away so progress isn't blocked.
	public void OnBossDefeated()
	{
		bossCount = Mathf.Max(0, bossCount - 1);
		if (bossCount > 0) return;
		awaitingStageAdvance = true;
		var menu = GetNodeOrNull<SelectionMenu>("Selection Menu");
		bool opened = menu != null && menu.Open();
		if (!opened) OnBossRewardSelected(0);
	}

	// Fired when the player confirms a boss-reward pick (or immediately if no menu
	// opened). Advances to the next stage when a boss fight has just been cleared.
	private void OnBossRewardSelected(int index)
	{
		if (!awaitingStageAdvance) return;
		awaitingStageAdvance = false;
		AdvanceToNextStage();
	}

	private void AdvanceToNextStage()
	{
		// The reward menu paused the game; clear that or the next scene starts frozen.
		GetTree().Paused = false;
		Engine.TimeScale = 1f;
		int next = CurrentStage + 1;
		if (Stages != null && next < Stages.Length) {
			// Carry the player's loadout/upgrades across the reload, and persist progress.
			var player = GetNodeOrNull<Player>("Player");
			if (player != null) {
				global::Player.CarriedState = player.CaptureState();
				SaveSystem.Save(next, GameSession.Mode, player);
			}
			pendingStage = next; // re-read by the reloaded Main._Ready
			// If the next stage has a cutscene, play it first; it loads Main afterward.
			// (pendingStage is static, so it survives the trip through the cutscene.)
			var nextCutscene = Stages[next]?.Cutscene;
			if (nextCutscene?.Slides != null && nextCutscene.Slides.Length > 0) {
				CutsceneManager.Play(this, nextCutscene, MainScenePath);
			} else {
				GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
			}
		} else {
			// No further stages — run complete; drop any carry and return to the title.
			global::Player.CarriedState = null;
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://assets/objects/TitleScreen.tscn");
		}
	}

	// Displays the current stage's name in large white text at screen centre for a
	// few seconds, then kicks off the first enemy wave. The player is spawned
	// separately in _Ready, so only enemy spawning is gated behind the intro.
	private async void StageIntroThenSpawn()
	{
		var stage = CurrentStageData();
		if (stage != null && !string.IsNullOrEmpty(stage.Name)) {
			CanvasLayer intro = ShowStageName(stage.Name);
			await ToSignal(GetTree().CreateTimer(5.0f), "timeout");
			intro?.QueueFree();
		}
		SpawnEnemyGroup();
	}

	private CanvasLayer ShowStageName(string name)
	{
		var layer = new CanvasLayer { Layer = 100 };
		var label = new Label { Text = name };
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
		label.AddThemeConstantOverride("outline_size", 12);
		label.AddThemeFontSizeOverride("font_size", 120);
		layer.AddChild(label);
		AddChild(layer);
		// Fade in, hold, then fade out over the intro's lifetime.
		label.Modulate = new Color(1f, 1f, 1f, 0f);
		Tween tween = label.CreateTween();
		tween.TweenProperty(label, "modulate:a", 1.0f, 0.6f);
		tween.TweenInterval(3.5f);
		tween.TweenProperty(label, "modulate:a", 0.0f, 0.9f);
		return layer;
	}

	// Fades out the stage music and flashes a red WARNING across the screen, then
	// starts the boss music. Awaited just before a boss is spawned.
	private async Task BossWarningSequence()
	{
		music?.FadeOut(1.5f);
		CanvasLayer warning = ShowBossWarning();
		await ToSignal(GetTree().CreateTimer(5.0f), "timeout");
		warning?.QueueFree();
		var stage = CurrentStageData();
		if (stage != null && music != null) music.PlayMusic(stage.BossIntroMusic, stage.BossMusic);
	}

	private CanvasLayer ShowBossWarning()
	{
		var layer = new CanvasLayer { Layer = 100 };
		var label = new Label { Text = "WARNING" };
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AddThemeColorOverride("font_color", new Color(1f, 0f, 0f));
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
		label.AddThemeConstantOverride("outline_size", 12);
		label.AddThemeFontSizeOverride("font_size", 140);
		layer.AddChild(label);
		AddChild(layer);
		// Blink the text for the duration of the warning.
		Tween tween = label.CreateTween();
		tween.SetLoops();
		tween.TweenProperty(label, "modulate:a", 0.15f, 0.35f);
		tween.TweenProperty(label, "modulate:a", 1.0f, 0.35f);
		return layer;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{


	}

	public async void SpawnEnemyGroup () {
		if (spawningWave) return;
		spawningWave = true;
		try {
			var groups = Stages[CurrentStage].EnemyGroup;
			if (groups == null || groups.Length == 0) return;
			if (hasSpawnedFirstGroup) {
				var previous = groups[CurrentEnemyGroupWave];
				if (previous != null && previous.TimeBeforeNextGroup > 0) {
					await ToSignal(GetTree().CreateTimer(previous.TimeBeforeNextGroup), "timeout");
				}
				CurrentEnemyGroupWave = (CurrentEnemyGroupWave + 1) % groups.Length;
			}
			hasSpawnedFirstGroup = true;
			var group = groups[CurrentEnemyGroupWave];
			if (group == null || group.Enemies == null) return;
			for (int i = 0; i < group.Enemies.Count(); i++) {
				Enemy e = group.Enemies[i].Instantiate<Enemy>();
				e.Position = group.SpawnLocations[i];
				e.Set("PostSpawnDestination", group.PostSpawnDestination[i]);
				if (e.IsBoss) {
					await BossWarningSequence();
					bossCount++;
				}
				AddChild(e);
				await ToSignal(GetTree().CreateTimer(group.TimeBetweenSpawns), "timeout");
			}
		} finally {
			spawningWave = false;
		}
	}
}