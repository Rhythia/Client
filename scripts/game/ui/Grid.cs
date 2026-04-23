using System;
using Godot;

public partial class Grid : UIComponent
{
    private SettingsProfile settings;

    [Export]
    public MeshInstance3D Cursor { get; set; }

    public override void ApplySettings(SettingsProfile settings)
    {
        this.settings = settings;
    }

    public override void Process(double delta, Attempt state)
    {
        updateCursorPosition(state.CursorPosition);
    }

    private void updateGridPosition(Vector2 position)
    {

    }

    private void updateCursorPosition(Vector2 position)
    {
        Cursor.Position = new Vector3(position.X, position.Y, 0);
    }
}

