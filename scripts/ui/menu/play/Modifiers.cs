using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Modifiers : Panel
{
    [Export]
    private HBoxContainer container;

    [Export]
    private Button templateButton;

    private List<Button> buttons = [];

    public override void _Ready()
    {
        container.RemoveChild(templateButton);

        Lobby.Instance.ModifiersChanged += updateButtons;

        updateModifiers();
    }

    public override void _EnterTree()
    {
        updateButtons([.. Lobby.Modifiers.Select(mod => mod.Name)]);
    }

    private void updateModifiers()
    {
        buttons = [];

        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var mod in Rhythia.Modifiers)
        {
            var button = templateButton.Duplicate() as Button;

            button.Name = mod.Name;
            button.TooltipText = mod.Name;
            button.Icon = Util.Misc.GetModIcon(mod.Name);
            button.AddThemeColorOverride("icon_pressed_color", mod.Color);
            button.AddThemeColorOverride("icon_hover_pressed_color", mod.Color);

            button.Pressed += () =>
            {
                Lobby.SetModifier(mod, !Lobby.Modifiers.Select(mod => mod.Name).Contains(mod.Name));
            };

            container.AddChild(button);
            buttons.Add(button);
        }

        updateButtons([.. Lobby.Modifiers.Select(mod => mod.Name)]);
    }

    private void updateButtons(string[] modifiers)
    {
        foreach (var button in buttons)
        {
            button.ButtonPressed = modifiers.Contains(button.Name);
        }
    }
}
