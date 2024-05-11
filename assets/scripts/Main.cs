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
	public PackedScene Player {get;set;}
	private int CurrentEnemyGroupWave = 0;
	public override void _Ready()
	{
		SpawnEnemyGroup();
		Player player = Player.Instantiate<Player>();
		player.Position = new Vector2(500,500).Normalized();
		AddChild(player);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		

	}

	public async void SpawnEnemyGroup () {
		GD.Print("Spawn");
		for (int i = 0; i < Stages[CurrentStage].EnemyGroup[CurrentEnemyGroupWave].Enemies.Count(); i++) {
			Enemy e = Stages[CurrentStage].EnemyGroup[CurrentEnemyGroupWave].Enemies[i].Instantiate<Enemy>();
			e.Position = Stages[CurrentStage].EnemyGroup[CurrentEnemyGroupWave].SpawnLocations[i];
			e.Set("PostSpawnDestination", Stages[CurrentStage].EnemyGroup[CurrentEnemyGroupWave].PostSpawnDestination[i]);
			AddChild(e);
			await ToSignal(GetTree().CreateTimer(Stages[CurrentStage].EnemyGroup[CurrentEnemyGroupWave].TimeBetweenSpawns), "timeout");
		}
	}
}
