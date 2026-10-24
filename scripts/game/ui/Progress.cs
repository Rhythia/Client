using System;
using Godot;

public partial class Progress : UIComponent
{
    private Label3D label;

    public override void Init()
    {
        label = GetNode<Label3D>("Label");
        label.Visible = !Runner.Attempt.Settings.SuperSimpleHUD;
    }

    public override void Process(double delta, Attempt attempt)
    {
        label.Text = $"{util.String.FormatTime(Math.Max(0, attempt.Progress) / 1000)} / {util.String.FormatTime(attempt.Map.Length / 1000)}";
    }
}
