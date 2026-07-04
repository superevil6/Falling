using Godot;

// A presentation-style cutscene: an ordered list of slides played one after another
// by the CutscenePlayer. Assign one to a Stage (plays before that stage) or to the
// boot Intro scene (plays before the title screen).
public partial class Cutscene : Resource
{
	[Export]
	public string Name {get;set;}
	[Export]
	public CutsceneSlide[] Slides {get;set;}
}
