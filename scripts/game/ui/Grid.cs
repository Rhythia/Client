using System;
using Godot;

public partial class Grid : MeshInstance3D, IUIComponent
{
    [Export] public Runner Runner {get; set;}
    public MeshInstance3D Cursor { get; set; }
    public MultiMeshInstance3D CursorTrail { get; set; }

    private static readonly PackedScene hit_feedback = GD.Load<PackedScene>("res://prefabs/hit_popup.tscn");
	private static readonly PackedScene miss_feedback = GD.Load<PackedScene>("res://prefabs/miss_icon.tscn");
    private int hitPopups, missPopups;

    public override void _ExitTree()
    {
        if (Runner.Attempt == null) return;
		Runner.HitResultChanged -= onHitResultChanged;
        QueueFree();
    }

    public void Init()
    {
        Cursor ??= GetNode<MeshInstance3D>("Cursor");
        (Cursor.Mesh as QuadMesh).Size = new Vector2((float)(Constants.CURSOR_SIZE * Runner.Attempt.Settings.CursorScale.Value), (float)(Constants.CURSOR_SIZE * Runner.Attempt.Settings.CursorScale.Value));
		(Cursor.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.CursorImage;

        CursorTrail ??= GetNode<MultiMeshInstance3D>("CursorTrail");
        (CursorTrail.MaterialOverride as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.CursorImage;

        Runner.HitResultChanged += onHitResultChanged;
    }

    private void onHitResultChanged(int noteIndex, HitResult result)
    {
        float lateness = Runner.Attempt.IsReplay ? Runner.Attempt.HitsInfo[noteIndex] : (float)(((int)Runner.Attempt.Progress - Runner.Attempt.Map.Notes[noteIndex].Millisecond) / Runner.Attempt.Speed);
		float factor = 1 - Math.Max(0, lateness - 25) / 150f;
        uint hitScore = (uint)(100 * Runner.Attempt.ComboMultiplier * Runner.Attempt.ModsMultiplier * factor * ((Runner.Attempt.Speed - 1) / 2.5 + 1));

        switch (result)
        {
            case HitResult.Hit:
                spawnHitIcon(noteIndex, hitScore);
                break;
            case HitResult.Miss:
                spawnMissIcon(noteIndex);
                break;
        }
    }

    private void spawnHitIcon(int objIndex, uint hitScore)
    {
		if (!Runner.Attempt.Settings.HitPopups || hitPopups >= 64) return;

		hitPopups++;

		Label3D popup = hit_feedback.Instantiate<Label3D>();
		AddChild(popup);
		popup.GlobalPosition = new Vector3(Runner.Attempt.Map.Notes[objIndex].X, -1.4f, 0);
		popup.Text = hitScore.ToString();
		Tween tween = popup.CreateTween();
		tween.TweenProperty(popup, "transparency", 1, 0.25f);
		tween.Parallel().TweenProperty(popup, "position", popup.Position + Vector3.Up / 4f, 0.25f).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(() => {
			hitPopups--;
			popup.QueueFree();
		}));
		tween.Play();
    }

    private void spawnMissIcon(int objIndex)
    {
		if (!Runner.Attempt.Settings.MissPopups || missPopups >= 64) return;

		missPopups++;

		Sprite3D icon = miss_feedback.Instantiate<Sprite3D>();
		AddChild(icon);
		icon.GlobalPosition = new Vector3(Runner.Attempt.Map.Notes[objIndex].X, -1.4f, 0);
		icon.Texture = SkinManager.Instance.Skin.MissFeedbackImage;
		Tween tween = icon.CreateTween();
		tween.TweenProperty(icon, "transparency", 1, 0.25f);
		tween.Parallel().TweenProperty(icon, "position", icon.Position + Vector3.Up / 4f, 0.25f).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(() => {
			missPopups--;
			icon.QueueFree();
		}));
		tween.Play();
    }

    private void updateGridPosition(Vector2 position)
    {

    }
}

