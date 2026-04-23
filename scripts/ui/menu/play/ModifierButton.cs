using System;
using Godot;

public partial class ModifierButton : Button
{
    [Export]
    public string Modifier = "";

    public override void _Ready()
    {
        base._Ready();

        TooltipText = Modifier;

        Godot.Collections.Dictionary<string, bool> mods = new(Lobby.Modifiers);

        updateState(mods);

        Lobby.Instance.ModifiersChanged += updateState;
    }

    public override void _Pressed()
    {
        base._Pressed();

        if (Lobby.Modifiers.TryGetValue(Modifier, out bool active))
        {
            Lobby.SetModifier(Modifier, !active);
        }
    }

    private void updateState(Godot.Collections.Dictionary<string, bool> mods)
    {
        if (IsInstanceValid(this) && mods.TryGetValue(Modifier, out bool active))
        {
            ButtonPressed = active;
        }
    }
}
