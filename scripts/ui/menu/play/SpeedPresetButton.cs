using System;
using Godot;

public partial class SpeedPresetButton : Button
{
    [Export(PropertyHint.Range, "20,1000")]
    public double Speed = 100;

    public override void _Ready()
    {
        base._Ready();

        TooltipText = Speed.ToString();

        updateState(Lobby.Speed);

        Lobby.Instance.SpeedChanged += updateState;
    }

    public override void _Pressed()
    {
        base._Pressed();

        Lobby.SetSpeed(Speed / 100);
    }

    private void updateState(double speed)
    {
        if (!IsInstanceValid(this)) { return; }

        Disabled = Speed / 100 == speed;
    }
}
