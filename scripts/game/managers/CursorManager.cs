using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Class containing all gameplay related logic regarding the cursor.
/// </summary>
public partial class CursorManager : Node
{
    [Export]
    private Runner runner;

    [Export]
    private PlayerInputController playerInputController;

    [Export]
    private ReplayManager replayManager;

    [Export]
    private MeshInstance3D cursorMesh;

    [Export]
    private Camera3D camera;

    private SettingsProfile settings;
    private float sensitivity;
    private List<MeshInstance3D> cursors;
    private Transform3D defaultCameraTransform = Transform3D.Identity;

    [Signal]
    public delegate void OnCursorUpdatedEventHandler(Vector2 position);

    public override void _Ready()
    {
        defaultCameraTransform = camera.Transform;

        cursorMesh ??= GetNode<MeshInstance3D>("Cursor");
        playerInputController ??= GetNode<PlayerInputController>("/PlayerInputController");
        replayManager ??= GetNode<ReplayManager>("ReplayManager");
    }

    public override void _EnterTree()
    {
        settings = Game.Attempt.IsReplay ? Game.Attempt.Replays[0].Settings : Game.Attempt.Settings;
        cursorMesh.Transform = Transform3D.Identity;
        cursors = [cursorMesh];

        if (defaultCameraTransform != Transform3D.Identity)
        {
            camera.Transform = defaultCameraTransform;
        }

        var parent = cursorMesh.GetParent();

        if (Game.Attempt.IsReplay)
        {
            for (int i = 1; i < Game.Attempt.Replays.Length; i++)
            {
                cursors.Add(cursorMesh.Duplicate() as MeshInstance3D);
                parent.AddChild(cursors[i]);
            }
        }
    }

    public override void _ExitTree()
    {
        if (cursors.Count > 1)
        {
            for (int i = 1; i < cursors.Count; i++)
            {
                cursors[i].QueueFree();
            }

            cursors.RemoveRange(1, cursors.Count - 1);
        }
    }

    public override void _Process(double delta)
    {
        if (!runner.Playing)
            return;

        updateCursorRotation(delta);
    }

    public void ShowCursor(int cursorIndex = 0, bool instant = true)
    {
        if (instant)
        {
            cursors[cursorIndex].Transparency = 1 - (float)(double)settings.CursorOpacity;
        }
        else
        {
            CreateTween().TweenProperty(cursors[cursorIndex], "transparency", 1, 0.5);
        }
    }

    public void HideCursor(int cursorIndex = 0, bool instant = true)
    {
        ShowCursor(cursorIndex, instant);
    }

    public void UpdateCursor(Vector2 inputDelta, int cursorIndex = 0)
    {
        EmitSignalOnCursorUpdated(inputDelta);

        sensitivity = (float)(double)settings.Sensitivity;

        if (settings.AbsoluteInput && !runner.Attempt.IsReplay)
        {
            sensitivity = (float)(double)settings.AbsoluteSensitivity;
        }

        sensitivity *= (float)(double)settings.FoV / 70f;

        if (settings.AbsoluteInput || runner.Attempt.IsReplay)
        {
            repositionAbsolute();
        }

        var attempt = runner.Attempt;

        attempt.CameraMode.Process(attempt, replayManager, camera, cursors[cursorIndex], inputDelta, sensitivity);
    }

    // Reset everything to zero so it doesn't have infinite sensitivity
    private void repositionAbsolute()
    {
        camera.Rotation = Vector3.Zero;
        runner.Attempt.RawCursorPosition = Vector2.Zero;
        runner.Attempt.CursorPosition = Vector2.Zero;
    }

    private void updateCursorRotation(double delta) =>
        cursorMesh.RotationDegrees += Vector3.Back * (float)(double)settings.CursorRotation * (float)delta;
}
