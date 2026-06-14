using Godot;

// Plays a two-part music track: an optional intro (played once) that transitions
// into a looping main track. Either part may be null:
//   - intro null, loop set  -> starts straight on the loop
//   - intro set,  loop null  -> plays the intro once, then silence
//   - both null              -> stops the music
public partial class MusicPlayer : AudioStreamPlayer
{
	private AudioStream pendingLoop;
	private AudioStream activeLoop;

	public override void _Ready()
	{
		Finished += OnFinished;
	}

	public void PlayMusic(AudioStream intro, AudioStream loop)
	{
		pendingLoop = null;
		activeLoop = loop;
		if (loop != null) SetStreamLoop(loop, true);
		if (intro != null) {
			SetStreamLoop(intro, false);
			pendingLoop = loop;
			Stream = intro;
			Play();
		} else if (loop != null) {
			Stream = loop;
			Play();
		} else {
			Stop();
		}
	}

	private void OnFinished()
	{
		// Intro finished: hand off to the loop (which loops internally for stream
		// types we could flag, so Finished won't fire again).
		if (pendingLoop != null) {
			AudioStream next = pendingLoop;
			pendingLoop = null;
			Stream = next;
			Play();
		} else if (activeLoop != null && Stream == activeLoop) {
			// Fallback for stream types whose loop flag we couldn't set: restart it.
			Play();
		}
	}

	// Enables/disables looping on the stream resource regardless of its concrete
	// type, so we don't depend on per-asset import settings.
	private static void SetStreamLoop(AudioStream stream, bool loop)
	{
		switch (stream) {
			case AudioStreamOggVorbis ogg:
				ogg.Loop = loop;
				break;
			case AudioStreamMP3 mp3:
				mp3.Loop = loop;
				break;
			case AudioStreamWav wav:
				wav.LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled;
				break;
		}
	}
}
