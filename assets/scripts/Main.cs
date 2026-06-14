using Godot;
using System.Linq;

public partial class Main : Node2D
{
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
		SpawnEnemyGroup();
		Player player = Player.Instantiate<Player>();
		AddChild(player);
		player.Position = new Vector2(500,500);
	}

	public Stage CurrentStageData()
	{
		if (Stages == null || CurrentStage < 0 || CurrentStage >= Stages.Length) return null;
		return Stages[CurrentStage];
	}

	// Called by an Enemy with IsBoss when it dies, to clear the boss-active state.
	public void OnBossDefeated()
	{
		bossCount = Mathf.Max(0, bossCount - 1);
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
				AddChild(e);
				if (e.IsBoss) {
					bossCount++;
					var stage = CurrentStageData();
					if (stage != null && music != null) music.PlayMusic(stage.BossIntroMusic, stage.BossMusic);
				}
				await ToSignal(GetTree().CreateTimer(group.TimeBetweenSpawns), "timeout");
			}
		} finally {
			spawningWave = false;
		}
	}
}