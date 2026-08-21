using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Attempt : RefCounted
{
    public ulong TimeStarted;
    public double DeathTime = -1;
    public string ID;
    public Map Map;
    public CameraMode CameraMode { get; set; } = new CameraLock();
    public List<Modifier> Modifiers { get; set; } = [];
    public Dictionary<Type, List<ITimelineObject>> Objects { get; set; } = [];
    public SettingsProfile Settings;
    public bool UseFadeOut = true;
    public bool IsReplay = false;
    public bool Stopped = false;
    public bool Paused = false;
    public bool Alive = true;
    public bool CanSkip = false;
    public bool Qualifies = true;

    public string[] Players = [];

    public double Progress { get; set; }
    public double Speed;
    public double StartFrom;
    public double Length;

    public double Accuracy = 100;
    public double Health = 100;
    public double HealthStep = 15;
    public bool HasHealthModifier = false;

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
    public Color LastHitColour = new();

    public Vector3 CameraPosition { get; set; } = new(0, 0, 3.75f);
    public Vector3 CameraRotation { get; set; } = Vector3.Zero;
    public Vector3 CameraBasisZ { get; set; } = new();

    public Vector2 CursorPosition = Vector2.Zero;
    public Vector2 RawCursorPosition = Vector2.Zero;
    public double DistanceMM = 0;

    public ulong FirstNote = 0;
    public string ReplayPath;
    public Replay[] Replays;
    public List<float[]> ReplayFrames = [];
    public List<float> ReplaySkips = [];
    public float MaxReplayLength = 0;
    public ulong LastReplayFrame = 0;
    public uint ReplayFrameCountOffset = 0;
    public uint ReplayAttemptStatusOffset = 0;

    public Attempt(Map map, double speed, double startFrom, CameraMode cameraMode, List<Modifier> mods, string[] players = null, Replay[] replays = null)
    {
        ID = $"{map.Name}_{OS.GetUniqueId()}_{Time.GetDatetimeStringFromUnixTime((long)Time.GetUnixTimeFromSystem())}".Replace(":", "_");
        Settings = SettingsManager.Instance.Settings;
        Replays = replays;
        IsReplay = Replays != null;
        Map = map;
        Speed = speed;
        StartFrom = startFrom;
        Length = Math.Max(1000 * (SoundManager.Song?.Stream?.GetLength() ?? 0), Map.Length + 1000);
        Players = players ?? [];
        Progress = Speed * -1000 - Settings.ApproachTime * 1000 + StartFrom;
        ComboMultiplierIncrement = Math.Max(2, (uint)Map.Notes.Length / 200);
        CameraMode = cameraMode;
        Modifiers = mods;
        HasHealthModifier = Modifiers.Any(mod => mod is IHealthModifier);
        Objects[typeof(Note)] = [.. map.Notes];
        HitsInfo = IsReplay ? Replays[0].Notes : new float[Map.Notes.Length];

        if (IsReplay)
        {
            foreach (var replay in Replays)
            {
                if (replay.Length > MaxReplayLength)
                {
                    MaxReplayLength = replay.Length;
                }
            }
        }

        if (StartFrom > 0)
        {
            Qualifies = false;

            foreach (var note in Map.Notes)
            {
                FirstNote = (ulong)note.Index;

                if (note.Millisecond >= StartFrom)
                {
                    break;
                }
            }
        }

        var skin = SkinManager.Instance.Skin;
        int index = (int)(FirstNote - 1) % skin.NoteColors.Length;

        LastHitColour = skin.NoteColors[index >= 0 ? index : ^Math.Abs(index)];
    }
}
