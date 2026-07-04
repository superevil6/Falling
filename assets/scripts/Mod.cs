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
	[Export]
	public int ExperienceCurveStep { get; set; } = 5;

	public void AddExperience(int amount)
	{
		CurrentExperience += amount;
		int needed = Helpers.ExperienceForLevel(Level, ExperiencePerLevel, ExperienceCurveStep);
		while (needed > 0 && CurrentExperience >= needed) {
			CurrentExperience -= needed;
			Level++;
			needed = Helpers.ExperienceForLevel(Level, ExperiencePerLevel, ExperienceCurveStep);
		}
	}
}
