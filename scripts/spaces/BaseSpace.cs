using Godot;
using System;
using System.IO;

public partial class BaseSpace : Node3D
{
    public bool Playing = false;

    public Camera3D Camera;
    public WorldEnvironment WorldEnvironment;
    public ImageTexture Cover;
    public Color NoteHitColor = new(1, 1, 1);

    public override void _Ready()
    {
        base._Ready();

        Camera = (Camera3D)FindChild("Camera3D", false);

        if (Camera == null)
        {
            Camera = new() { Fov = 70 };
            AddChild(Camera);
        }

        WorldEnvironment = GetNode<WorldEnvironment>("WorldEnvironment");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Playing)
        {
            if (SettingsManager.Instance.Settings.SpaceHitEffects)
            {
                NoteHitColor = NoteHitColor.Lerp(LegacyRunner.CurrentAttempt.LastHitColour, Math.Min(1, (float)delta * 8));
            }
        }
    }

    public virtual void UpdateMap(Map map)
    {
        Cover = ImageTexture.CreateFromImage(map.Cover.GetImage());
    }

    public virtual void UpdateState(bool playing)
    {
        Playing = playing;
        Camera.Current = !Playing;
    }
}
