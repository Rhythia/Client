using Godot;
using System;

public partial class Combo : UIComponent
{
    private Label3D label;

	public override void OnExitTree()
    {
        if (Runner.Attempt == null) return;
		Runner.AttemptStatsUpdated -= OnStatsUpdated;
    }

	public override void Init()
	{
		label = GetNode<Label3D>("Label");
		Runner.AttemptStatsUpdated += OnStatsUpdated;
	}

    public void OnStatsUpdated(Attempt attempt)
	{
		label.Text = attempt.Combo.ToString();
	}
}
