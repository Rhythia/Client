using System;
using Godot;

public class CameraLock : CameraMode
{
    public override string Name => "Lock";

    public override bool Rankable => true;

    public override void Process(Attempt attempt, Camera3D camera, Vector2 mouseDelta)
    {
        var settings = attempt.Settings;

        float sensitivity = (float)settings.Sensitivity.Value;
        sensitivity *= (float)settings.FoV.Value / 70f;

        if (settings.CursorDrift.Value)
        {
            attempt.CursorPosition = (attempt.CursorPosition + new Vector2(1, -1) * mouseDelta / 120 * sensitivity).Clamp(-Constants.BOUNDS, Constants.BOUNDS);
        }
        else
        {
            attempt.RawCursorPosition += new Vector2(1, -1) * mouseDelta / 120 * sensitivity;
            attempt.CursorPosition = attempt.RawCursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS);
        }

        attempt.CursorPosition = new Vector2(attempt.CursorPosition.X, attempt.CursorPosition.Y);

        camera.Position = new Vector3(0, 0, 3.75f) + new Vector3(attempt.CursorPosition.X, attempt.CursorPosition.Y, 0) * (float)settings.CameraParallax.Value;
        camera.Rotation = Vector3.Zero;

        attempt.CameraPosition = camera.Position;
        attempt.CameraRotation = camera.Rotation;
    }
}
