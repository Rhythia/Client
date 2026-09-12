using System;
using Godot;

public partial class Skip : UIComponent
{
    private Label3D label;
    private Tween tween;

    public override void OnExitTree()
    {
        if (Runner.Attempt == null)
            return;
        Runner.SkipAvailable -= OnSkipAvailable;
    }

    public override void Init()
    {
        label = GetNode<Label3D>("Label");
        label.Text = "Press Space to skip";
        Runner.SkipAvailable += OnSkipAvailable;
    }

    public override void Process(double delta, Attempt attempt)
    {
        if (tween != null && !attempt.CanSkip)
        {
            tween.Kill();
            tween = null;
            label.Modulate = new Color(label.Modulate, 0);
        }
    }

    public void OnSkipAvailable(Attempt attempt)
    {
        if (Runner.ObjectIndicesStart[typeof(Note)] >= attempt.Map.Notes.Length)
        {
            label.Text = "Press Space to complete";
        }

        if (tween != null)
            return;

        tween = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(label, "modulate", new Color(label.Modulate, 0.25f), 0.75f);
        tween.TweenProperty(label, "modulate", new Color(label.Modulate, 0.75f), 0.75f);
    }
}
