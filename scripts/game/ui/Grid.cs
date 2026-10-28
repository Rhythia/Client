using System;
using System.Globalization;
using Godot;

public partial class Grid : MeshInstance3D, IUIComponent
{
    [Export]
    public Runner Runner { get; set; }
    public MeshInstance3D Cursor { get; set; }
    public MultiMeshInstance3D CursorTrail { get; set; }
    public MeshInstance3D GridGuides { get; set; }
    public MeshInstance3D VisibilityAssist { get; set; }

    private string popupLabelFormat;

    private static readonly PackedScene hit_feedback = GD.Load<PackedScene>("res://prefabs/hit_popup.tscn");
    private static readonly PackedScene miss_feedback = GD.Load<PackedScene>("res://prefabs/miss_icon.tscn");
    private int hitPopups,
        missPopups;

    public override void _ExitTree()
    {
        if (Runner.Attempt == null)
            return;
        Runner.HitResultChanged -= onHitResultChanged;
        // QueueFree();
    }

    public void Init()
    {
        VisibilityAssist ??= GetNode<MeshInstance3D>("VisibilityAssist");

        VisibilityAssist.Visible = Runner.Attempt.Settings.VisibilityAssist;
        (VisibilityAssist.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.VisibilityAssistImage;

        GridGuides ??= GetNode<MeshInstance3D>("GridGuides");

        GridGuides.Visible = Runner.Attempt.Settings.GridGuides;
        (GridGuides.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.GridGuidesImage;

        Cursor ??= GetNode<MeshInstance3D>("Cursor");
        (Cursor.Mesh as QuadMesh).Size = new Vector2(
            (float)(Constants.CURSOR_SIZE * Runner.Attempt.Settings.CursorScale),
            (float)(Constants.CURSOR_SIZE * Runner.Attempt.Settings.CursorScale)
        );

        (Cursor.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.CursorImage;

        // Cursor Transparency
        float alpha = Math.Clamp((float)(double)Runner.Attempt.Settings.CursorOpacity, 0, 1);
        Cursor.Transparency = 1f - alpha;

        CursorTrail ??= GetNode<MultiMeshInstance3D>("CursorTrail");
        (CursorTrail.MaterialOverride as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.CursorImage;

        CursorTrail.Transparency = 1f - alpha;

        (GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.GridImage;

        Runner.HitResultChanged += onHitResultChanged;
    }

    private void onHitResultChanged(int noteIndex, HitResult result)
    {
        float lateness = Runner.Attempt.IsReplay
            ? Runner.Attempt.HitsInfo[noteIndex]
            : (float)(((int)Runner.Attempt.Progress - Runner.Attempt.Map.Notes[noteIndex].Millisecond) / Runner.Speed);
        float factor = 1 - (float)Math.Round(Math.Max(0, lateness - 30) / 82f, 1);
        double hitScore = Math.Truncate(Runner.Attempt.UnitHitScore * Runner.Attempt.ComboMultiplier * factor);

        string[] listOfJudgements = { "perfect", "great", "okay", "bad" };
        string judgement = "";

        switch (result)
        {
            case HitResult.Hit:
                switch (factor) // judgements
                {
                    case 1f:
                        judgement = listOfJudgements[0]; // pawfect
                        break;

                    case 0.9f:
                        judgement = listOfJudgements[1]; // gewd
                        break;

                    case 0.8f:
                        judgement = listOfJudgements[2]; // okay
                        break;

                    case 0.7f:
                        judgement = listOfJudgements[3]; // bad
                        break;
                }
                spawnHitIcon(noteIndex, hitScore, judgement);
                break;
            case HitResult.Miss:
                spawnMissIcon(noteIndex);
                break;
        }
    }

    private void spawnHitIcon(int objIndex, double hitScore, string judgement)
    {
        if (!Runner.Attempt.Settings.HitPopups || hitPopups >= 8)
            return;

        hitPopups++;

        Label3D popup = hit_feedback.Instantiate<Label3D>();
        AddChild(popup);
        popupLabelFormat = popup.Text;

        popup.Text = string.Format(CultureInfo.CurrentCulture, popupLabelFormat, hitScore.ToString(CultureInfo.CurrentCulture), judgement);
        popup.GlobalPosition = new Vector3(Runner.Attempt.Map.Notes[objIndex].X, -1.4f, 0);
        // popup.Text = hitScore.ToString(CultureInfo.CurrentCulture);
        Tween tween = popup.CreateTween();
        tween.TweenProperty(popup, "transparency", 1, 0.25f);
        tween
            .Parallel()
            .TweenProperty(popup, "position", popup.Position + Vector3.Up / 4f, 0.25f)
            .SetTrans(Tween.TransitionType.Quint)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(
            Callable.From(() =>
            {
                hitPopups--;
                popup.QueueFree();
            })
        );
        tween.Play();
    }

    private void spawnMissIcon(int objIndex)
    {
        if (!Runner.Attempt.Settings.MissPopups || missPopups >= 64)
            return;

        missPopups++;

        Sprite3D icon = miss_feedback.Instantiate<Sprite3D>();
        AddChild(icon);
        icon.GlobalPosition = new Vector3(Runner.Attempt.Map.Notes[objIndex].X, -1.4f, 0);
        icon.Texture = SkinManager.Instance.Skin.MissFeedbackImage;
        Tween tween = icon.CreateTween();
        tween.TweenProperty(icon, "transparency", 1, 0.25f);
        tween
            .Parallel()
            .TweenProperty(icon, "position", icon.Position + Vector3.Up / 4f, 0.25f)
            .SetTrans(Tween.TransitionType.Quint)
            .SetEase(Tween.EaseType.Out);
        tween.TweenCallback(
            Callable.From(() =>
            {
                missPopups--;
                icon.QueueFree();
            })
        );
        tween.Play();
    }

    private static void updateGridPosition(Vector2 position) { }
}
