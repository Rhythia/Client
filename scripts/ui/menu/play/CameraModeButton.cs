using System;
using Godot;

// will be reworked when the legacyrunner is rewritten
public partial class CameraModeButton : Button
{
    [Export]
    public string CameraMode = "";

    public override void _Ready()
    {
        base._Ready();

        Godot.Collections.Dictionary<string, bool> mods = new(Lobby.Modifiers);

        updateState(mods);

        Lobby.Instance.ModifiersChanged += updateState;
    }

    public override void _Pressed()
    {
        base._Pressed();

        Lobby.SetModifier("Spin", CameraMode == "Spin");
    }

    private void updateState(Godot.Collections.Dictionary<string, bool> mods)
    {
        if (!IsInstanceValid(this)) { return; }

        Disabled = (CameraMode == "Spin" && mods["Spin"]) || (CameraMode == "Lock" && !mods["Spin"]);
    }
}
