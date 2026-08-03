using System;
using Godot;

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
        label.Visible = Runner.Attempt.Settings.SuperSimpleHUD ? false : true;
        if (!Runner.Attempt.Settings.SuperSimpleHUD)
        {
            label.Visible = Runner.Attempt.Settings.AltComboCounter ? false : true;
        }

        Runner.AttemptStatsUpdated += OnStatsUpdated;
    }

    public void OnStatsUpdated(Attempt attempt)
    {
        label.Text = attempt.Combo.ToString();
    }
}
