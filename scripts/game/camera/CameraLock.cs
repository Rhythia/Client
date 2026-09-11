using System;
using Godot;

public class CameraLock : CameraMode
{
    public override string Name => "Lock";

    public override bool Rankable => true;

    public override void Process(
        Attempt attempt,
        ReplayManager replayManager,
        Camera3D camera,
        MeshInstance3D cursor,
        Vector2 mouseDelta,
        float sensitivity
    )
    {
        var settings = attempt.Settings;
        var delta = new Vector2(1, -1) * (mouseDelta * sensitivity / 120f);

        if (settings.CursorDrift)
        {
            attempt.CursorPosition = attempt.IsReplay
                ? replayManager.CursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS)
                : (attempt.CursorPosition + delta).Clamp(-Constants.BOUNDS, Constants.BOUNDS);
        }
        else
        {
            attempt.RawCursorPosition = attempt.IsReplay
                ? replayManager.CursorPosition
                : attempt.RawCursorPosition + delta;
            attempt.CursorPosition = attempt.RawCursorPosition.Clamp(
                -Constants.BOUNDS,
                Constants.BOUNDS
            );
        }

        var origin = new Vector3(0, 0, 3.75f);
        float parallax = (float)(double)settings.CameraParallax;

        // camera should manage parallax on its own
        camera.Position =
            origin
            + (
                attempt.IsReplay && attempt.Replays.Length > 1
                    ? Vector3.Zero
                    : new Vector3(attempt.CursorPosition.X, attempt.CursorPosition.Y, 0) * parallax
            );
        camera.Rotation = Vector3.Zero;

        Vector3 cursorPos = new(attempt.CursorPosition.X, attempt.CursorPosition.Y, 0);

        if (cursorPos.IsFinite())
        {
            cursor.Position = cursorPos;
        }
    }
}
