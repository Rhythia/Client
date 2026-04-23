using Godot;
using System;

public partial class Skip : UIComponent
{
	private Label3D label;
	private Tween tween;

	public override void OnExitTree()
    {
        if (Runner.Attempt == null) return;
		Runner.SkipAvailable -= OnSkipAvailable;
    }

	public override void Init()
	{
		label = GetNode<Label3D>("Label");
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
		if (tween != null) return;

		tween = CreateTween().SetLoops();
		tween.TweenProperty(label, "modulate", new Color(label.Modulate, 0), 0.5f);
		tween.TweenProperty(label, "modulate", new Color(label.Modulate, 1), 0.5f);
	}
}

