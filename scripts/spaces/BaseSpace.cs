using System;
using Godot;

public partial class BaseSpace : Node3D
{
    public bool Playing = false;

    public Camera3D Camera;
    public WorldEnvironment WorldEnvironment;
    public ImageTexture Cover;
    public Color NoteHitColor = new(0xffffffff);

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

    private void onHitResultChanged(int noteIndex, HitResult hitResult)
    {
        if (hitResult == HitResult.Hit)
        {
            OnHit(Game.Instance.Runner.Attempt.Combo);
        }
    }

    public override void _ExitTree()
    {
        if (Game.Instance?.Runner != null)
        {
            Game.Instance.Runner.HitResultChanged -= onHitResultChanged;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Playing)
        {
            if (SettingsManager.Instance.Settings.SpaceHitEffects)
            {
                NoteHitColor = NoteHitColor.Lerp(Game.Attempt.LastHitColour, Math.Min(1, (float)delta * 8));
            }
        }
    }

    public virtual void OnHit(uint combo)
    {
    }

    public virtual void Load()
    {
        var skin = SkinManager.Instance.Skin;

        NoteHitColor = skin.NoteColors[^1];
    }

    public virtual void UpdateMap(Map map)
    {
        Cover = ImageTexture.CreateFromImage(map.Cover.GetImage());
    }

    public virtual void UpdateState(bool playing)
    {
        Playing = playing;
        Camera.Current = !Playing;

        if (Playing && Game.Instance?.Runner != null)
        {
            Game.Instance.Runner.HitResultChanged -= onHitResultChanged;
            Game.Instance.Runner.HitResultChanged += onHitResultChanged;
        }
    }
}
