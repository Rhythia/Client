using System;
using Godot;

public partial class ModifierButton : Button
{
    [Export]
    public string Modifier = "";

    [Export]
    public string Description = "";

    public override void _Ready()
    {
        base._Ready();
        TooltipText = Description != "" ? Description : Modifier;

        Godot.Collections.Dictionary<string, bool> mods = new(Lobby.Modifiers);

        updateState(mods);

        Lobby.Instance.ModifiersChanged += updateState;

        if (Modifier == "HardRock")
        {
            try {
                string path = "res://user/skins/default/modifiers/hardrock.png";
                if (FileAccess.FileExists(path))
                {
                    Image img = Image.LoadFromFile(ProjectSettings.GlobalizePath(path));
                    if (img != null) {
                        Icon = ImageTexture.CreateFromImage(img);
                    }
                }
            } catch (Exception) { }
        }
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
