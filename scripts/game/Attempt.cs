using System;
using System.Collections.Generic;
using Godot;

public partial class Attempt : GodotObject
{
    public bool IsReplay { get; set; }

    public bool Paused { get; set; }

    public CameraMode CameraMode { get; set; } = new CameraLock();

    public Map Map { get; set; }

    public double Progress { get; set; }

    public Vector3 CameraPosition { get; set; } = new Vector3(0, 0, 3.75f);

    public Vector3 CameraRotation { get; set; } = Vector3.Zero;

    public Vector2 CursorPosition { get; set; } = new();

    public Vector2 RawCursorPosition { get; set; } = new();

    public Vector3 CameraBasisZ { get; set; } = new();

    public int Speed { get; set; }

    public List<Mod> Mods { get; set; } = new();

    public Dictionary<Type, IList<object>> Objects { get; set; } = new();

    public SettingsProfile Settings { get; set; } = new();

    public double DistanceMM { get; set; }

    public Replay? Replay { get; set; }
}
