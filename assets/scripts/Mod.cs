using Godot;

public partial class Mod : Resource
{
	[Export]
	public string Name { get; set; }
	[Export]
	public int Level { get; set; }
	[Export]
	public int CurrentExperience { get; set; }
	[Export]
	public int Value1 { get; set; }
	[Export]
	public int Value2 { get; set; }
	[Export]
	public int ExperiencePerLevel { get; set; } = 10;

	public void AddExperience(int amount)
	{
		CurrentExperience += amount;
		while (ExperiencePerLevel > 0 && CurrentExperience >= ExperiencePerLevel) {
			CurrentExperience -= ExperiencePerLevel;
			Level++;
		}
	}
}
