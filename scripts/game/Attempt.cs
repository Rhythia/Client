using System;
using System.Collections.Generic;
using Godot;

public partial class Attempt : GodotObject
{
	public ulong TimeStarted;
	public double DeathTime = -1;
	public string ID;
	public Map Map;
	//public List<Mod> Mods { get; set; } = new();
	public Dictionary<string, bool> Mods {get; set;} = new();
	public Dictionary<Type, IList<object>> Objects { get; set; } = new();
	public SettingsProfile Settings;
	public bool IsReplay = false;
	public bool Stopped = false;
	public bool Paused = false;
	public bool Alive = true;
	public bool CanSkip = false;
	public bool Qualifies = false;

	public string[] Players = [];

	public CameraMode CameraMode { get; set; } = new CameraLock();
	public double Progress { get; set; }
	public double Speed;
	public double StartFrom;
	public double MapLength;
	public uint PassedNotes = 0;

	public double Accuracy = 100;
	public double Health = 100;
	public double HealthStep = 15;

	public uint Hits = 0;
	public uint Misses = 0;
	public uint Sum = 0;
	public uint Score = 0;
	public uint Combo = 0;
	public uint ComboMultiplier = 1;
	public uint ComboMultiplierProgress = 0;
	public uint ComboMultiplierIncrement = 0;
	public double ModsMultiplier = 1;
	public float[] HitsInfo = [];
	public Color LastHitColour = new Color(0.37254903f, 0.61960787f, 0.627451f, 1);

	public Vector3 CameraPosition { get; set; } = new Vector3(0, 0, 3.75f);
	public Vector3 CameraRotation { get; set; } = Vector3.Zero;
	public Vector3 CameraBasisZ { get; set; } = new();

	public Vector2 CursorPosition = Vector2.Zero;
	public Vector2 RawCursorPosition = Vector2.Zero;
	public double DistanceMM = 0;

	public ulong FirstNote;
	public string ReplayPath;
	public Replay? Replay { get; set; }
	public Replay[] Replays;
	public List<float[]> ReplayFrames = [];
	public List<float> ReplaySkips = [];
	public ulong LastReplayFrame = 0;
	public uint ReplayFrameCountOffset = 0;
	public uint ReplayAttemptStatusOffset = 0;

	public Attempt(Map map, double speed, double startFrom, Dictionary<string, bool> mods, string[] players = null, Replay[] replays = null)
	{
		ID = $"{map.Name}_{OS.GetUniqueId()}_{Time.GetDatetimeStringFromUnixTime((long)Time.GetUnixTimeFromSystem())}".Replace(":", "_");
		Settings = SettingsManager.Instance.Settings;
		Replays = replays;
		IsReplay = Replays != null;
		Map = map;
		Speed = speed;
		StartFrom = startFrom;
		Players = players ?? [];
		Progress = Speed * -1000 - Settings.ApproachTime.Value * 1000 + StartFrom;
		ComboMultiplierIncrement = Math.Max(2, (uint)Map.Notes.Length / 200);
		Mods = mods;
		HitsInfo = IsReplay ? Replays[0].Notes : new float[Map.Notes.Length];
		
		if (StartFrom > 0)
		{
			Qualifies = false;
			foreach (Note note in Map.Notes)
			{
				if (note.Millisecond < StartFrom)
				{
					FirstNote = (ulong)note.Index + 1;
				}
			}
		}
	}
}
