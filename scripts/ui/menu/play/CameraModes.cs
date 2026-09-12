using System.Collections.Generic;
using Godot;

public partial class CameraModes : Panel
{
    [Export]
    private VBoxContainer container;

    [Export]
    private Button templateButton;

    private List<Button> buttons = [];

    public override void _Ready()
    {
        container.RemoveChild(templateButton);

        Lobby.Instance.CameraModeChanged += updateButtons;

        updateModes();
    }

    public override void _EnterTree()
    {
        updateButtons(Lobby.CameraMode.Name);
    }

    private void updateModes()
    {
        buttons = [];

        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var mode in Rhythia.CameraModes)
        {
            var button = templateButton.Duplicate() as Button;

            button.Name = mode.Name;
            button.Text = mode.Name;

            button.Pressed += () =>
            {
                if (Lobby.CameraMode.Name != mode.Name)
                {
                    Lobby.SetCameraMode(mode);
                }
                else
                {
                    updateButtons(Lobby.CameraMode.Name);
                }
            };

            container.AddChild(button);
            buttons.Add(button);
        }

        updateButtons(Lobby.CameraMode.Name);
    }

    private void updateButtons(string mode)
    {
        foreach (var button in buttons)
        {
            button.ButtonPressed = button.Name == mode;
        }
    }
}
